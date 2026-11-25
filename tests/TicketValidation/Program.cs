using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Authentication;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Security.Authentication;
using API_Berruti.Services.Location;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.EntityFrameworkCore.Extensions;

// MVP-005 T2: pruebas deterministas, sin MySQL. El servicio y los controllers reales corren contra un repositorio
// en memoria cuya consumición condicional emula el UPDATE atómico; la consulta SQL real y la concurrencia en MySQL
// las cubre tests/MySqlTicketValidation (con base de prueba dedicada, no se ejecuta aquí).
var comprobaciones = 0;
var fallos = 0;
void Verificar(bool condicion, string nombre)
{
    comprobaciones++;
    if (condicion) Console.WriteLine($"PASS: {nombre}");
    else { fallos++; Console.Error.WriteLine($"FAIL: {nombre}"); }
}

async Task Caso(string nombre, Func<Task> cuerpo)
{
    try { await cuerpo(); }
    catch (Exception error)
    {
        comprobaciones++;
        fallos++;
        Console.Error.WriteLine($"FAIL: {nombre} ({error.GetType().Name}: {error.Message})");
    }
}

// Reloj: 2026-11-24 15:00 UTC = 12:00 en Montevideo (UTC-3).
var ahora = new DateTime(2026, 11, 24, 15, 0, 0, DateTimeKind.Utc);
var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo");
var parametros = new ParametrosUbicacion { ZonaHoraria = zona };

const string Pasajero1 = "pasajero-publico-1";
const string Pasajero2 = "pasajero-publico-2";
const string CobradorPublico = "cobrador-publico-1";
const string SvcHoy = "00000000-0000-0000-0000-00000000000a";   // Ombúes → Radial, hoy 14:00
const string SvcHoy2 = "00000000-0000-0000-0000-00000000000b";  // Radial → Colonia, hoy 16:00
const string SvcAyer = "00000000-0000-0000-0000-00000000000c";   // ayer
const string SvcManana = "00000000-0000-0000-0000-00000000000d"; // mañana
var qrValido = new string('A', 64);
var qrOtroTramo = new string('B', 64);
var qrVencido = new string('C', 64);
var qrUsado = new string('D', 64);
var qrAjeno = new string('E', 64);
var qrManana = new string('F', 64);
var qrDesconocido = new string('9', 64);

FakeTicketsRepository NuevoRepo()
{
    var repo = new FakeTicketsRepository { CobradorId = "US-9" };
    foreach (var servicioAsignado in new[] { SvcHoy, SvcHoy2, SvcAyer, SvcManana }) repo.Asignar("US-9", servicioAsignado);
    repo.Agregar(Pasajero1, Registro("TI-1", qrValido, SvcHoy, "Ombúes", "Radial de Conchillas", new DateTime(2026, 11, 24, 14, 0, 0)));
    repo.Agregar(Pasajero1, Registro("TI-2", qrOtroTramo, SvcHoy2, "Radial de Conchillas", "Colonia", new DateTime(2026, 11, 24, 16, 0, 0)));
    repo.Agregar(Pasajero1, Registro("TI-3", qrVencido, SvcAyer, "Ombúes", "Colonia", new DateTime(2026, 11, 23, 9, 0, 0)));
    repo.Agregar(Pasajero1, Registro("TI-4", qrUsado, SvcHoy, "Ombúes", "Radial de Conchillas", new DateTime(2026, 11, 24, 14, 0, 0)),
        usadoUtc: new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc));
    repo.Agregar(Pasajero1, Registro("TI-6", qrManana, SvcManana, "Colonia", "Ombúes", new DateTime(2026, 11, 25, 8, 0, 0)));
    repo.Agregar(Pasajero2, Registro("TI-5", qrAjeno, SvcHoy, "Ombúes", "Radial de Conchillas", new DateTime(2026, 11, 24, 14, 0, 0)));
    return repo;
}

static TicketRegistro Registro(string id, string qr, string servicioId, string origen, string destino, DateTime salida) =>
    new(id, qr, null, 1, servicioId, $"tramo-{servicioId[^1]}", origen, destino, salida, salida.AddHours(1),
        PoliticaAsiento.SinAsiento, null);

