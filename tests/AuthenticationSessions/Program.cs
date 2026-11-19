using Microsoft.AspNetCore.Builder;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Globalization;
using System.Reflection;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using API_Berruti.Security.Authentication;

if (!args.SequenceEqual(new[] { "--unit" }))
{
    Console.Error.WriteLine("Uso: --unit. Solo políticas puras y pipeline en memoria; no escucha HTTP ni conecta MySQL.");
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
    catch (Exception exception) { fallos++; Console.Error.WriteLine($"FAIL: {nombre} ({exception.GetType().Name}: {exception.Message})"); }
}

// Clave sintética solo para pruebas: no es un secreto real ni se usa fuera de este proceso.
const string ClavePrueba = "clave-sintetica-solo-pruebas-0123456789-abcdef";
const string EmisorPrueba = "berruti-api-prueba";
const string AudienciaPrueba = "berruti-clientes-prueba";
IConfiguration ConfigJwt(string? clave, string? emisor = EmisorPrueba, string? audiencia = AudienciaPrueba) =>
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Berruti"] = "Server=localhost;Database=berrutiauthtests;User ID=berruti_test;Password=ficticia-unit-no-conexion",
        ["Jwt:Key"] = clave, ["Jwt:Issuer"] = emisor, ["Jwt:Audience"] = audiencia
    }).Build();

