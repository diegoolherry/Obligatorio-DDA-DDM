using System.Security.Claims;
using System.Globalization;
using System.Text.Json;
using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Authentication;
using API_Berruti.Services.Identifiers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using API_Berruti.Security.Authentication;

if (!args.SequenceEqual(new[] { "--unit" }))
{
    Console.Error.WriteLine("Uso: --unit. Solo políticas puras; no inicia HTTP ni conecta MySQL.");
    return 1;
}
var fallos = 0;
var pruebas = 0;
void Prueba(string nombre, Func<bool> comprobar)
{
    pruebas++;
    try
    {
        if (!comprobar()) throw new InvalidOperationException();
        Console.WriteLine($"PASS: {nombre}");
    }
    catch { fallos++; Console.Error.WriteLine($"FAIL: {nombre}"); }
}
Prueba("S10 contratos específicos implementados y consumidos", () =>
{
    var assembly = typeof(PoliticaSesion).Assembly;
    foreach (var nombre in new[] { "Repositories.Authentication.CuentasRepository", "Repositories.Authentication.ContadoresRepository",
        "Repositories.Authentication.SesionesRepository", "Services.Identifiers.Correlativo", "Services.Authentication.AutenticacionService" })
    {
        var clase = assembly.GetType("API_Berruti." + nombre);
        var contrato = assembly.GetType("API_Berruti." + nombre.Insert(nombre.LastIndexOf('.') + 1, "I"));
        if (clase == null || contrato == null || !contrato.IsInterface || !contrato.IsAssignableFrom(clase) || contrato.GetMethods().Length == 0) return false;
        if (clase.GetConstructors().SelectMany(c => c.GetParameters()).Any(p =>
            p.ParameterType.Namespace?.StartsWith("API_Berruti.Repositories") == true && !p.ParameterType.IsInterface)) return false;
    }
    return true;
});
var ahora = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var vence = ahora.AddDays(7);
Prueba("32 bytes Base64url canónicos y distintos", () =>
{
    var tokens = Enumerable.Range(0, 128).Select(_ => PoliticaSesion.NuevoToken()).ToArray();
    return tokens.Distinct().Count() == 128 && tokens.All(t => t.Length == 43 &&
        PoliticaSesion.EsToken(t) && Convert.FromBase64String(t.Replace('-', '+').Replace('_', '/') + "=").Length == 32);
});
Prueba("huella SHA256 exacta de 64 hex, no token recuperable", () =>
{
    var token = new string('A', 43);
    return PoliticaSesion.Huella(token) == Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))) &&
        PoliticaSesion.Huella(token).Length == 64;
});
foreach (var token in new[] { "", "US-1", "abc", new string('A', 42), new string('A', 44),
    new string('A', 42) + "+", new string('A', 42) + "B", new string('A', 42) + "=" })
    Prueba("rechaza token mal formado sin buscarlo", () => !PoliticaSesion.EsToken(token));
Prueba("reloj UTC conserva microsegundos MySQL", () => PoliticaSesion.AhoraUtc(new Reloj(ahora.AddTicks(1234567))) == ahora.AddTicks(1234560));
Prueba("vigente hasta un tick antes del vencimiento", () => PoliticaSesion.EsVigente(vence, null, true, vence.AddTicks(-1)));
Prueba("igual al vencimiento ya no es válida", () => !PoliticaSesion.EsVigente(vence, null, true, vence));
Prueba("después del vencimiento no es válida", () => !PoliticaSesion.EsVigente(vence, null, true, vence.AddTicks(1)));
Prueba("revocación rechaza aunque no venza", () => !PoliticaSesion.EsVigente(vence, ahora, true, ahora));
Prueba("usuario inactivo rechaza aunque no venza", () => !PoliticaSesion.EsVigente(vence, null, false, ahora));
Prueba("roles de servidor: pasajero no administrador, sin PK", () =>
{
    var identidad = PoliticaSesion.Identidad("publico-opaco", new[] { "Pasajero" });
    return identidad.Identity?.IsAuthenticated == true && identidad.IsInRole("Pasajero") &&
        !identidad.IsInRole("Administrador") && identidad.FindFirstValue(ClaimTypes.NameIdentifier) == "publico-opaco" &&
        identidad.Claims.Count() == 2;
});
Prueba("rol desconocido falla cerrado", () =>
{
    try { PoliticaSesion.Identidad("publico-opaco", new[] { "pasajero", "999" }); return false; }
    catch (InvalidOperationException) { return true; }
});
foreach (var encabezado in new[] { "", "Basic abc", "Bearer", "Bearer abc", "Bearer  " + new string('A', 43),
    "Bearer\t" + new string('A', 43), "Bearer " + new string('A', 43) + ",Bearer abc" })
    Prueba("encabezado inválido no entrega token", () => EncabezadoBearer.Token(encabezado) == null);