TicketsService NuevoServicio(FakeTicketsRepository repo, RelojPrueba reloj) => new(repo, reloj, parametros);

// ---------- S4: el primer escaneo válido consume y el segundo se rechaza ----------
await Caso("S4", async () =>
{
    var repo = NuevoRepo();
    var reloj = new RelojPrueba(ahora);
    var servicio = NuevoServicio(repo, reloj);
    var primero = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy });
    Verificar(primero.Resultado == ResultadoValidacion.Valido, "S4: el primer escaneo del tramo correcto es válido");
    Verificar(primero.Ticket?.TicketId == "TI-1" && primero.Ticket.OrigenNombre == "Ombúes"
        && primero.Ticket.DestinoNombre == "Radial de Conchillas" && primero.Ticket.ServicioId == SvcHoy,
        "S4: el resultado válido describe el tramo y el servicio del ticket");
    Verificar(primero.UsadoUtc == ahora && repo.Estado(qrValido).UsadoUtc == ahora && repo.Estado(qrValido).UsadoPor == "US-9",
        "S4: el ticket queda utilizado con la hora UTC y el cobrador que validó");

    reloj.Definir(ahora.AddMinutes(5));
    var segundo = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy });
    Verificar(segundo.Resultado == ResultadoValidacion.YaUtilizado, "S4: el segundo escaneo del mismo ticket es rechazado como ya utilizado");
    Verificar(segundo.UsadoUtc == ahora, "S4: ya utilizado informa la hora del primer uso, no la del reintento");
    Verificar(repo.Estado(qrValido).UsadoUtc == ahora, "S4: el segundo escaneo no modifica la marca de uso");
});

// ---------- S5: tramo incorrecto no consume ----------
await Caso("S5", async () =>
{
    var repo = NuevoRepo();
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    var resultado = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrOtroTramo, ServicioId = SvcHoy });
    Verificar(resultado.Resultado == ResultadoValidacion.TramoIncorrecto, "S5: ticket de otro tramo → tramo incorrecto");
    Verificar(resultado.Ticket?.ServicioId == SvcHoy2 && resultado.Ticket.OrigenNombre == "Radial de Conchillas"
        && resultado.Ticket.DestinoNombre == "Colonia", "S5: informa el tramo propio del ticket");
    Verificar(repo.Estado(qrOtroTramo).UsadoUtc is null && repo.Estado(qrOtroTramo).UsadoPor is null,
        "S5: el ticket de otro tramo sigue sin usar");
    var correcto = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrOtroTramo, ServicioId = SvcHoy2 });
    Verificar(correcto.Resultado == ResultadoValidacion.Valido, "S5: después, en su tramo, el mismo ticket sigue siendo válido");

    var usadoOtroTramo = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrUsado, ServicioId = SvcHoy2 });
    Verificar(usadoOtroTramo.Resultado == ResultadoValidacion.TramoIncorrecto,
        "S5: un ticket ya usado escaneado en otro tramo se informa como tramo incorrecto (prioridad documentada)");
});

// ---------- S3: vencido y código inexistente ----------
await Caso("S3", async () =>
{
    var repo = NuevoRepo();
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    var vencido = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrVencido, ServicioId = SvcAyer });
    Verificar(vencido.Resultado == ResultadoValidacion.Vencido && vencido.Ticket?.ServicioId == SvcAyer,
        "S3: ticket de un día anterior → vencido, con su servicio");
    Verificar(repo.Estado(qrVencido).UsadoUtc is null, "S3: un ticket vencido no se consume");

    var inexistente = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrDesconocido, ServicioId = SvcHoy });
    Verificar(inexistente.Resultado == ResultadoValidacion.CodigoInexistente && inexistente.Ticket is null && inexistente.UsadoUtc is null,
        "S3: código desconocido → código inexistente, sin datos de ticket");
    var consumosAntes = repo.ConsumosIntentados;
    var ajeno = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = "https://example.invalid/" + new string('x', 400), ServicioId = SvcHoy });
    Verificar(ajeno.Resultado == ResultadoValidacion.CodigoInexistente && repo.ConsumosIntentados == consumosAntes,
        "S3: un QR ajeno al sistema (formato/longitud) es código inexistente sin tocar la base");
    var mayusculas = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido.ToLowerInvariant(), ServicioId = SvcHoy });
    Verificar(mayusculas.Resultado == ResultadoValidacion.CodigoInexistente && repo.Estado(qrValido).UsadoUtc is null,
        "S3: el código se compara tal cual (sensible a mayúsculas), no se normaliza a otro ticket");

    Verificar(repo.Tickets.All(t => t.Value.UsadoUtc == (t.Key == qrUsado ? new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc) : (DateTime?)null)),
        "S3: ningún rechazo modificó ticket alguno");
});