Prueba("S10 contratos específicos implementados y consumidos", () =>
{
    var assembly = typeof(PoliticaSesion).Assembly;
    foreach (var nombre in new[] { "Repositories.Authentication.CuentasRepository", "Repositories.Authentication.ContadoresRepository",
        "Services.Identifiers.Correlativo", "Services.Authentication.AutenticacionService" })
    {
        var clase = assembly.GetType("API_Berruti." + nombre);
        var contrato = assembly.GetType("API_Berruti." + nombre.Insert(nombre.LastIndexOf('.') + 1, "I"));
        if (clase == null || contrato == null || !contrato.IsInterface || !contrato.IsAssignableFrom(clase) || contrato.GetMethods().Length == 0) return false;
        if (clase.GetConstructors().SelectMany(c => c.GetParameters()).Any(p =>
            p.ParameterType.Namespace?.StartsWith("API_Berruti.Repositories") == true && !p.ParameterType.IsInterface)) return false;
    }
    return true;
});
Prueba("S3 sin estado: no quedan tipos de sesión de servidor ni handler opaco", () =>
{
    var assembly = typeof(PoliticaSesion).Assembly;
    return new[] { "API_Berruti.Models.Authentication.Sesion", "API_Berruti.Repositories.Authentication.ISesionesRepository",
        "API_Berruti.Repositories.Authentication.SesionesRepository", "API_Berruti.Security.Authentication.SesionBearerHandler" }
        .All(nombre => assembly.GetType(nombre) == null) &&
        typeof(IAutenticacionService).GetMethods().Select(m => m.Name).SequenceEqual(new[] { "IngresarAsync" });
});
Prueba("SA identifier uses the shared unpadded counter range and rejects exhaustion", () =>
{
    var correlativo = new Correlativo();
    var agotado = false;
    try { correlativo.Siguiente("SA-", 50000); }
    catch (InvalidOperationException) { agotado = true; }
    return correlativo.Siguiente("SA-", 0) == "SA-1" && correlativo.Siguiente("SA-", 8) == "SA-9" &&
        correlativo.Siguiente("SA-", 9) == "SA-10" && correlativo.Siguiente("SA-", 49999) == "SA-50000" && agotado;
});
Prueba("Salida inventory EF metadata maps composite and restrictive optional relationships without opening a connection", () =>
{
    var options = new DbContextOptionsBuilder<BerrutiDbContext>()
        .UseMySQL("Server=127.0.0.1;Database=berrutimodeltests;User ID=model_test;Password=unused-placeholder")
        .Options;
    using var context = new BerrutiDbContext(options);
    var salida = context.Model.FindEntityType("API_Berruti.Models.Operational.Salida");
    var asiento = context.Model.FindEntityType("API_Berruti.Models.Operational.AsientoSalida");
    var servicio = context.Model.FindEntityType(typeof(API_Berruti.Models.Operational.Servicio));
    var serviceForeignKey = servicio?.GetForeignKeys().SingleOrDefault(fk => fk.PrincipalEntityType == salida);
    var seatForeignKey = asiento?.GetForeignKeys().SingleOrDefault(fk => fk.PrincipalEntityType == salida);
    var key = asiento?.FindPrimaryKey()?.Properties.Select(p => p.Name).ToArray();
    return salida?.FindProperty("Fecha")?.GetColumnType() == "date" &&
        key?.SequenceEqual(new[] { "SalidaId", "NumeroAsiento" }) == true &&
        serviceForeignKey?.Properties.Single().Name == "SalidaId" &&
        serviceForeignKey.IsRequired == false && serviceForeignKey.DeleteBehavior == Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict &&
        seatForeignKey?.Properties.Single().Name == "SalidaId" &&
        seatForeignKey.DeleteBehavior == Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict &&
        context.Database.GetDbConnection().State == System.Data.ConnectionState.Closed;
});
Prueba("Departure stop and service bounds EF metadata is present without SQL CHECK execution or opening a connection", () =>
{
    var options = new DbContextOptionsBuilder<BerrutiDbContext>()
        .UseMySQL("Server=127.0.0.1;Database=berrutimodeltests;User ID=model_test;Password=unused-placeholder")
        .Options;
    using var context = new BerrutiDbContext(options);
    var designModel = context.GetService<IDesignTimeModel>().Model;
    var assembly = typeof(BerrutiDbContext).Assembly;
    var occurrenceType = assembly.GetType("API_Berruti.Models.Operational.ParadaSalida");
    if (occurrenceType is null) return false;
    var occurrence = designModel.FindEntityType(occurrenceType);
    var service = designModel.FindEntityType(typeof(API_Berruti.Models.Operational.Servicio));
    var stop = designModel.FindEntityType(typeof(API_Berruti.Models.Operational.Parada));
    var departure = designModel.FindEntityType(typeof(API_Berruti.Models.Operational.Salida));
    var occurrenceKey = occurrence?.FindPrimaryKey()?.Properties.Select(property => property.Name).ToArray();
    var occurrenceFks = occurrence?.GetForeignKeys().ToArray() ?? [];
    var boundProperties = new[] { "OrdenEmbarque", "OrdenDesembarque" };
    var serviceFks = service?.GetForeignKeys().Where(fk => fk.PrincipalEntityType == occurrence).ToArray() ?? [];
    var check = service?.GetCheckConstraints().SingleOrDefault(constraint => constraint.Name == "ck_servicio_rango_paradas");
    var outcome = occurrence is not null && service is not null && occurrence.GetTableName() == "parada_salida" &&
        occurrenceKey?.SequenceEqual(new[] { "SalidaId", "Orden" }) == true &&
        occurrence.GetCheckConstraints().Any(constraint => constraint.Sql.Contains("orden >= 1")) &&
        occurrenceFks.Any(fk => fk.PrincipalEntityType == departure && fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "SalidaId" }) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
        occurrenceFks.Any(fk => fk.PrincipalEntityType == stop && fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "ParadaId" }) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
        occurrence.FindProperty("ParadaId")?.GetColumnType() == stop?.FindProperty("Id")?.GetColumnType() &&
        !occurrence.GetIndexes().Any(index => index.IsUnique && index.Properties.Any(property => property.Name == "ParadaId")) &&
        boundProperties.All(name => service?.FindProperty(name)?.ClrType == typeof(int?) && service.FindProperty(name)?.IsNullable == true) &&
        service.FindProperty("OrdenEmbarque")?.GetColumnName() == "orden_embarque" &&
        service.FindProperty("OrdenDesembarque")?.GetColumnName() == "orden_desembarque" &&
        serviceFks.Length == 2 &&
        serviceFks.Any(fk => fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "SalidaId", "OrdenEmbarque" }) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
        serviceFks.Any(fk => fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "SalidaId", "OrdenDesembarque" }) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
        service.GetForeignKeys().Count(fk => fk.Properties.Any(property => property.Name is "OrdenEmbarque" or "OrdenDesembarque")) == 2 &&
        check is not null && check.Sql.Contains("IS NOT NULL") && check.Sql.Contains("orden_embarque < orden_desembarque") &&
        context.Database.GetDbConnection().State == System.Data.ConnectionState.Closed;
    return outcome;
});
Prueba("EF tracking allows repeated stop occurrences and separate departures, then rejects a duplicate tracked key", () =>
{
    var options = new DbContextOptionsBuilder<BerrutiDbContext>()
        .UseMySQL("Server=127.0.0.1;Database=berrutimodeltests;User ID=model_test;Password=unused-placeholder")
        .Options;
    using var context = new BerrutiDbContext(options);
    var repeatedStop = new API_Berruti.Models.Operational.ParadaSalida { SalidaId = "SA-1", Orden = 1, ParadaId = "PA-1" };
    var repeatedStopLater = new API_Berruti.Models.Operational.ParadaSalida { SalidaId = "SA-1", Orden = 2, ParadaId = "PA-1" };
    var separateDeparture = new API_Berruti.Models.Operational.ParadaSalida { SalidaId = "SA-2", Orden = 1, ParadaId = "PA-2" };
    context.AddRange(repeatedStop, repeatedStopLater, separateDeparture);
    var tracked = context.ChangeTracker.Entries<API_Berruti.Models.Operational.ParadaSalida>()
        .Select(entry => entry.Entity).ToArray();
    var allowedOccurrencesTracked = tracked.Length == 3 && tracked.Select(item => (item.SalidaId, item.Orden)).Distinct().Count() == 3 &&
        tracked.Count(item => item.ParadaId == "PA-1") == 2;

    using var duplicateContext = new BerrutiDbContext(options);
    duplicateContext.Add(new API_Berruti.Models.Operational.ParadaSalida { SalidaId = "SA-1", Orden = 1, ParadaId = "PA-1" });
    var duplicateTrackedKeyRejected = false;
    try
    {
        duplicateContext.Add(new API_Berruti.Models.Operational.ParadaSalida { SalidaId = "SA-1", Orden = 1, ParadaId = "PA-2" });
    }
    catch (InvalidOperationException) { duplicateTrackedKeyRejected = true; }

    return allowedOccurrencesTracked && duplicateTrackedKeyRejected &&
        context.Database.GetDbConnection().State == System.Data.ConnectionState.Closed &&
        duplicateContext.Database.GetDbConnection().State == System.Data.ConnectionState.Closed;
});
Prueba("identificador público: 32 bytes Base64url canónicos y distintos", () =>
{
    var tokens = Enumerable.Range(0, 128).Select(_ => PoliticaSesion.NuevoToken()).ToArray();
    return tokens.Distinct().Count() == 128 && tokens.All(t => t.Length == 43 && !t.Any(c => c is '+' or '/' or '=') &&
        Convert.FromBase64String(t.Replace('-', '+').Replace('_', '/') + "=").Length == 32);
});
var ahora = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
Prueba("reloj UTC conserva microsegundos MySQL", () => PoliticaSesion.AhoraUtc(new Reloj(ahora.AddTicks(1234567))) == ahora.AddTicks(1234560));
Prueba("roles de servidor: valores del enum, sin duplicados", () =>
    PoliticaSesion.Roles(new[] { "Pasajero", "Pasajero", "Administrador" }).SequenceEqual(new[] { RolIngreso.Pasajero, RolIngreso.Administrador }));
