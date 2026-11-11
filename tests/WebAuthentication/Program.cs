using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web_App.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web_App.Models.Authentication;
using Web_App.Services.Authentication;

int checks = 0, failures = 0;
void Check(bool condition, string name)
{
    checks++;
    if (!condition) { failures++; Console.Error.WriteLine($"FAIL {name}"); }
    else Console.WriteLine($"PASS {name}");
}
const string token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
string Success() => JsonSerializer.Serialize(new { identificadorUsuario = token, roles = new[] { "pasajero" }, token, venceEnUtc = new TestClock().GetUtcNow().AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff'Z'") });
const string identity = "{\"identificadorUsuario\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\",\"roles\":[\"pasajero\"]}";
SolicitudIngresoDto Input() => new() { Correo = "prueba@example.invalid", Contrasena = "ficticia" };
HttpResponseMessage Response(int status, string body = "") => new((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
ServiceProvider Provider(FakeHandler handler, string? url = "https://api.example.invalid/")
{
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = url }).Build());
    services.AddLogging();
    services.AddSingleton<TimeProvider>(new TestClock());
    services.AddHttpClient("Autenticacion").ConfigurePrimaryHttpMessageHandler(() => handler);
    services.AddScoped<ISesionCliente, SesionCliente>();
    return services.BuildServiceProvider();
}
var fake = new FakeHandler();
using var provider = Provider(fake);
using var scope = provider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<ISesionCliente>();
Check(!service.Estado.Autenticada, "new circuit anonymous");
fake.Reply = _ => Task.FromResult(Response(200, Success()));
var result = await service.IngresarAsync(Input());
Check(result.Exito && result.EstadoHttp == 200 && service.Estado.Autenticada, "validated login commits session");
Check(fake.LastMethod == "POST" && fake.LastPath == "/api/autenticacion/ingreso" && fake.LastBearer == null, "login exact request without bearer");
Check(fake.LastBody == "{\"correo\":\"prueba@example.invalid\",\"contrasena\":\"ficticia\"}", "exact typed request body");
fake.Reply = _ => Task.FromResult(Response(200, identity));
result = await service.ProbarPasajeroAsync();
Check(result.Exito && fake.LastMethod == "GET" && fake.LastPath == "/api/autenticacion/prueba-pasajero" && fake.LastBearer == "Bearer " + token, "protected passenger request");
fake.Reply = _ => Task.FromResult(Response(403));
result = await service.ProbarAdministradorAsync();
Check(!result.Exito && result.EstadoHttp == 403 && result.Codigo == "denegado" && service.Estado.Autenticada && fake.LastPath == "/api/autenticacion/prueba-administrador", "server role denial preserves session");
fake.Reply = _ => throw new HttpRequestException("fictitious private detail");
result = await service.SalirAsync();
Check(!result.Exito && result.Mensaje.Contains("no confirmada") && !result.Mensaje.Contains("private") && service.Estado.Autenticada, "failed logout retains safe retry, sanitized uncertainty");
fake.Reply = _ => Task.FromResult(Response(204));
result = await service.SalirAsync();
Check(result.Exito && result.EstadoHttp == 204 && !service.Estado.Autenticada && fake.LastMethod == "POST" && fake.LastPath == "/api/autenticacion/salida" && fake.LastBearer == "Bearer " + token, "confirmed logout clears memory");
int calls = fake.Calls;
result = await service.ConsultarIdentidadAsync();
Check(!result.Exito && fake.Calls == calls, "no authorization after logout");
using (var otherScope = provider.CreateScope())
    Check(!otherScope.ServiceProvider.GetRequiredService<ISesionCliente>().Estado.Autenticada, "isolated DI scopes");

// Administrator reuses the same scoped service; no second login implementation.
var adminTransport = new FakeHandler();
using (var adminProvider = Provider(adminTransport))
using (var adminScope = adminProvider.CreateScope())
{
    var admin = adminScope.ServiceProvider.GetRequiredService<ISesionCliente>();
    adminTransport.Reply = _ => Task.FromResult(Response(200, Success().Replace("pasajero", "administrador")));
    var login = await admin.IngresarAsync(Input());
    Check(login == new ResultadoAutenticacion(true, 200, "ingresoConfirmado", "Sesión confirmada por la API.") &&
        admin.Estado.Autenticada, "administrator existing login commits a valid scoped session");
    foreach (var (status, body, code, message) in new[] {
        (403, "", "denegado", "La API denegó el acceso a esta operación."),
        (503, "", "servidor", "El servicio no está disponible. Reintente más tarde."),
        (200, identity.Replace("pasajero", "administrador"), "autorizado", "Operación autorizada por la API.") })
    {
        adminTransport.Reply = _ => Task.FromResult(Response(status, body));
        var observed = await admin.ProbarAdministradorAsync();
        Check(observed == new ResultadoAutenticacion(status == 200, status, code, message) &&
            admin.Estado.Autenticada && !admin.Estado.Ocupada &&
            adminTransport.LastMethod == "GET" && adminTransport.LastPath == "/api/autenticacion/prueba-administrador" &&
            adminTransport.LastBearer == "Bearer " + token && adminTransport.LastBody == null,
            $"administrator probe exact request/result and session preserved for {status}");
    }
    adminTransport.Reply = _ => throw new HttpRequestException("private error");
    var offline = await admin.ProbarAdministradorAsync();
    Check(offline == new ResultadoAutenticacion(false, null, "red", "No se pudo contactar con la API. Revise la conexión y reintente.") &&
        admin.Estado.Autenticada && !admin.Estado.Ocupada, "administrator probe sanitized network failure keeps retry available");
    using (var isolated = adminProvider.CreateScope())
        Check(!isolated.ServiceProvider.GetRequiredService<ISesionCliente>().Estado.Autenticada, "administrator session isolated from another circuit");
    adminTransport.Reply = _ => Task.FromResult(Response(204));
    var logout = await admin.SalirAsync();
    Check(logout == new ResultadoAutenticacion(true, 204, "salidaConfirmada", "Sesión cerrada en el servidor.") &&
        !admin.Estado.Autenticada && adminTransport.LastMethod == "POST" &&
        adminTransport.LastPath == "/api/autenticacion/salida" && adminTransport.LastBearer == "Bearer " + token,
        "administrator confirmed logout clears only its circuit");
    var beforeCalls = adminTransport.Calls;
    var afterLogout = await admin.ProbarAdministradorAsync();
    Check(afterLogout == new ResultadoAutenticacion(false, null, "sesionInvalida", "La sesión local no es válida o venció. Ingrese nuevamente.") &&
        !admin.Estado.Autenticada && adminTransport.Calls == beforeCalls, "administrator operation after logout sends no request");
}

// Passenger location requests reuse the scoped session with a role-neutral 403 text.
var locTransport = new FakeHandler();
using (var locProvider = Provider(locTransport))
using (var locScope = locProvider.CreateScope())
{
    var loc = locScope.ServiceProvider.GetRequiredService<ISesionCliente>();
    var noSession = await loc.SolicitarAutenticadaAsync(HttpMethod.Get, "api/ubicacion/servicios");
    Check(!noSession.Exito && locTransport.Calls == 0, "authenticated request without session sends nothing");
    locTransport.Reply = _ => Task.FromResult(Response(200, Success()));
    await loc.IngresarAsync(Input());
    locTransport.Reply = _ => Task.FromResult(Response(200, "[]"));
    var ok = await loc.SolicitarAutenticadaAsync(HttpMethod.Get, "api/ubicacion/servicios");
    Check(ok.Exito && ok.Contenido == "[]" && locTransport.LastMethod == "GET" && locTransport.LastPath == "/api/ubicacion/servicios"
        && locTransport.LastBearer == "Bearer " + token, "location request sends bearer to the exact route");
    locTransport.Reply = _ => Task.FromResult(Response(403));
    var denied = await loc.SolicitarAutenticadaAsync(HttpMethod.Get, "api/ubicacion/servicios");
    Check(!denied.Exito && denied.EstadoHttp == 403 && !denied.Mensaje.Contains("administrador") && loc.Estado.Autenticada, "403 text is role-neutral and keeps session");
    locTransport.Reply = _ => Task.FromResult(Response(404));
    var missing = await loc.SolicitarAutenticadaAsync(HttpMethod.Get, "api/ubicacion/servicios/x");
    Check(!missing.Exito && missing.EstadoHttp == 404 && loc.Estado.Autenticada, "404 keeps session");
    locTransport.Reply = _ => Task.FromResult(Response(401));
    var expired = await loc.SolicitarAutenticadaAsync(HttpMethod.Get, "api/ubicacion/servicios");
    Check(!expired.Exito && expired.EstadoHttp == 401 && !loc.Estado.Autenticada, "401 discards the local session");
}

foreach (var url in new string?[] { null, "", "http://api.example.invalid", "https://user:pass@api.example.invalid", "https://api.example.invalid/?q=1", "https://api.example.invalid/#x", "not a url" })
{
    var transport = new FakeHandler();
    using var p = Provider(transport, url);
    using var s = p.CreateScope();
    var client = s.ServiceProvider.GetRequiredService<ISesionCliente>();
    var r = await client.IngresarAsync(Input());
    Check(!r.Exito && r.Codigo == "configuracion" && r.Mensaje.Contains("Api:BaseUrl") && transport.Calls == 0, "unsafe/missing URL fails before transport");
}
fake.Reply = _ => Task.FromResult(Response(200, Success()));
result = await service.IngresarAsync(new());
Check(!result.Exito && result.Codigo == "validacion" && fake.Calls == calls, "DataAnnotations before request");
foreach (int status in new[] { 400, 401, 403, 503, 500, 302, 204 })
{
    fake.Reply = _ => Task.FromResult(Response(status, status == 400 ? "{\"errors\":{\"Correo\":[\"El correo no es válido.\"]}}" : "{}"));
    result = await service.IngresarAsync(Input());
    Check(!result.Exito && result.EstadoHttp == status && !service.Estado.Autenticada, $"login status {status} no false success");
    if (status == 401) Check(result.Mensaje == "No se pudo ingresar con las credenciales proporcionadas.", "generic exact credential rejection");
    if (status == 400) Check(result.Codigo == "validacion" && result.Mensaje == "El correo no es válido.", "recognized validation only");
}
foreach (var body in new[] { "null", "{}", "not-json", Success().Replace(token, "bad"), Success().Replace("pasajero", "unknown"), Success().Replace("[\"pasajero\"]", "[0]"), Success().Replace("[\"pasajero\"]", "null"), Success().Replace("Z\"", "+00:00\""), Success().Replace("2030", "2000") })
{
    fake.Reply = _ => Task.FromResult(Response(200, body));
    result = await service.IngresarAsync(Input());
    Check(!result.Exito && !service.Estado.Autenticada, "invalid response shape/token/roles/UTC/expiry rejected");
}
foreach (bool timeout in new[] { false, true })
{
    fake.Reply = _ => timeout ? throw new TaskCanceledException() : throw new HttpRequestException();
    result = await service.IngresarAsync(Input());
    Check(!result.Exito && result.EstadoHttp == null && !service.Estado.Autenticada, "network/timeout no success");
}
fake.Reply = _ => Task.FromResult(Response(200, Success()));
await service.IngresarAsync(Input());
fake.Reply = _ => Task.FromResult(Response(401));
result = await service.ConsultarIdentidadAsync();
Check(!result.Exito && result.EstadoHttp == 401 && !service.Estado.Autenticada && result.Codigo == "sesionInvalida", "authenticated 401 clears invalid session");
var pending = new TaskCompletionSource<HttpResponseMessage>();
fake.Reply = _ => pending.Task;
var first = service.IngresarAsync(Input());
Check(service.Estado.Ocupada, "busy visible during request");
calls = fake.Calls;
result = await service.IngresarAsync(Input());
Check(!result.Exito && result.Codigo == "ocupado" && fake.Calls == calls, "duplicate submission suppressed");
service.Descartar();
pending.SetResult(Response(200, Success()));
result = await first;
Check(!result.Exito && !service.Estado.Autenticada && !service.Estado.Ocupada, "late response cannot resurrect discarded session");
fake.Reply = _ => Task.FromResult(Response(200, Success()));
result = await service.IngresarAsync(Input());
Check(result.Exito, "retry after discarded operation");
calls = fake.Calls;
result = await service.IngresarAsync(Input());
Check(!result.Exito && service.Estado.Autenticada && fake.Calls == calls, "rejected replacement preserves current session");
using (var otherScope = provider.CreateScope())
    Check(!otherScope.ServiceProvider.GetRequiredService<ISesionCliente>().Estado.Autenticada, "active credential never shared between scopes");
fake.Reply = _ => Task.FromResult(Response(200, "{}"));
result = await service.SalirAsync();
Check(!result.Exito && service.Estado.Autenticada && result.Mensaje.Contains("no confirmada"), "logout requires exact 204");
foreach (var body in new[] { Success().Replace("pasajero", "Pasajero"), Success().Replace("[\"pasajero\"]", "[]") })
{
    service.Descartar();
    fake.Reply = _ => Task.FromResult(Response(200, body));
    result = await service.IngresarAsync(Input());
    Check(result.Exito, "no invented role cardinality/casing policy");
}
foreach (int status in new[] { 400, 403, 500, 503, 302, 200 })
{
    fake.Reply = _ => Task.FromResult(Response(status, "{\"errors\":{\"Correo\":[\"untrusted detail\"]}}"));
    result = await service.SalirAsync();
    Check(!result.Exito && result.EstadoHttp == status && service.Estado.Autenticada && result.Mensaje.Contains("no confirmada") && !result.Mensaje.Contains("untrusted"), $"logout {status} uncertainty and unchanged session");
}
fake.Reply = _ => Task.FromResult(Response(200, "{}"));
result = await service.ConsultarIdentidadAsync();
Check(!result.Exito && service.Estado.Autenticada, "invalid authenticated body preserves session");
fake.Reply = _ => Task.FromResult(Response(200, identity));
result = await service.ConsultarIdentidadAsync();
Check(result.Exito && fake.LastPath == "/api/autenticacion/identidad" && fake.LastBearer == "Bearer " + token, "identity typed consumer and per-request bearer");
var clock = (TestClock)provider.GetRequiredService<TimeProvider>();
clock.Now = clock.Now.AddDays(1);
calls = fake.Calls;
result = await service.ProbarPasajeroAsync();
Check(!result.Exito && !service.Estado.Autenticada && fake.Calls == calls, "exact local expiry boundary forbids bearer request");
clock.Now = clock.Now.AddDays(-1);
foreach (string body in new[] { Success().Replace("2030-01-02", "2030-01-01"), Success().Replace("2030-01-02", "2030-01-09"), Success().Replace(token, token[..42] + "B"), Success().Replace("[\"pasajero\"]", "[\"0\"]"), "{\"roles\":[]}" })
{
    fake.Reply = _ => Task.FromResult(Response(200, body));
    result = await service.IngresarAsync(Input());
    Check(!result.Exito && !service.Estado.Autenticada, "boundary/noncanonical/missing shape rejects session");
}
fake.Reply = _ => Task.FromResult(Response(200, Success()));
await service.IngresarAsync(Input());
var lateLogout = new TaskCompletionSource<HttpResponseMessage>();
fake.Reply = _ => lateLogout.Task;
var leaving = service.SalirAsync();
service.Descartar();
lateLogout.SetResult(Response(204));
result = await leaving;
Check(!result.Exito && !service.Estado.Autenticada, "discard during logout does not misreport confirmation");
// W1: composición real en memoria; sin servidor, TLS, variables de entorno ni puertos HTTPS.
async Task<(DefaultHttpContext Context, int NextCalls, string Body)> FirstHop(string scheme, string method, string path, bool guard = true)
{
    var context = new DefaultHttpContext();
    context.Request.Scheme = scheme;
    context.Request.Method = method;
    context.Request.Path = path;
    context.Request.Host = new HostString("localhost", 5025);
    using var body = new MemoryStream();
    context.Response.Body = body;
    int nextCalls = 0;
    RequestDelegate formSink = async ctx =>
    {
        nextCalls++;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.WriteAsync("<form><input type=\"password\"></form>");
    };
    var addresses = new ServerAddressesFeature();
    addresses.Addresses.Add("http://localhost:5025");
    var redirect = new HttpsRedirectionMiddleware(formSink, Options.Create(new HttpsRedirectionOptions()),
        new ConfigurationBuilder().Build(), NullLoggerFactory.Instance, addresses);
    RequestDelegate pipeline = guard ? new LimiteIngresoHttps(redirect.Invoke).InvokeAsync : redirect.Invoke;
    await pipeline(context);
    body.Position = 0;
    string text = await new StreamReader(body).ReadToEndAsync();
    return (context, nextCalls, text);
}
var baselineHttp = await FirstHop("http", "GET", "/ingreso", guard: false);
Check(baselineHttp.NextCalls == 1 && baselineHttp.Context.Response.StatusCode == 200 && baselineHttp.Body.Contains("<form>"),
    "W1 baseline framework redirection without HTTPS port reaches form sink");
var httpGuardBody = (await FirstHop("http", "GET", "/ingreso")).Body;
foreach (var (method, path) in new[] { ("GET", "/ingreso"), ("POST", "/ingreso"), ("GET", "/InGrEsO/"), ("POST", "/INGRESO/continuacion") })
{
    var observed = await FirstHop("http", method, path);
    Check(observed.Context.Response.StatusCode == 400
        && observed.Context.Response.ContentType == "text/plain; charset=utf-8"
        && observed.Context.Response.Headers.CacheControl == "no-store"
        && !observed.Context.Response.Headers.ContainsKey("Location")
        && observed.Body == httpGuardBody
        && observed.NextCalls == 0,
        $"W1 HTTP {method} {path} rejects exactly without form processing");
}
var secureIngreso = await FirstHop("https", "GET", "/ingreso");
Check(secureIngreso.NextCalls == 1 && secureIngreso.Context.Response.StatusCode == 200 && secureIngreso.Body.Contains("<form>"),
    "W1 HTTPS ingreso continues exactly once");
foreach (var path in new[] { "/_blazor", "/_blazor/negotiate" })
{
    var observed = await FirstHop("http", "POST", path);
    Check(observed.Context.Response.StatusCode == 400
        && observed.Context.Response.ContentType == "text/plain; charset=utf-8"
        && observed.Context.Response.Headers.CacheControl == "no-store"
        && observed.Body == httpGuardBody
        && observed.NextCalls == 0,
        $"W1 HTTP circuit {path} rejects before next (status {observed.Context.Response.StatusCode}, next {observed.NextCalls})");
    var secure = await FirstHop("https", "POST", path);
    Check(secure.Context.Response.StatusCode == 200 && secure.NextCalls == 1 && secure.Body.Contains("<form>"),
        $"W1 HTTPS circuit {path} continues exactly once");
}
foreach (var path in new[] { "/", "/counter", "/weather", "/_blazorish", "/_blazorish/negotiate", "/ingresos", "/ingresoextra" })
{
    var observed = await FirstHop("http", "GET", path);
    Check(observed.NextCalls == 1 && observed.Context.Response.StatusCode == 200 && observed.Body.Contains("<form>"),
        $"W1 sibling HTTP {path} preserves forwarding without HTTPS port");
}
// Fixture independiente de Git: solo busca la ruta pública concreta desde el bin del test.
var ancestor = new DirectoryInfo(AppContext.BaseDirectory);
const string webProgram = "src/Web/Web-App/Web-App/Program.cs";
while (ancestor != null && !File.Exists(Path.Combine(ancestor.FullName, webProgram))) ancestor = ancestor.Parent;
string source = ancestor == null ? "" : File.ReadAllText(Path.Combine(ancestor.FullName, webProgram));
int guardIndex = source.IndexOf("app.UseMiddleware<LimiteIngresoHttps>();", StringComparison.Ordinal);
int redirectIndex = source.IndexOf("app.UseHttpsRedirection();", StringComparison.Ordinal);
int razorIndex = source.IndexOf("app.MapRazorComponents<App>()", StringComparison.Ordinal);
Check(guardIndex >= 0 && guardIndex < redirectIndex && redirectIndex < razorIndex,
    "W1 structural Program registration puts guard before redirection and Razor (not network proof)");
string loginPage = ancestor == null ? "" : File.ReadAllText(Path.Combine(ancestor.FullName, "src/Web/Web-App/Web-App/Components/Pages/Login.razor"));
Check(loginPage.Contains("@onclick=\"ProbarAdministradorAsync\"") &&
    loginPage.Contains("Comprobar acceso de administrador") &&
    loginPage.Contains("Sesion.ProbarAdministradorAsync()") &&
    loginPage.Contains("Consultando permiso en la API…"),
    "administrator action wired with progress feedback (structural, not rendered UI)");
string appShell = ancestor == null ? "" : File.ReadAllText(Path.Combine(ancestor.FullName, "src/Web/Web-App/Web-App/Components/App.razor"));
Check(appShell.Contains("<Routes @rendermode=\"InteractiveServer\" />") && appShell.Contains("<HeadOutlet @rendermode=\"InteractiveServer\" />")
    && !loginPage.Contains("@rendermode InteractiveServer"),
    "interactive routing is global and login shares the scoped circuit (source check)");
string adminPage = ancestor == null ? "" : File.ReadAllText(Path.Combine(ancestor.FullName, "src/Web/Web-App/Web-App/Components/Pages/Admin/AdministracionOperativa.razor"));
Check(adminPage.Contains("Eliminar pendiente") && adminPage.Contains("Confirmar") && adminPage.Contains("Cancelar")
    && adminPage.Contains("ConfirmarEliminarAsync") && adminPage.Contains("CancelarEliminar")
    && adminPage.Contains("disabled=\"@Ocupado\"") && adminPage.Contains("_eliminacionPendiente")
    && adminPage.Contains("SolicitarEliminarParada") && adminPage.Contains("SolicitarEliminarTramo")
    && adminPage.Contains("SolicitarEliminarServicio") && adminPage.Contains("SolicitarEliminarTarifa")
    && adminPage.Contains("Cliente.EliminarAsync(pending.Tipo, pending.Id)")
    && adminPage.IndexOf("SolicitarEliminar(string tipo", StringComparison.Ordinal) < adminPage.IndexOf("ConfirmarEliminarAsync()", StringComparison.Ordinal),
    "all delete flows require typed confirm/cancel before request (source check)");
// MVP-003-02: public itinerary search client (no bearer, typed contract, user-facing states).
var searchFake = new FakeHandler();
var searchServices = new ServiceCollection();
searchServices.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Api:BaseUrl"] = "https://api.example.invalid/" }).Build());
searchServices.AddHttpClient("Autenticacion").ConfigurePrimaryHttpMessageHandler(() => searchFake);
searchServices.AddScoped<Web_App.Services.Operational.BusquedaItinerariosCliente>();
using (var searchProvider = searchServices.BuildServiceProvider())
{
    var search = searchProvider.GetRequiredService<Web_App.Services.Operational.BusquedaItinerariosCliente>();
    const string itinerary = "[{\"salida\":\"2031-05-12T08:00:00\",\"llegada\":\"2031-05-12T10:00:00\",\"precioTotal\":350.00," +
        "\"transbordo\":{\"parada\":{\"id\":\"r\",\"nombre\":\"Radial de Conchillas\"},\"minutos\":15},\"tramos\":[" +
        "{\"orden\":1,\"servicioId\":\"s1\",\"tramoId\":\"t1\",\"origen\":{\"id\":\"o\",\"nombre\":\"Ombúes\"},\"destino\":{\"id\":\"r\",\"nombre\":\"Radial de Conchillas\"},\"salida\":\"2031-05-12T08:00:00\",\"llegada\":\"2031-05-12T08:30:00\",\"importe\":100.00,\"politicaAsiento\":\"sinAsiento\"}," +
        "{\"orden\":2,\"servicioId\":\"s2\",\"tramoId\":\"t2\",\"origen\":{\"id\":\"r\",\"nombre\":\"Radial de Conchillas\"},\"destino\":{\"id\":\"c\",\"nombre\":\"Colonia\"},\"salida\":\"2031-05-12T08:45:00\",\"llegada\":\"2031-05-12T10:00:00\",\"importe\":250.00,\"politicaAsiento\":\"asignacionAutomatica\"}]}]";
    searchFake.Reply = _ => Task.FromResult(Response(200, itinerary));
    var found = await search.BuscarAsync("o id", "c", new DateOnly(2031, 5, 12));
    Check(searchFake.LastMethod == "GET" && searchFake.LastPath == "/api/itinerarios" && searchFake.LastBearer == null &&
        searchFake.LastQuery == "?origenId=o%20id&destinoId=c&fecha=2031-05-12", "itinerary search sends an anonymous GET with escaped ids and ISO date");
    Check(found.Error == null && found.Items.Count == 1 && found.Items[0].PrecioTotal == 350.00m && found.Items[0].Transbordo?.Minutos == 15 &&
        found.Items[0].Tramos.Select(t => t.PoliticaAsiento).SequenceEqual([Web_App.Models.Operational.PoliticaAsiento.SinAsiento, Web_App.Models.Operational.PoliticaAsiento.AsignacionAutomatica]) &&
        found.Items[0].Tramos[1].Destino.Nombre == "Colonia", "itinerary contract deserializes legs, transfer, total and seat policies");
    searchFake.Reply = _ => Task.FromResult(Response(200, "[]"));
    found = await search.BuscarAsync("o", "c", new DateOnly(2031, 5, 13));
    Check(found.Error == null && found.Items.Count == 0, "empty search result is not an error");
    searchFake.Reply = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("El origen y el destino deben ser paradas distintas.") });
    found = await search.BuscarAsync("o", "o", new DateOnly(2031, 5, 12));
    Check(found.Error == "El origen y el destino deben ser paradas distintas." && found.Items.Count == 0, "400 shows the API validation message");
    searchFake.Reply = _ => Task.FromResult(Response(404));
    found = await search.BuscarAsync("x", "c", new DateOnly(2031, 5, 12));
    Check(found.Error == Web_App.Services.Operational.BusquedaItinerariosCliente.ErrorParadaInexistente, "404 asks to refresh the stop list");
    searchFake.Reply = _ => Task.FromResult(Response(500, "internal detail"));
    found = await search.BuscarAsync("o", "c", new DateOnly(2031, 5, 12));
    Check(found.Error == Web_App.Services.Operational.BusquedaItinerariosCliente.ErrorServidor && !found.Error.Contains("internal"), "server error is sanitized and retryable");
    searchFake.Reply = _ => throw new HttpRequestException("fictitious private detail");
    var stops = await search.ListarParadasAsync();
    Check(stops.Error == Web_App.Services.Operational.BusquedaItinerariosCliente.ErrorConexion && stops.Items.Count == 0 &&
        searchFake.LastPath == "/api/paradas" && searchFake.LastBearer == null, "stop list network failure is reported for retry");
}