// ---------- regla de «vencido» en hora de Uruguay ----------
await Caso("vencido-uruguay", async () =>
{
    // 2026-11-25 02:30 UTC es 23:30 del 24 en Montevideo: el servicio del 24 todavía no venció.
    var repo = NuevoRepo();
    var reloj = new RelojPrueba(new DateTime(2026, 11, 25, 2, 30, 0, DateTimeKind.Utc));
    var servicio = NuevoServicio(repo, reloj);
    var tarde = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrOtroTramo, ServicioId = SvcHoy2 });
    Verificar(tarde.Resultado == ResultadoValidacion.Valido, "vencido: 23:30 hora de Uruguay del día del servicio sigue vigente aunque UTC ya sea el día siguiente");
    // 2026-11-25 03:30 UTC es 00:30 del 25: el servicio del 24 ya venció.
    reloj.Definir(new DateTime(2026, 11, 25, 3, 30, 0, DateTimeKind.Utc));
    var pasada = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy });
    Verificar(pasada.Resultado == ResultadoValidacion.Vencido && repo.Estado(qrValido).UsadoUtc is null,
        "vencido: pasada la medianoche de Uruguay el ticket del día anterior vence y no se consume");
    var futuro = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrManana, ServicioId = SvcManana });
    Verificar(futuro.Resultado == ResultadoValidacion.Valido, "vencido: el día del servicio actual es válido (no se rechaza por estar en el futuro cercano)");
});

// ---------- S4/S6: concurrencia, un solo ganador ----------
await Caso("concurrencia", async () =>
{
    var repo = NuevoRepo();
    repo.ExigirLlegadaSimultanea = true;
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    var solicitud = () => servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy });
    var resultados = await Task.WhenAll(Task.Run(solicitud), Task.Run(solicitud));
    Verificar(resultados.Count(r => r.Resultado == ResultadoValidacion.Valido) == 1
        && resultados.Count(r => r.Resultado == ResultadoValidacion.YaUtilizado) == 1,
        "S6: dos escaneos simultáneos → exactamente un válido y un ya utilizado");
    Verificar(resultados.Single(r => r.Resultado == ResultadoValidacion.YaUtilizado).UsadoUtc == ahora,
        "S6: el perdedor ve la hora del uso ganador");
    var primeraLectura = repo.Llamadas.IndexOf("BuscarPorQr");
    var consumos = repo.Llamadas.Select((n, i) => (n, i)).Where(x => x.n == "Consumir").Select(x => x.i).ToList();
    Verificar(consumos.Count == 2 && (primeraLectura < 0 || consumos.All(i => i < primeraLectura)),
        "S6: la decisión es el UPDATE condicional (no hay lectura previa que decida), una llamada por escaneo");
});

// ---------- S3/S5: cuenta que no es un cobrador activo ----------
await Caso("cobrador-inactivo", async () =>
{
    var repo = NuevoRepo();
    repo.CobradorId = null;
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    var rechazado = false;
    try { await servicio.ValidarAsync("quien-sea", new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy }); }
    catch (UnauthorizedAccessException) { rechazado = true; }
    Verificar(rechazado && repo.Estado(qrValido).UsadoUtc is null, "una identidad que no es cobrador activo no consume nada");
});