Prueba("rol desconocido falla cerrado", () =>
{
    try { PoliticaSesion.Roles(new[] { "pasajero", "999" }); return false; }
    catch (InvalidOperationException) { return true; }
});
foreach (var encabezado in new[] { "Bearer", "bEaReR abc", "Bearer\tabc" })
    Prueba("encabezado con esquema Bearer es sensible para HTTPS", () => EncabezadoBearer.EsBearer(encabezado));
foreach (var encabezado in new[] { "", "Basic abc", "Bearerabc" })
    Prueba("encabezado sin esquema Bearer no es sensible", () => !EncabezadoBearer.EsBearer(encabezado));

var configuracion = ConfigJwt(ClavePrueba);
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
    object? Campo(object obj, string nombre) => obj.GetType().GetField(nombre,
        BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
    return cuentasDi is CuentasRepository && contadoresDi is ContadoresRepository &&
        sp.GetRequiredService<ICorrelativo>() is Correlativo && sp.GetRequiredService<IAutenticacionService>() is AutenticacionService &&
        ReferenceEquals(Campo(cuentasDi, "_contexto"), db) && ReferenceEquals(Campo(contadoresDi, "_contexto"), db) &&
        ReferenceEquals(Campo(cuentasDi, "_contadores"), contadoresDi);
});
Prueba("DI: hasher estándar de Identity para Usuario y sin tabla sesion en el modelo", () =>
{
    var sp = alcance.ServiceProvider;
    var db = sp.GetRequiredService<BerrutiDbContext>();
    return sp.GetRequiredService<IPasswordHasher<Usuario>>() is PasswordHasher<Usuario> &&
        db.Model.GetEntityTypes().All(t => t.GetTableName() != "sesion") &&
        typeof(BerrutiDbContext).GetProperties().All(p => p.Name != "Sesiones");
});
Prueba("contador público mantiene guardia transaccional antes de SQL", () =>
{
    try { alcance.ServiceProvider.GetRequiredService<IContadoresRepository>().IncrementarAsync("US-").GetAwaiter().GetResult(); return false; }
    catch (InvalidOperationException e) { return e.Message == "El alta requiere una transacción."; }
});
Prueba("configuración ausente no impide construir DI ni resolver el esquema Bearer", () =>
{
    var sinDb = new ServiceCollection();
    sinDb.AddLogging();
    sinDb.AgregarAutenticacionLocal(new ConfigurationBuilder().Build(), new Entorno());
    using var p = sinDb.BuildServiceProvider();
    var esquema = p.GetRequiredService<IAuthenticationSchemeProvider>().GetDefaultAuthenticateSchemeAsync().GetAwaiter().GetResult();
    return esquema?.Name == "Bearer";
});
foreach (var cadena in new[] { "", "Server=remote;Database=berrutiauthtests;User ID=berruti_test;Password=ficticia",
    "Server=localhost;Database=berrutiauthtests;User ID=root;Password=ficticia",
    "Server=localhost;Database=berrutilocal;User ID=berruti_test;Password=ficticia" })
    Prueba("configuración fuera del par local falla cerrada", () =>
    {
        try { ConfiguracionAutenticacion.ValidarConexion(cadena, "AuthenticationTests"); return false; }
        catch { return true; }
    });

// S8: la clave JWT viene de configuración externa; falta o longitud menor a 32 bytes detienen el arranque.
foreach (var (nombre, clave, emisor, audiencia) in new (string, string?, string?, string?)[]
{
    ("clave ausente", null, EmisorPrueba, AudienciaPrueba), ("clave vacía", "", EmisorPrueba, AudienciaPrueba),
    ("clave de 31 bytes", new string('k', 31), EmisorPrueba, AudienciaPrueba),
    ("emisor ausente", new string('k', 40), null, AudienciaPrueba),
    ("audiencia en blanco", new string('k', 40), EmisorPrueba, "  ") })
    Prueba($"ConfiguracionJwt rechaza {nombre} sin exponer la clave", () =>
    {
        try { ConfiguracionJwt.Leer(ConfigJwt(clave, emisor, audiencia)); return false; }
        catch (InvalidOperationException e) { return e.Message.Contains("Jwt") && (string.IsNullOrEmpty(clave) || !e.Message.Contains(clave)); }
    });