string locationPage = ancestor == null ? "" : File.ReadAllText(Path.Combine(ancestor.FullName, "src/Web/Web-App/Web-App/Components/Pages/Pasajero/UbicacionServicio.razor"));
Check(locationPage.Contains("@page \"/pasajero/ubicacion\"") && locationPage.Contains("href=\"/ingreso\"") && locationPage.Contains("Reintentar")
    && locationPage.Contains("Actualizar") && locationPage.Contains("desactualizada") && locationPage.Contains("Datos ficticios y aproximados")
    && locationPage.Contains("Parámetros académicos usados") && locationPage.Contains("d.LlegadaAproximada is { } llegada"),
    "passenger location page states session, retry, stale, fictitious data and parameters (source check)");
Console.WriteLine($"{checks - failures}/{checks} PASS");
return failures == 0 ? 0 : 1;

class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

class FakeHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> Reply { get; set; } = _ => throw new InvalidOperationException("Unexpected transport call");
    public int Calls { get; private set; }
    public string? LastMethod, LastPath, LastQuery, LastBody, LastBearer;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        LastMethod = request.Method.Method;
        LastPath = request.RequestUri?.AbsolutePath;
        LastQuery = request.RequestUri?.Query;
        LastBearer = request.Headers.Authorization?.ToString();
        LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await Reply(request);
    }
}