// ---------- L9: solo en servicios asignados (decisión previa a cualquier lectura o consumo) ----------
await Caso("L9", async () =>
{
    var repo = NuevoRepo();
    repo.Desasignar("US-9", SvcHoy);
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    foreach (var qr in new[] { qrValido, qrOtroTramo, qrUsado, qrDesconocido })
    {
        var rechazado = false;
        try { await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qr, ServicioId = SvcHoy }); }
        catch (CobradorNoAsignadoException) { rechazado = true; }
        Verificar(rechazado, $"L9: servicio no asignado → CobradorNoAsignadoException (código {qr[..2]}…)");
    }
    Verificar(repo.ConsumosIntentados == 0 && !repo.Llamadas.Contains("BuscarPorQr") && repo.Estado(qrValido).UsadoUtc is null,
        "L9: sin asignación no se intenta consumir ni se lee ticket alguno");
    // Otro servicio sí asignado: el mismo cobrador valida normalmente.
    var otro = await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrOtroTramo, ServicioId = SvcHoy2 });
    Verificar(otro.Resultado == ResultadoValidacion.Valido, "L9: el cobrador sigue validando en su servicio asignado");
    // La asignación es por cobrador: otro cobrador asignado a SvcHoy no habilita a US-9.
    repo.Asignar("US-10", SvcHoy);
    var ajeno = false;
    try { await servicio.ValidarAsync(CobradorPublico, new ValidarTicketRequestDto { QrOpaco = qrValido, ServicioId = SvcHoy }); }
    catch (CobradorNoAsignadoException) { ajeno = true; }
    Verificar(ajeno && repo.Estado(qrValido).UsadoUtc is null, "L9: la asignación de otro cobrador no habilita a este");
});

// ---------- S2: consulta del pasajero ----------
await Caso("S2", async () =>
{
    var repo = NuevoRepo();
    var servicio = NuevoServicio(repo, new RelojPrueba(ahora));
    var lista = await servicio.ListarDelPasajeroAsync(Pasajero1);
    Verificar(lista.Vigentes.Select(t => t.TicketId).SequenceEqual(["TI-1", "TI-2", "TI-6"]),
        "S2: vigentes = sin usar y no vencidos, ordenados por salida");
    Verificar(lista.Vigentes.All(t => t.Estado == EstadoTicket.SinUsar && t.UsadoUtc is null),
        "S2: todos los vigentes están sin usar");
    Verificar(lista.Anteriores.Select(t => t.TicketId).SequenceEqual(["TI-4", "TI-3"]),
        "S2: anteriores = utilizados y vencidos, el más reciente primero");
    Verificar(lista.Anteriores.Single(t => t.TicketId == "TI-4").Estado == EstadoTicket.Utilizado
        && lista.Anteriores.Single(t => t.TicketId == "TI-4").UsadoUtc == new DateTime(2026, 11, 24, 13, 0, 0, DateTimeKind.Utc)
        && lista.Anteriores.Single(t => t.TicketId == "TI-3").Estado == EstadoTicket.Vencido,
        "S2: estados utilizado (con fecha de uso) y vencido");
    var primero = lista.Vigentes[0];
    Verificar(primero.QrOpaco == qrValido && primero.OrigenNombre == "Ombúes" && primero.DestinoNombre == "Radial de Conchillas"
        && primero.ServicioId == SvcHoy && primero.Salida == new DateTime(2026, 11, 24, 14, 0, 0),
        "S2: cada ticket trae QR opaco, tramo, servicio y horario");
    var todos = lista.Vigentes.Concat(lista.Anteriores).Select(t => t.QrOpaco).ToList();
    Verificar(!todos.Contains(qrAjeno) && repo.UltimoPasajeroConsultado == Pasajero1,
        "S2: solo tickets del pasajero autenticado");
    var otro = await servicio.ListarDelPasajeroAsync(Pasajero2);
    Verificar(otro.Vigentes.Count == 1 && otro.Vigentes[0].QrOpaco == qrAjeno && otro.Anteriores.Count == 0,
        "S2: otro pasajero ve únicamente los suyos");
    // Un ticket sin usar pasa a vencido solo con el día de Uruguay.
    var tarde = await new TicketsService(repo, new RelojPrueba(new DateTime(2026, 11, 25, 3, 30, 0, DateTimeKind.Utc)), parametros)
        .ListarDelPasajeroAsync(Pasajero1);
    Verificar(tarde.Vigentes.Select(t => t.TicketId).SequenceEqual(["TI-6"]) && tarde.Anteriores.Count(t => t.Estado == EstadoTicket.Vencido) == 3,
        "S2: tras la medianoche de Uruguay los del día anterior pasan a anteriores/vencido");
});