Prueba("ConfiguracionJwt acepta clave de exactamente 32 bytes", () =>
    ConfiguracionJwt.Leer(ConfigJwt(new string('k', 32))).Emisor == EmisorPrueba);

// Arranque real del proceso de la API sin Jwt__Key válida: termina con código 2 y un mensaje claro, sin escuchar HTTP.
(int Codigo, string Salida, string Error, bool Vencio) ArrancarApi(string? clave)
{
    var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    var configuracionBuild = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
    var carpeta = Path.Combine(raiz, "src/Api/API-Berruti/API-Berruti/bin", configuracionBuild, "net10.0");
    var info = new ProcessStartInfo("dotnet") { WorkingDirectory = carpeta, RedirectStandardOutput = true,
        RedirectStandardError = true, UseShellExecute = false };
    info.ArgumentList.Add(Path.Combine(carpeta, "API-Berruti.dll"));
    info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    info.Environment["ASPNETCORE_URLS"] = "https://127.0.0.1:0";
    info.Environment.Remove("Jwt__Key");
    info.Environment.Remove("ConnectionStrings__Berruti");
    if (clave != null) info.Environment["Jwt__Key"] = clave;
    using var proceso = Process.Start(info)!;
    var salida = proceso.StandardOutput.ReadToEndAsync();
    var error = proceso.StandardError.ReadToEndAsync();
    var vencio = !proceso.WaitForExit(60000);
    if (vencio) proceso.Kill(true);
    proceso.WaitForExit();
    return (proceso.ExitCode, salida.GetAwaiter().GetResult(), error.GetAwaiter().GetResult(), vencio);
}
foreach (var (nombre, clave) in new (string, string?)[] { ("ausente", null), ("de 31 bytes", new string('k', 31)) })
    Prueba($"arranque normal de la API falla claro con Jwt__Key {nombre}", () =>
    {
        var r = ArrancarApi(clave);
        return !r.Vencio && r.Codigo == 2 && r.Error.Contains("Jwt__Key") && (clave == null || !(r.Error + r.Salida).Contains(clave));
    });

// Servicio con hasher real, emisor real y doble de cuentas.
var hasher = new PasswordHasher<Usuario>();
var cuentas = new CuentasFicticias(hasher, "x");
var reloj = new RelojMutable(DateTime.UtcNow.AddTicks(1234567));
IAutenticacionService CrearServicio(IPasswordHasher<Usuario>? otro = null, TimeProvider? relojServicio = null) =>
    new AutenticacionService(cuentas, otro ?? hasher, new EmisorJwt(configuracion), relojServicio ?? reloj);
var servicio = CrearServicio();
SolicitudIngresoDto Solicitud(string correo, string clave) => new() { Correo = correo, Contrasena = clave };
RespuestaIngresoDto? Ingresar(IAutenticacionService s, string correo, string clave) =>
    s.IngresarAsync(Solicitud(correo, clave)).GetAwaiter().GetResult();