Prueba("esquema Bearer insensible a mayúsculas, token exacto", () =>
    EncabezadoBearer.Token("bEaReR " + new string('A', 43)) == new string('A', 43));

var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Berruti"] = "Server=localhost;Database=berrutiauthtests;User ID=berruti_test;Password=ficticia-unit-no-conexion"
}).Build();
var servicios = new ServiceCollection();
servicios.AddLogging();
servicios.AddRouting();
servicios.AgregarAutenticacionLocal(configuracion, new Entorno());
using var proveedor = servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
using var alcance = proveedor.CreateScope();
Prueba("DI resuelve interfaces y comparte exactamente el mismo DbContext scoped", () =>
{
    var sp = alcance.ServiceProvider;
    var db = sp.GetRequiredService<BerrutiDbContext>();
    var cuentasDi = sp.GetRequiredService<ICuentasRepository>();
    var contadoresDi = sp.GetRequiredService<IContadoresRepository>();
    var sesionesDi = sp.GetRequiredService<ISesionesRepository>();
    object? Campo(object obj, string nombre) => obj.GetType().GetField(nombre,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(obj);
    return cuentasDi is CuentasRepository && contadoresDi is ContadoresRepository && sesionesDi is SesionesRepository &&
        sp.GetRequiredService<ICorrelativo>() is Correlativo && sp.GetRequiredService<IAutenticacionService>() is AutenticacionService &&
        ReferenceEquals(Campo(cuentasDi, "_contexto"), db) && ReferenceEquals(Campo(contadoresDi, "_contexto"), db) &&
        ReferenceEquals(Campo(sesionesDi, "_contexto"), db) && ReferenceEquals(Campo(cuentasDi, "_contadores"), contadoresDi) &&
        db.Model.FindEntityType(typeof(Sesion))?.FindProperty(nameof(Sesion.VenceUtc))?.GetColumnType() == "datetime(6)";
});
Prueba("contador público mantiene guardia transaccional antes de SQL", () =>
{
    try { alcance.ServiceProvider.GetRequiredService<IContadoresRepository>().IncrementarAsync("US-").GetAwaiter().GetResult(); return false; }
    catch (InvalidOperationException e) { return e.Message == "El alta requiere una transacción."; }
});
Prueba("configuración ausente no impide construir DI ni resolver handler", () =>
{
    var sinDb = new ServiceCollection();
    sinDb.AddLogging();
    sinDb.AgregarAutenticacionLocal(new ConfigurationBuilder().Build(), new Entorno());
    using var p = sinDb.BuildServiceProvider();
    return p.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>() != null;
});
foreach (var cadena in new[] { "", "Server=remote;Database=berrutiauthtests;User ID=berruti_test;Password=ficticia",
    "Server=localhost;Database=berrutiauthtests;User ID=root;Password=ficticia",
    "Server=localhost;Database=berrutilocal;User ID=berruti_test;Password=ficticia" })
    Prueba("configuración fuera del par local falla cerrada", () =>
    {
        try { ConfiguracionAutenticacion.ValidarConexion(cadena, "AuthenticationTests"); return false; }
        catch { return true; }
    });

var cuentas = new CuentasFicticias();
var sesiones = new SesionesFicticias();
var reloj = new RelojMutable(ahora.AddTicks(1234567));
IAutenticacionService servicio = new AutenticacionService(cuentas, sesiones, reloj);
RespuestaIngresoDto? ingreso = null;
Prueba("servicio: ingreso persiste solo huella, FK y UTC con siete días absolutos", () =>
{
    ingreso = servicio.IngresarAsync(new SolicitudIngresoDto { Correo = "prueba@example.invalid", Contrasena = "x" }).GetAwaiter().GetResult();
    var fila = sesiones.Filas.Single().Value;
    return ingreso != null && ingreso.IdentificadorUsuario == "publico-opaco" && ingreso.Roles.SequenceEqual(new[] { RolIngreso.Pasajero }) &&
        fila.Huella == PoliticaSesion.Huella(ingreso.Token) && fila.Huella != ingreso.Token && fila.UsuarioId == "US-1" &&
        fila.CreadaUtc.Kind == DateTimeKind.Utc && fila.CreadaUtc.Ticks % 10 == 0 && fila.VenceUtc == fila.CreadaUtc.AddDays(7) &&
        DateTime.Parse(ingreso.VenceEnUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) == fila.VenceUtc;
});
Prueba("servicio: desconocido, contraseña incorrecta, casing e inactivo rechazan sin mutar sesiones", () =>
{
    var total = sesiones.Filas.Count;
    var rechazadas = new[] { ("otro@example.invalid", "x"), ("prueba@example.invalid", "incorrecta"),
        ("Prueba@example.invalid", "x"), ("prueba@example.invalid", " x ") }
        .All(par => servicio.IngresarAsync(new SolicitudIngresoDto { Correo = par.Item1, Contrasena = par.Item2 }).GetAwaiter().GetResult() == null);
    cuentas.Usuario.Activo = false;
    rechazadas &= servicio.IngresarAsync(new SolicitudIngresoDto { Correo = cuentas.Usuario.Correo, Contrasena = "x" }).GetAwaiter().GetResult() == null;
    cuentas.Usuario.Activo = true;
    return rechazadas && sesiones.Filas.Count == total;
});
Prueba("servicio: relee rol autoritativo, no renueva ni modifica la sesión", () =>
{
    var antes = JsonSerializer.Serialize(sesiones.Filas);
    var pasajero = servicio.AutenticarAsync(ingreso!.Token).GetAwaiter().GetResult();
    cuentas.Roles = new[] { "Administrador" };
    var administrador = servicio.AutenticarAsync(ingreso.Token).GetAwaiter().GetResult();
    cuentas.Roles = new[] { "Pasajero" };
    return pasajero!.IsInRole("Pasajero") && !pasajero.IsInRole("Administrador") && administrador!.IsInRole("Administrador") &&
        !administrador.IsInRole("Pasajero") && JsonSerializer.Serialize(sesiones.Filas) == antes;
});
Prueba("servicio: token desconocido e inactivo rechazados", () =>
{
    var desconocida = servicio.AutenticarAsync(PoliticaSesion.NuevoToken()).GetAwaiter().GetResult() == null;
    cuentas.Usuario.Activo = false;
    var inactiva = servicio.AutenticarAsync(ingreso!.Token).GetAwaiter().GetResult() == null;
    cuentas.Usuario.Activo = true;
    return desconocida && inactiva;
});
Prueba("servicio: frontera exacta y posterior rechazan sin sliding", () =>
{
    var venceServicio = sesiones.Filas.Single().Value.VenceUtc;
    reloj.Ahora = venceServicio.AddTicks(-1);
    var previa = servicio.AutenticarAsync(ingreso!.Token).GetAwaiter().GetResult() != null;
    reloj.Ahora = venceServicio;
    var exacta = servicio.AutenticarAsync(ingreso.Token).GetAwaiter().GetResult() == null;
    reloj.Ahora = venceServicio.AddTicks(1);
    var posterior = servicio.AutenticarAsync(ingreso.Token).GetAwaiter().GetResult() == null;
    reloj.Ahora = ahora;
    return previa && exacta && posterior && sesiones.Filas.Single().Value.VenceUtc == venceServicio;
});
Prueba("servicio: logout revoca solo esa sesión; token posterior rechazado (doble manual, no DB)", () =>
{
    var segunda = servicio.IngresarAsync(new SolicitudIngresoDto { Correo = cuentas.Usuario.Correo, Contrasena = "x" }).GetAwaiter().GetResult()!;
    servicio.SalirAsync(ingreso!.Token).GetAwaiter().GetResult();
    return servicio.AutenticarAsync(ingreso.Token).GetAwaiter().GetResult() == null &&
        servicio.AutenticarAsync(segunda.Token).GetAwaiter().GetResult() != null &&
        sesiones.Filas[PoliticaSesion.Huella(segunda.Token)].RevocadaUtc == null;
});
Prueba("handler y autorización: challenge 401 Bearer, forbid 403 y roles (sin servidor)", () =>
{
    var coleccion = new ServiceCollection();
    coleccion.AddLogging();
    coleccion.AddRouting();
    coleccion.AgregarAutenticacionLocal(new ConfigurationBuilder().Build(), new Entorno());
    coleccion.AddSingleton<IAutenticacionService>(servicio);
    using var p = coleccion.BuildServiceProvider();
    DefaultHttpContext Contexto(string? token)
    {
        var ctx = new DefaultHttpContext { RequestServices = p.CreateScope().ServiceProvider };
        ctx.Request.Scheme = "https";
        if (token != null) ctx.Request.Headers.Authorization = "Bearer " + token;
        return ctx;
    }
    var anonimo = Contexto(null);
    var resultado = anonimo.AuthenticateAsync("Bearer").GetAwaiter().GetResult();
    anonimo.ChallengeAsync("Bearer").GetAwaiter().GetResult();
    if (resultado.Succeeded || anonimo.Response.StatusCode != 401 || anonimo.Response.Headers.WWWAuthenticate != "Bearer") return false;
    var desconocido = Contexto(PoliticaSesion.NuevoToken());
    if (desconocido.AuthenticateAsync("Bearer").GetAwaiter().GetResult().Succeeded) return false;
    var tokenValido = servicio.IngresarAsync(new SolicitudIngresoDto { Correo = cuentas.Usuario.Correo, Contrasena = "x" }).GetAwaiter().GetResult()!.Token;
    var valido = Contexto(tokenValido);
    var autenticado = valido.AuthenticateAsync("Bearer").GetAwaiter().GetResult();
    var autorizacion = valido.RequestServices.GetRequiredService<IAuthorizationService>();
    var permite = autorizacion.AuthorizeAsync(autenticado.Principal!, null,
        new AuthorizationPolicyBuilder("Bearer").RequireAuthenticatedUser().RequireRole("Pasajero").Build()).GetAwaiter().GetResult();
    var deniega = autorizacion.AuthorizeAsync(autenticado.Principal!, null,
        new AuthorizationPolicyBuilder("Bearer").RequireAuthenticatedUser().RequireRole("Administrador").Build()).GetAwaiter().GetResult();
    valido.ForbidAsync("Bearer").GetAwaiter().GetResult();
    return autenticado.Succeeded && permite.Succeeded && !deniega.Succeeded && valido.Response.StatusCode == 403;
});
Prueba("middleware: HTTP sensible rechazado sin redirigir ni ejecutar siguiente", () =>
{
    foreach (var ruta in new[] { "/api/autenticacion/ingreso", "/WeatherForecast" })
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = ruta;
        ctx.Request.Method = "POST";
        ctx.Request.Headers.Authorization = "Bearer " + new string('A', 43);
        ctx.Response.Body = new MemoryStream();
        var llamadas = 0;
        new LimiteAutenticacion(_ => { llamadas++; return Task.CompletedTask; }).InvokeAsync(ctx).GetAwaiter().GetResult();
        if (ctx.Response.StatusCode != 400 || llamadas != 0 || ctx.Response.Headers.ContainsKey("Location")) return false;
    }
    return true;
});
Prueba("middleware: HTTP scaffold sin Bearer continúa", () =>
{
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/WeatherForecast";
    var llamadas = 0;
    new LimiteAutenticacion(_ => { llamadas++; return Task.CompletedTask; }).InvokeAsync(ctx).GetAwaiter().GetResult();
    return llamadas == 1;
});
Prueba("middleware: excepción privada se convierte en 500 sanitizado", () =>
{
    var ctx = new DefaultHttpContext();
    ctx.Request.Scheme = "https";
    ctx.Request.Path = "/api/autenticacion/ingreso";
    ctx.Response.Body = new MemoryStream();
    new LimiteAutenticacion(_ => throw new Exception("detalle-privado-no-exponer")).InvokeAsync(ctx).GetAwaiter().GetResult();
    ctx.Response.Body.Position = 0;
    var json = new StreamReader(ctx.Response.Body).ReadToEnd();
    return ctx.Response.StatusCode == 500 &&
        ctx.Response.Headers.CacheControl.ToString() == "no-store" &&
        json == "{\"codigo\":\"autenticacionNoDisponible\",\"mensaje\":\"La autenticación no está disponible. Intentá nuevamente.\"}";
});
Console.WriteLine($"{pruebas - fallos}/{pruebas} comprobaciones unitarias PASS. No demuestra HTTP público, durabilidad ni revocación MySQL.");
return fallos == 0 ? 0 : 1;