// ---------- HTTP real: 401/403 y contratos JSON ----------
await Caso("http", async () =>
{
    const string clave = "clave-sintetica-solo-pruebas-0123456789-abcdef";
    var configuracion = new Dictionary<string, string?>
    {
        ["Jwt:Key"] = clave, ["Jwt:Issuer"] = "berruti-api-prueba", ["Jwt:Audience"] = "berruti-clientes-prueba",
        ["ConnectionStrings:Berruti"] = "Server=localhost;Database=berrutilocal;User ID=berruti_app;Password=ficticia-no-conexion"
    };
    var repo = NuevoRepo();
    repo.Desasignar("US-9", SvcManana); // el cobrador no viaja en el servicio de mañana
    var reloj = new RelojPrueba(ahora);
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://127.0.0.1:0");
    builder.Logging.ClearProviders();
    builder.Configuration.AddInMemoryCollection(configuracion);
    builder.Services.AgregarAutenticacionLocal(builder.Configuration, builder.Environment);
    builder.Services.AddControllers().AddApplicationPart(typeof(TicketsController).Assembly)
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
    builder.Services.AddSingleton<TimeProvider>(reloj);
    builder.Services.AddSingleton(parametros);
    builder.Services.AddSingleton<ITicketsRepository>(repo);
    builder.Services.AddScoped<ITicketsService, TicketsService>();
    await using var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    await app.StartAsync();
    try
    {
        var emisor = new EmisorJwt(new ConfigurationBuilder().AddInMemoryCollection(configuracion).Build());
        string Token(string sub, params RolIngreso[] roles) => emisor.Emitir(sub, roles, DateTime.UtcNow).Token;
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        HttpRequestMessage Pedido(HttpMethod metodo, string ruta, string? token, object? cuerpo = null)
        {
            var pedido = new HttpRequestMessage(metodo, ruta);
            if (token is not null) pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (cuerpo is not null) pedido.Content = JsonContent.Create(cuerpo);
            return pedido;
        }
        var tokenPasajero1 = Token(Pasajero1, RolIngreso.Pasajero);
        var tokenPasajero2 = Token(Pasajero2, RolIngreso.Pasajero);
        var tokenCobrador = Token(CobradorPublico, RolIngreso.Cobrador);
        var tokenAdmin = Token("admin-publico", RolIngreso.Administrador);
        var cuerpoValidar = new { qrOpaco = qrValido, servicioId = SvcHoy };

        async Task<HttpStatusCode> Estado(HttpMethod metodo, string ruta, string? token, object? cuerpo = null)
        {
            using var respuesta = await http.SendAsync(Pedido(metodo, ruta, token, cuerpo));
            return respuesta.StatusCode;
        }

        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", null, cuerpoValidar) == HttpStatusCode.Unauthorized,
            "S3/S4: validar sin identidad → 401");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", "token.invalido.firma", cuerpoValidar) == HttpStatusCode.Unauthorized,
            "S3/S4: validar con token inválido → 401");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenPasajero1, cuerpoValidar) == HttpStatusCode.Forbidden,
            "S3/S4: un pasajero no puede validar → 403");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenAdmin, cuerpoValidar) == HttpStatusCode.Forbidden,
            "S3/S4: un administrador no puede validar → 403");
        Verificar(repo.Estado(qrValido).UsadoUtc is null && repo.ConsumosIntentados == 0,
            "S3/S4: los 401/403 no llegan al repositorio ni consumen el ticket");

        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, new { qrOpaco = "  ", servicioId = SvcHoy }) == HttpStatusCode.BadRequest,
            "S3: código vacío → 400");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, new { qrOpaco = qrValido }) == HttpStatusCode.BadRequest,
            "S3: sin tramo seleccionado → 400");

        using (var ok = await http.SendAsync(Pedido(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, cuerpoValidar)))
        {
            var texto = await ok.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(texto);
            var raiz = json.RootElement;
            Verificar(ok.StatusCode == HttpStatusCode.OK && raiz.GetProperty("resultado").GetString() == "valido"
                && raiz.GetProperty("ticket").GetProperty("origenNombre").GetString() == "Ombúes"
                && raiz.GetProperty("ticket").GetProperty("servicioId").GetString() == SvcHoy,
                "S4: cobrador → 200 con resultado `valido` y el tramo del ticket (JSON camelCase)");
            Verificar(!texto.Contains(qrValido) && !texto.Contains(Pasajero1) && !texto.Contains("US-"),
                "S4: la respuesta del cobrador no devuelve el QR ni datos del pasajero ni IDs internos de usuario");
        }
        using (var repetido = await http.SendAsync(Pedido(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, cuerpoValidar)))
        {
            using var json = JsonDocument.Parse(await repetido.Content.ReadAsStringAsync());
            Verificar(repetido.StatusCode == HttpStatusCode.OK && json.RootElement.GetProperty("resultado").GetString() == "yaUtilizado"
                && json.RootElement.TryGetProperty("usadoUtc", out var usado) && usado.GetDateTime() == ahora,
                "S4: segundo escaneo → `yaUtilizado` con la hora del primer uso");
        }
        foreach (var (qr, servicioId, esperado) in new[]
        {
            (qrOtroTramo, SvcHoy, "tramoIncorrecto"), (qrVencido, SvcAyer, "vencido"), (qrDesconocido, SvcHoy, "codigoInexistente")
        })
        {
            using var respuesta = await http.SendAsync(Pedido(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, new { qrOpaco = qr, servicioId }));
            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Verificar(respuesta.StatusCode == HttpStatusCode.OK && json.RootElement.GetProperty("resultado").GetString() == esperado,
                $"S3: resultado `{esperado}` por HTTP");
        }
        Verificar(repo.Estado(qrOtroTramo).UsadoUtc is null && repo.Estado(qrVencido).UsadoUtc is null,
            "S5: tramo incorrecto y vencido no consumieron por HTTP");

        // L9: el cobrador solo valida en los servicios a los que está asignado → 403 (como la publicación de posiciones).
        var consumosPrevios = repo.ConsumosIntentados;
        foreach (var (qr, descripcion) in new[]
        {
            (qrManana, "un ticket válido de ese servicio"), (qrOtroTramo, "un ticket de otro tramo"), (qrDesconocido, "un código inexistente")
        })
        {
            using var sinAsignar = await http.SendAsync(Pedido(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, new { qrOpaco = qr, servicioId = SvcManana }));
            var textoSinAsignar = await sinAsignar.Content.ReadAsStringAsync();
            Verificar(sinAsignar.StatusCode == HttpStatusCode.Forbidden && textoSinAsignar.Length == 0,
                $"L9: {descripcion} en un servicio no asignado → 403 sin cuerpo, no un resultado de negocio");
        }
        Verificar(repo.Estado(qrManana).UsadoUtc is null && repo.Estado(qrManana).UsadoPor is null && repo.ConsumosIntentados == consumosPrevios,
            "L9: el cobrador no asignado no consumió el ticket ni llegó a la sentencia de consumo");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenCobrador, new { qrOpaco = qrDesconocido, servicioId = "servicio-que-no-existe" }) == HttpStatusCode.Forbidden,
            "L9: un servicio inexistente tampoco está asignado → 403 (no revela si existe)");
        Verificar(await Estado(HttpMethod.Post, "/api/validaciones-ticket", tokenPasajero1, new { qrOpaco = qrManana, servicioId = SvcManana }) == HttpStatusCode.Forbidden,
            "L9: el rol sigue mandando: un pasajero → 403 también en ese servicio");

        Verificar(await Estado(HttpMethod.Get, "/api/tickets", null) == HttpStatusCode.Unauthorized, "S2: listar sin identidad → 401");
        Verificar(await Estado(HttpMethod.Get, "/api/tickets", tokenCobrador) == HttpStatusCode.Forbidden, "S2: un cobrador no lista tickets de pasajero → 403");
        Verificar(await Estado(HttpMethod.Get, "/api/tickets", tokenAdmin) == HttpStatusCode.Forbidden, "S2: un administrador no lista tickets de pasajero → 403");

        using (var propia = await http.SendAsync(Pedido(HttpMethod.Get, "/api/tickets", tokenPasajero1)))
        {
            var texto = await propia.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(texto);
            var vigentes = json.RootElement.GetProperty("vigentes");
            var anteriores = json.RootElement.GetProperty("anteriores");
            Verificar(propia.StatusCode == HttpStatusCode.OK && vigentes.GetArrayLength() == 2 && anteriores.GetArrayLength() == 3,
                "S2: pasajero → 200 con `vigentes` y `anteriores` (tras consumir TI-1 por HTTP)");
            Verificar(anteriores.EnumerateArray().Any(t => t.GetProperty("ticketId").GetString() == "TI-1"
                    && t.GetProperty("estado").GetString() == "utilizado" && t.TryGetProperty("usadoUtc", out _))
                && vigentes.EnumerateArray().All(t => t.GetProperty("estado").GetString() == "sinUsar"),
                "S2: estados `sinUsar`/`utilizado` y consumo visible para el pasajero");
            Verificar(!texto.Contains(qrAjeno) && !texto.Contains("TI-5"), "S2: un pasajero nunca recibe el QR ni el ticket de otro");
        }
        using (var ajeno = await http.SendAsync(Pedido(HttpMethod.Get, "/api/tickets", tokenPasajero2)))
        {
            var texto = await ajeno.Content.ReadAsStringAsync();
            Verificar(texto.Contains(qrAjeno) && !texto.Contains(qrValido) && !texto.Contains(qrOtroTramo) && !texto.Contains(qrVencido),
                "S2: el otro pasajero solo recibe su propio QR");
        }
        Verificar(repo.UltimoPasajeroConsultado == Pasajero2, "S2: la identidad consultada sale del `sub` del token");
    }
    finally { await app.StopAsync(); }
});