Prueba("T1 hash: la contraseña guardada no es el texto plano y verifica con el hasher estándar", () =>
{
    var guardada = cuentas.Usuario.Contrasena;
    return guardada != "x" && guardada.Length > 40 &&
        hasher.VerifyHashedPassword(cuentas.Usuario, guardada, "x") == PasswordVerificationResult.Success &&
        hasher.VerifyHashedPassword(cuentas.Usuario, guardada, "incorrecta") == PasswordVerificationResult.Failed;
});
RespuestaIngresoDto? ingreso = null;
Prueba("T2 ingreso: respuesta conserva contrato {identificadorUsuario, roles, token, venceEnUtc} con JWT", () =>
{
    ingreso = Ingresar(servicio, "prueba@example.invalid", "x");
    return ingreso != null && ingreso.IdentificadorUsuario == "publico-opaco" &&
        ingreso.Roles.SequenceEqual(new[] { RolIngreso.Pasajero }) && ingreso.Token.Split('.').Length == 3 &&
        ingreso.VenceEnUtc.Length == 27 && ingreso.VenceEnUtc.EndsWith(".000000Z") &&
        JsonSerializer.Serialize(ingreso, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false) } })
        .StartsWith("{\"identificadorUsuario\":\"publico-opaco\",\"roles\":[\"pasajero\"],\"token\":\"eyJ");
});
Prueba("T2 JWT: HS256, sub = identificador público, un claim de rol, jti Guid y nada personal", () =>
{
    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(ingreso!.Token);
    var claves = jwt.Payload.Keys.OrderBy(k => k).ToArray();
    var esperadas = new[] { "aud", "exp", "iss", "jti", "sub", ClaimTypes.Role }.OrderBy(k => k).ToArray();
    var cargaUtil = ingreso.Token.Split('.')[1];
    var carga = Encoding.UTF8.GetString(Convert.FromBase64String(cargaUtil.Replace('-', '+').Replace('_', '/')
        .PadRight((cargaUtil.Length + 3) / 4 * 4, '=')));
    return jwt.Header.Alg == SecurityAlgorithms.HmacSha256 && claves.SequenceEqual(esperadas) &&
        jwt.Subject == "publico-opaco" && jwt.Subject != cuentas.Usuario.Identificador &&
        jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value == "Pasajero" &&
        Guid.TryParse(jwt.Id, out _) && jwt.Issuer == EmisorPrueba && jwt.Audiences.Single() == AudienciaPrueba &&
        !carga.Contains(cuentas.Usuario.Correo) && !carga.Contains("US-1") && !carga.Contains('@');
});
Prueba("T2 vigencia: siete días en segundos enteros, exp coincide con venceEnUtc", () =>
{
    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(ingreso!.Token);
    var vence = DateTime.Parse(ingreso.VenceEnUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    var inicio = PoliticaSesion.AhoraUtc(reloj);
    return vence.Ticks % TimeSpan.TicksPerSecond == 0 && vence <= inicio.AddDays(7) && vence > inicio.AddDays(7).AddSeconds(-1) &&
        new DateTimeOffset(vence).ToUnixTimeSeconds() == long.Parse(jwt.Payload["exp"].ToString()!, CultureInfo.InvariantCulture) &&
        jwt.ValidTo == vence && ingreso.VenceEnUtc == vence.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);
});
Prueba("T2 jti distinto en cada ingreso", () =>
{
    var otro = Ingresar(servicio, "prueba@example.invalid", "x")!;
    return new JwtSecurityTokenHandler().ReadJwtToken(otro.Token).Id != new JwtSecurityTokenHandler().ReadJwtToken(ingreso!.Token).Id;
});
Prueba("T1 rechazo: desconocido, contraseña incorrecta, casing e inactivo devuelven null sin mutar la cuenta", () =>
{
    var antes = JsonSerializer.Serialize(cuentas.Usuario);
    var rechazadas = new[] { ("otro@example.invalid", "x"), ("prueba@example.invalid", "incorrecta"),
        ("Prueba@example.invalid", "x"), ("prueba@example.invalid", " x ") }
        .All(par => Ingresar(servicio, par.Item1, par.Item2) == null);
    cuentas.Usuario.Activo = false;
    rechazadas &= Ingresar(servicio, cuentas.Usuario.Correo, "x") == null;
    cuentas.Usuario.Activo = true;
    return rechazadas && JsonSerializer.Serialize(cuentas.Usuario) == antes;
});
Prueba("T1 una contraseña legible guardada antes del hash ya no autentica", () =>
{
    var guardada = cuentas.Usuario.Contrasena;
    try
    {
        // "x" no es Base64 (el hasher lanza FormatException); "abcd" sí lo es pero no es un hash válido.
        foreach (var legible in new[] { "x", "abcd", "" })
        {
            cuentas.Usuario.Contrasena = legible;
            if (Ingresar(servicio, cuentas.Usuario.Correo, legible.Length == 0 ? "x" : legible) != null) return false;
        }
        return true;
    }
    finally { cuentas.Usuario.Contrasena = guardada; }
});
Prueba("T1 SuccessRehashNeeded cuenta como éxito; Failed rechaza", () =>
    Ingresar(CrearServicio(new HasherFijo(PasswordVerificationResult.SuccessRehashNeeded)), cuentas.Usuario.Correo, "cualquiera") != null &&
    Ingresar(CrearServicio(new HasherFijo(PasswordVerificationResult.Failed)), cuentas.Usuario.Correo, "x") == null &&
    Ingresar(CrearServicio(new HasherFijo(PasswordVerificationResult.Success)), cuentas.Usuario.Correo, "otra") != null);