class Entorno : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "AuthenticationTests";
    public string ApplicationName { get; set; } = "Unit";
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

class CuentasFicticias : ICuentasRepository
{
    public Usuario Usuario { get; } = new() { Identificador = "US-1", IdentificadorPublico = "publico-opaco",
        Correo = "prueba@example.invalid", Contrasena = "x", Activo = true };
    public string[] Roles { get; set; } = new[] { "Pasajero" };
    public Task<Usuario?> BuscarPorCorreoAsync(string correo) => Task.FromResult<Usuario?>(correo.Equals(Usuario.Correo, StringComparison.OrdinalIgnoreCase) ? Usuario : null);
    public Task<Usuario?> BuscarPorIdAsync(string id) => Task.FromResult<Usuario?>(id == Usuario.Identificador ? Usuario : null);
    public Task<string[]> ConsultarRolesAsync(string id) => Task.FromResult(Roles);
    public Task<Rol> CrearRolAsync(RolIngreso nombre) => throw new NotSupportedException();
    public Task<Usuario> CrearUsuarioAsync(string correo, string contrasena, string rolId) => throw new NotSupportedException();
}

class SesionesFicticias : ISesionesRepository
{
    public Dictionary<string, Sesion> Filas { get; } = new();
    public Task CrearAsync(Sesion sesion) { Filas.Add(sesion.Huella, sesion); return Task.CompletedTask; }
    public Task<Sesion?> BuscarAsync(string huella) => Task.FromResult(Filas.GetValueOrDefault(huella));
    public Task RevocarAsync(string huella, DateTime ahoraUtc)
    {
        if (Filas.TryGetValue(huella, out var sesion) && sesion.RevocadaUtc == null) sesion.RevocadaUtc = ahoraUtc;
        return Task.CompletedTask;
    }
}

class RelojMutable(DateTime ahora) : TimeProvider
{
    public DateTime Ahora { get; set; } = ahora;
    public override DateTimeOffset GetUtcNow() => new(Ahora);
}

class Reloj(DateTime ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(ahora);
}