// ---------- SQL generado por EF (sin conexión) ----------
await Caso("sql", () =>
{
    using var contexto = new API_Berruti.Data.BerrutiDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<API_Berruti.Data.BerrutiDbContext>()
        .UseMySQL("server=localhost;port=3306;database=berrutilocal;user=berruti_app;password=sin-conexion").Options);
    var sqlPasajero = TicketsRepository.ConsultaDePasajero(contexto, "US-1").ToQueryString();
    Verificar(sqlPasajero.Contains("`intento_compra`") && sqlPasajero.Contains("`usuario_id` = 'US-1'") && sqlPasajero.Contains("`compra_servicio`")
        && sqlPasajero.Contains("`ticket`") && sqlPasajero.Contains("`tramo_intento_compra`") && sqlPasajero.Contains("`usado_utc`"),
        "S2: la consulta del pasajero filtra por intento_compra.usuario_id y une ticket, compra_servicio y tramo_intento_compra");
    var sqlQr = TicketsRepository.ConsultaPorQr(contexto, "ABC").ToQueryString();
    Verificar(sqlQr.Contains("`qr_opaco` = 'ABC'") && sqlQr.Contains("`servicio_id`"), "S4: la consulta por QR traduce a SQL con el servicio comprado");
    return Task.CompletedTask;
});