Prueba("T1 controlador: cualquier rechazo produce el mismo 401, cuerpo y WWW-Authenticate", () =>
{
    string[] Rechazo(string correo, string clave, bool activo)
    {
        cuentas.Usuario.Activo = activo;
        try
        {
            var controlador = new API_Berruti.Controllers.AuthenticationController(servicio)
                { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            var resultado = controlador.Ingresar(Solicitud(correo, clave)).GetAwaiter().GetResult().Result as UnauthorizedObjectResult;
            return new[] { resultado!.StatusCode.ToString()!, JsonSerializer.Serialize(resultado.Value)!,
                controlador.Response.Headers.WWWAuthenticate.ToString() };
        }
        finally { cuentas.Usuario.Activo = true; }
    }
    var referencia = Rechazo("otro@example.invalid", "x", true);
    return referencia[0] == "401" && referencia[2] == "Bearer" &&
        referencia[1] == "{\"Codigo\":\"ingresoRechazado\",\"Mensaje\":\"No se pudo ingresar con las credenciales proporcionadas.\"}" &&
        Rechazo("prueba@example.invalid", "incorrecta", true).SequenceEqual(referencia) &&
        Rechazo("prueba@example.invalid", "x", false).SequenceEqual(referencia);
});

// Pipeline real: LimiteAutenticacion + UseAuthentication + UseAuthorization con el esquema JwtBearer registrado.
using var proveedorPipeline = ConstruirProveedor(configuracion);
ServiceProvider ConstruirProveedor(IConfiguration config)
{
    var coleccion = new ServiceCollection();
    coleccion.AddLogging();
    coleccion.AddRouting();
    coleccion.AgregarAutenticacionLocal(config, new Entorno());
    return coleccion.BuildServiceProvider();
}
(int Estado, string Autenticar, string Cache, string Cuerpo, int Invocaciones) Pipeline(
    string metodoControlador, string ruta, string verbo, string? encabezado, bool https = true)
{
    var metodo = typeof(API_Berruti.Controllers.AuthenticationController).GetMethod(metodoControlador)!;
    var metadatos = metodo.GetCustomAttributes(true);
    var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    opciones.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
    using var scope = proveedorPipeline.CreateScope();
    var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    ctx.Request.Scheme = https ? "https" : "http";
    ctx.Request.Path = "/api/autenticacion/" + ruta;
    ctx.Request.Method = verbo;
    ctx.Response.Body = new MemoryStream();
    if (encabezado != null) ctx.Request.Headers.Authorization = encabezado;
    var invocaciones = 0;
    ctx.SetEndpoint(new Endpoint(async context =>
    {
        invocaciones++;
        var controlador = new API_Berruti.Controllers.AuthenticationController(servicio)
            { ControllerContext = new ControllerContext { HttpContext = context } };
        var resultado = metodo.Invoke(controlador, null);
        if (resultado is ActionResult<RespuestaSesionDto> ok)
            await context.Response.WriteAsync(JsonSerializer.Serialize(((OkObjectResult)ok.Result!).Value, opciones));
        else if (resultado is NoContentResult) context.Response.StatusCode = 204;
        else throw new InvalidOperationException();
    }, new EndpointMetadataCollection(metadatos), "sonda"));
    var app = new ApplicationBuilder(proveedorPipeline);
    UseMiddlewareExtensions.UseMiddleware<LimiteAutenticacion>(app);
    AuthAppBuilderExtensions.UseAuthentication(app);
    AuthorizationAppBuilderExtensions.UseAuthorization(app);
    app.Run(context => context.GetEndpoint()!.RequestDelegate!(context));
    app.Build()(ctx).GetAwaiter().GetResult();
    ctx.Response.Body.Position = 0;
    return (ctx.Response.StatusCode, ctx.Response.Headers.WWWAuthenticate.ToString(),
        ctx.Response.Headers.CacheControl.ToString(), new StreamReader(ctx.Response.Body).ReadToEnd(), invocaciones);
}
string Forjar(string clave, string emisor, string audiencia, string[] roles, DateTime vence, string sub = "publico-opaco")
{
    var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, sub), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
    var credenciales = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)), SecurityAlgorithms.HmacSha256);
    return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(emisor, audiencia, claims, null, vence, credenciales));
}
string TokenEmitido(params string[] roles)
{
    var anterior = cuentas.Roles;
    cuentas.Roles = roles;
    try { return Ingresar(servicio, cuentas.Usuario.Correo, "x")!.Token; }
    finally { cuentas.Roles = anterior; }
}
string Alterar(string token)
{
    var partes = token.Split('.');
    var relleno = partes[1].Replace('-', '+').Replace('_', '/').PadRight((partes[1].Length + 3) / 4 * 4, '=');
    var json = Encoding.UTF8.GetString(Convert.FromBase64String(relleno)).Replace("Pasajero", "Administrador");
    partes[1] = Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    return string.Join('.', partes);
}
const string CuerpoPasajero = "{\"identificadorUsuario\":\"publico-opaco\",\"roles\":[\"pasajero\"]}";
Prueba("T2 token emitido por el login atraviesa el pipeline: 200, sub como NameIdentifier y rol", () =>
{
    var r = Pipeline("PruebaPasajero", "prueba-pasajero", "GET", "Bearer " + TokenEmitido("Pasajero"));
    var identidad = Pipeline("Identidad", "identidad", "GET", "Bearer " + TokenEmitido("Pasajero"));
    return r == (200, "", "no-store", CuerpoPasajero, 1) && identidad == (200, "", "no-store", CuerpoPasajero, 1);
});
Prueba("T2 control: token forjado con los parámetros correctos también se acepta (aísla cada rechazo)", () =>
    Pipeline("PruebaPasajero", "prueba-pasajero", "GET",
        "Bearer " + Forjar(ClavePrueba, EmisorPrueba, AudienciaPrueba, new[] { "Pasajero" }, DateTime.UtcNow.AddHours(1))).Estado == 200);
