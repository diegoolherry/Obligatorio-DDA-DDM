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
foreach (var (method, path) in new[] { ("GET", "/ingreso"), ("POST", "/ingreso"), ("GET", "/InGrEsO/"), ("POST", "/INGRESO/continuacion") })
{
    var observed = await FirstHop("http", method, path);
    Check(observed.Context.Response.StatusCode == 400
        && observed.Context.Response.ContentType == "text/plain; charset=utf-8"
        && observed.Context.Response.Headers.CacheControl == "no-store"
        && !observed.Context.Response.Headers.ContainsKey("Location")
        && observed.Body == "Se requiere HTTPS para ingresar. Abrí la dirección HTTPS configurada para esta aplicación."
        && observed.NextCalls == 0,
        $"W1 HTTP {method} {path} rejects exactly without form processing");
}
var secureIngreso = await FirstHop("https", "GET", "/ingreso");
Check(secureIngreso.NextCalls == 1 && secureIngreso.Context.Response.StatusCode == 200 && secureIngreso.Body.Contains("<form>"),
    "W1 HTTPS ingreso continues exactly once");
foreach (var path in new[] { "/", "/counter", "/weather", "/_blazor", "/ingresos" })
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
    public string? LastMethod, LastPath, LastBody, LastBearer;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        LastMethod = request.Method.Method;
        LastPath = request.RequestUri?.AbsolutePath;
        LastBearer = request.Headers.Authorization?.ToString();
        LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await Reply(request);
    }
}