// ---------- contratos estáticos ----------
await Caso("contratos", () =>
{
    Verificar(typeof(TicketsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Pasajero",
        "S2: TicketsController exige rol Pasajero");
    Verificar(typeof(ValidacionesTicketController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Cobrador",
        "S3: ValidacionesTicketController exige rol Cobrador");
    var propiedadesTicket = typeof(Ticket).GetProperties().Select(p => p.Name).ToHashSet();
    Verificar(propiedadesTicket.Contains("UsadoUtc") && propiedadesTicket.Contains("ValidadoPorUsuarioId")
        && typeof(Ticket).GetProperty("UsadoUtc")!.PropertyType == typeof(DateTime?),
        "S4: Ticket registra el uso con marca UTC anulable y el cobrador");
    Verificar(Enum.GetNames<ResultadoValidacion>().OrderBy(n => n).SequenceEqual(
        ["CodigoInexistente", "TramoIncorrecto", "Valido", "Vencido", "YaUtilizado"]),
        "S3: exactamente cinco resultados de validación");
    return Task.CompletedTask;
});

Console.WriteLine($"{comprobaciones - fallos}/{comprobaciones} PASS");
return fallos == 0 ? 0 : 1;

// ---------- dobles ----------
sealed class RelojPrueba(DateTime inicial) : TimeProvider
{
    private long _ticks = inicial.Ticks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
    public void Definir(DateTime utc) => Interlocked.Exchange(ref _ticks, utc.Ticks);
}

sealed class EstadoFake
{
    public required TicketRegistro Datos { get; init; }
    public required string Propietario { get; init; }
    public DateTime? UsadoUtc { get; set; }
    public string? UsadoPor { get; set; }
}

// Emula la consulta SQL real: la propiedad y la consumición condicional se deciden dentro del «repositorio»,
// y ConsumirAsync es indivisible respecto de otras llamadas (como un UPDATE de una sola sentencia).
sealed class FakeTicketsRepository : ITicketsRepository
{
    private readonly object _gate = new();
    private int _llegadas;
    private readonly TaskCompletionSource _ambos = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Dictionary<string, EstadoFake> Tickets { get; } = new(StringComparer.Ordinal);
    public HashSet<(string Cobrador, string Servicio)> Asignaciones { get; } = [];
    public void Asignar(string cobradorId, string servicioId) => Asignaciones.Add((cobradorId, servicioId));
    public void Desasignar(string cobradorId, string servicioId) => Asignaciones.Remove((cobradorId, servicioId));
    public List<string> Llamadas { get; } = [];
    public string? CobradorId { get; set; }
    public bool ExigirLlegadaSimultanea { get; set; }
    public int ConsumosIntentados { get; private set; }
    public string? UltimoPasajeroConsultado { get; private set; }

    public EstadoFake Estado(string qr) => Tickets[qr];

    public void Agregar(string propietario, TicketRegistro datos, DateTime? usadoUtc = null) =>
        Tickets[datos.QrOpaco] = new EstadoFake { Datos = datos, Propietario = propietario, UsadoUtc = usadoUtc };

    private TicketRegistro Proyectar(EstadoFake e) => e.Datos with { UsadoUtc = e.UsadoUtc };

    public Task<IReadOnlyList<TicketRegistro>> ListarDePasajeroAsync(string identificadorPublico, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Llamadas.Add("ListarDePasajero");
            UltimoPasajeroConsultado = identificadorPublico;
            IReadOnlyList<TicketRegistro> lista = Tickets.Values.Where(t => t.Propietario == identificadorPublico).Select(Proyectar).ToList();
            return Task.FromResult(lista);
        }
    }

    public Task<string> BuscarCobradorIdAsync(string identificadorPublico, CancellationToken cancellationToken = default)
    {
        lock (_gate) Llamadas.Add("BuscarCobradorId");
        return CobradorId is null
            ? Task.FromException<string>(new UnauthorizedAccessException("La identidad no corresponde a un cobrador activo."))
            : Task.FromResult(CobradorId);
    }

    public Task<bool> EstaAsignadoAsync(string cobradorId, string servicioId, CancellationToken cancellationToken = default)
    {
        lock (_gate) Llamadas.Add("EstaAsignado");
        return Task.FromResult(Asignaciones.Contains((cobradorId, servicioId)));
    }

    public async Task<bool> ConsumirAsync(string qrOpaco, string servicioId, string cobradorId, DateTime ahoraUtc, DateTime inicioDiaLocal,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Llamadas.Add("Consumir");
            ConsumosIntentados++;
        }
        if (ExigirLlegadaSimultanea)
        {
            if (Interlocked.Increment(ref _llegadas) == 2) _ambos.SetResult();
            await _ambos.Task;
        }
        lock (_gate)
        {
            if (!Tickets.TryGetValue(qrOpaco, out var e)) return false;
            if (e.UsadoUtc is not null || e.Datos.ServicioId != servicioId || e.Datos.Salida < inicioDiaLocal) return false;
            e.UsadoUtc = ahoraUtc;
            e.UsadoPor = cobradorId;
            return true;
        }
    }

    public Task<TicketRegistro?> BuscarPorQrAsync(string qrOpaco, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Llamadas.Add("BuscarPorQr");
            return Task.FromResult(Tickets.TryGetValue(qrOpaco, out var e) ? Proyectar(e) : null);
        }
    }
}