Prueba("S8 roles múltiples del servidor llegan como claims de rol (Cobrador y Pasajero)", () =>
{
    var token = TokenEmitido("Pasajero", "Cobrador");
    return Pipeline("PruebaCobrador", "prueba-cobrador", "GET", "Bearer " + token).Estado == 200 &&
        Pipeline("PruebaPasajero", "prueba-pasajero", "GET", "Bearer " + token).Cuerpo ==
        "{\"identificadorUsuario\":\"publico-opaco\",\"roles\":[\"pasajero\",\"cobrador\"]}";
});
Prueba("S8 token válido sin el rol requerido: 403 sin WWW-Authenticate ni ejecutar la acción", () =>
{
    var token = "Bearer " + TokenEmitido("Pasajero");
    return Pipeline("PruebaAdministrador", "prueba-administrador", "GET", token) == (403, "", "no-store", "", 0) &&
        Pipeline("PruebaCobrador", "prueba-cobrador", "GET", token) == (403, "", "no-store", "", 0);
});
var tokenAdmin = TokenEmitido("Administrador");
(string Nombre, string? Encabezado)[] invalidos =
{
    ("sin encabezado", null),
    ("esquema distinto de Bearer", "Basic " + tokenAdmin),
    ("Bearer sin token", "Bearer "),
    ("texto que no es JWT", "Bearer abc"),
    ("token opaco de 43 caracteres del esquema anterior", "Bearer " + PoliticaSesion.NuevoToken()),
    ("clave de firma distinta", "Bearer " + Forjar(new string('z', 48), EmisorPrueba, AudienciaPrueba, new[] { "Administrador" }, DateTime.UtcNow.AddHours(1))),
    ("emisor distinto", "Bearer " + Forjar(ClavePrueba, "otro-emisor", AudienciaPrueba, new[] { "Administrador" }, DateTime.UtcNow.AddHours(1))),
    ("audiencia distinta", "Bearer " + Forjar(ClavePrueba, EmisorPrueba, "otra-audiencia", new[] { "Administrador" }, DateTime.UtcNow.AddHours(1))),
    ("vencido más allá de la tolerancia", "Bearer " + Forjar(ClavePrueba, EmisorPrueba, AudienciaPrueba, new[] { "Administrador" }, DateTime.UtcNow.AddMinutes(-10))),
    ("emitido por el servicio hace más de siete días", "Bearer " + Ingresar(CrearServicio(null, new RelojMutable(DateTime.UtcNow.AddDays(-8))),
        cuentas.Usuario.Correo, "x")!.Token),
    ("sin firma (alg none)", "Bearer " + string.Join('.', new[] { "{\"alg\":\"none\",\"typ\":\"JWT\"}",
        "{\"sub\":\"publico-opaco\",\"" + ClaimTypes.Role + "\":\"Administrador\",\"exp\":" + DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() +
        ",\"iss\":\"" + EmisorPrueba + "\",\"aud\":\"" + AudienciaPrueba + "\"}" }
        .Select(parte => Convert.ToBase64String(Encoding.UTF8.GetBytes(parte)).TrimEnd('=').Replace('+', '-').Replace('/', '_'))) + "."),
    ("payload alterado a Administrador con la firma anterior", "Bearer " + Alterar(TokenEmitido("Pasajero")))
};
foreach (var (nombre, encabezado) in invalidos)
    Prueba($"S8 {nombre}: 401 con WWW-Authenticate Bearer exacto, no-store y sin ejecutar la acción", () =>
        Pipeline("PruebaAdministrador", "prueba-administrador", "GET", encabezado) == (401, "Bearer", "no-store", "", 0));
Prueba("S8 el payload alterado sí contiene el rol que el atacante quería (la prueba anterior es real)", () =>
    new JwtSecurityTokenHandler().ReadJwtToken(Alterar(TokenEmitido("Pasajero"))).Claims
        .Any(c => c.Type == ClaimTypes.Role && c.Value == "Administrador"));
Prueba("S8 HTTP con Bearer se rechaza antes de autenticar (400 httpsRequerido)", () =>
{
    var r = Pipeline("PruebaPasajero", "prueba-pasajero", "GET", "Bearer " + TokenEmitido("Pasajero"), https: false);
    return r.Estado == 400 && r.Invocaciones == 0 && r.Cache == "no-store" && r.Cuerpo.Contains("httpsRequerido");
});
Prueba("S3 salida: 204 sin cuerpo; sin token 401; el JWT sigue válido hasta exp (sin estado de servidor)", () =>
{
    var token = "Bearer " + TokenEmitido("Pasajero");
    var salida = Pipeline("Salir", "salida", "POST", token);
    var despues = Pipeline("PruebaPasajero", "prueba-pasajero", "GET", token);
    return salida == (204, "", "no-store", "", 1) && despues.Estado == 200 &&
        Pipeline("Salir", "salida", "POST", null) == (401, "Bearer", "no-store", "", 0);
});
Prueba("S3 salida no escribe ni lee estado: el controlador no usa el servicio de autenticación", () =>
{
    var servicioEspia = new ServicioEspia();
    var controlador = new API_Berruti.Controllers.AuthenticationController(servicioEspia);
    return controlador.Salir() is NoContentResult && servicioEspia.Llamadas == 0 &&
        typeof(API_Berruti.Controllers.AuthenticationController).GetMethod("Salir")!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Length == 1;
});
foreach (var sonda in new[] { (Metodo: "PruebaCobrador", Ruta: "prueba-cobrador", Rol: "Cobrador", Json: "cobrador"),
    (Metodo: "PruebaAdministrador", Ruta: "prueba-administrador", Rol: "Administrador", Json: "administrador") })
Prueba($"{sonda.Ruta}: solo su rol obtiene 200 con JWT real; otros 403; anónimo 401", () =>
{
    foreach (var rol in new[] { "Cobrador", "Pasajero", "Administrador" })
    {
        var esperado = rol == sonda.Rol ? (200, "", "no-store", $"{{\"identificadorUsuario\":\"publico-opaco\",\"roles\":[\"{sonda.Json}\"]}}", 1)
            : (403, "", "no-store", "", 0);
        if (Pipeline(sonda.Metodo, sonda.Ruta, "GET", "Bearer " + TokenEmitido(rol)) != esperado) return false;
    }
    return Pipeline(sonda.Metodo, sonda.Ruta, "GET", null) == (401, "Bearer", "no-store", "", 0);
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
Prueba("FIN-012 el DbContext registra el interceptor de espera de bloqueo", () =>
{
    var db = alcance.ServiceProvider.GetRequiredService<BerrutiDbContext>();
    var nucleo = ((Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>)db).Instance
        .GetRequiredService<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions>()
        .FindExtension<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>();
    return nucleo!.Interceptors!.OfType<EsperaBloqueoInterceptor>().Count() == 1 &&
        EsperaBloqueoInterceptor.Sentencia == "SET SESSION innodb_lock_wait_timeout = 3";
});
Prueba("FIN-012 al abrir una conexión (sync y async) se fija la espera de bloqueo por debajo de 5 s", () =>
{
    var conexion = new ConexionRegistradora();
    var interceptor = new EsperaBloqueoInterceptor();
    interceptor.ConnectionOpened(conexion, null!);
    interceptor.ConnectionOpenedAsync(conexion, null!).GetAwaiter().GetResult();
    return conexion.Sentencias.SequenceEqual(new[] { EsperaBloqueoInterceptor.Sentencia, EsperaBloqueoInterceptor.Sentencia }) &&
        ConfiguracionAutenticacion.ValidarConexion(
            "Server=localhost;Database=berrutiauthtests;User ID=berruti_test;Password=ficticia", "AuthenticationTests")
            .DefaultCommandTimeout > 3;
});
Console.WriteLine($"{pruebas - fallos}/{pruebas} comprobaciones unitarias PASS. No demuestra HTTP público ni MySQL.");
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
    public CuentasFicticias(IPasswordHasher<Usuario> hasher, string contrasena)
    {
        Usuario = new() { Identificador = "US-1", IdentificadorPublico = "publico-opaco",
            Correo = "prueba@example.invalid", Activo = true };
        Usuario.Contrasena = hasher.HashPassword(Usuario, contrasena);
    }
    public Usuario Usuario { get; }
    public string[] Roles { get; set; } = new[] { "Pasajero" };
    public Task<Usuario?> BuscarPorCorreoAsync(string correo) => Task.FromResult<Usuario?>(correo.Equals(Usuario.Correo, StringComparison.OrdinalIgnoreCase) ? Usuario : null);
    public Task<Usuario?> BuscarPorIdAsync(string id) => Task.FromResult<Usuario?>(id == Usuario.Identificador ? Usuario : null);
    public Task<string[]> ConsultarRolesAsync(string id) => Task.FromResult(Roles);
    public Task<Rol> CrearRolAsync(RolIngreso nombre) => throw new NotSupportedException();
    public Task<Usuario> CrearUsuarioAsync(string correo, string contrasena, string rolId) => throw new NotSupportedException();
}

class HasherFijo(PasswordVerificationResult resultado) : IPasswordHasher<Usuario>
{
    public string HashPassword(Usuario usuario, string contrasena) => throw new NotSupportedException();
    public PasswordVerificationResult VerifyHashedPassword(Usuario usuario, string contrasenaGuardada, string contrasenaIngresada) => resultado;
}

class ServicioEspia : IAutenticacionService
{
    public int Llamadas { get; private set; }
    public Task<RespuestaIngresoDto?> IngresarAsync(SolicitudIngresoDto solicitud) { Llamadas++; return Task.FromResult<RespuestaIngresoDto?>(null); }
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

class ConexionRegistradora : System.Data.Common.DbConnection
{
    public List<string> Sentencias { get; } = new();
    [System.Diagnostics.CodeAnalysis.AllowNull] public override string ConnectionString { get; set; } = "";
    public override string Database => "";
    public override string DataSource => "";
    public override string ServerVersion => "";
    public override System.Data.ConnectionState State => System.Data.ConnectionState.Open;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override System.Data.Common.DbCommand CreateDbCommand() => new ComandoRegistrador(Sentencias);
}

class ComandoRegistrador(List<string> sentencias) : System.Data.Common.DbCommand
{
    [System.Diagnostics.CodeAnalysis.AllowNull] public override string CommandText { get; set; } = "";
    public override int CommandTimeout { get; set; }
    public override System.Data.CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override System.Data.UpdateRowSource UpdatedRowSource { get; set; }
    protected override System.Data.Common.DbConnection? DbConnection { get; set; }
    protected override System.Data.Common.DbParameterCollection DbParameterCollection => throw new NotSupportedException();
    protected override System.Data.Common.DbTransaction? DbTransaction { get; set; }
    public override void Cancel() { }
    public override int ExecuteNonQuery() { sentencias.Add(CommandText); return 0; }
    public override object? ExecuteScalar() => throw new NotSupportedException();
    public override void Prepare() { }
    protected override System.Data.Common.DbParameter CreateDbParameter() => throw new NotSupportedException();
    protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) => throw new NotSupportedException();
}
