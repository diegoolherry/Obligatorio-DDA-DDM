using System.Reflection;
using System.Security.Claims;
using API_Berruti.Controllers.Location;
using API_Berruti.DTOs.Location;
using API_Berruti.Models.Location;
using API_Berruti.Repositories.Location;
using API_Berruti.Services.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// Pruebas unitarias de MVP-009: servicio de ubicación con repositorio en memoria y controlador sin HTTP ni MySQL.
var fallos = 0;
var pruebas = 0;
async Task Prueba(string nombre, Func<Task<bool>> comprobar)
{
    pruebas++;
    try
    {
        if (!await comprobar()) throw new InvalidOperationException("comprobación falsa");
        Console.WriteLine($"PASS: {nombre}");
    }
    catch (Exception e) { fallos++; Console.Error.WriteLine($"FAIL: {nombre} ({e.GetType().Name}: {e.Message})"); }
}

var configuracionDemo = new Dictionary<string, string?>
{
    ["Ubicacion:PeriodicidadSegundos"] = "30",
    ["Ubicacion:MargenPeriodicidadSegundos"] = "10",
    ["Ubicacion:UmbralDesactualizacionSegundos"] = "120",
    ["Ubicacion:VelocidadPromedioKmH"] = "60",
    ["Ubicacion:ToleranciaDemoraMinutos"] = "5",
    ["Ubicacion:ZonaHoraria"] = "America/Montevideo",
    ["Ubicacion:Destinos:Colonia:Latitud"] = "-34.4626",
    ["Ubicacion:Destinos:Colonia:Longitud"] = "-57.8400",
};
ParametrosUbicacion Parametros(Dictionary<string, string?>? cambios = null)
{
    var valores = new Dictionary<string, string?>(configuracionDemo);
    foreach (var (k, v) in cambios ?? new()) valores[k] = v;
    return ParametrosUbicacion.Desde(new ConfigurationBuilder().AddInMemoryCollection(valores).Build());
}

// Servicio demo: Radial -> Colonia, llegada programada 10:00 hora de Uruguay (UTC-3) = 13:00 UTC.
var ahoraInicial = new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);
const string Cobrador = "publico-cobrador";
const string OtroCobrador = "publico-otro";
const string ServicioId = "SV-COLONIA";
const string ServicioSinDestino = "SV-SIN-DESTINO";

(RepositorioMemoria, RelojFijo, UbicacionService) Crear(ParametrosUbicacion? p = null)
{
    var repo = new RepositorioMemoria();
    repo.Usuarios[Cobrador] = "US-2";
    repo.Usuarios[OtroCobrador] = "US-9";
    repo.Servicios.Add(new ServicioUbicacionDto { Id = ServicioId, Origen = "Radial de Conchillas", Destino = "Colonia",
        Salida = new DateTime(2026, 11, 11, 8, 45, 0), Llegada = new DateTime(2026, 11, 11, 10, 0, 0) });
    repo.Servicios.Add(new ServicioUbicacionDto { Id = ServicioSinDestino, Origen = "Ombúes", Destino = "Radial de Conchillas",
        Salida = new DateTime(2026, 11, 11, 8, 0, 0), Llegada = new DateTime(2026, 11, 11, 8, 30, 0) });
    repo.Asignaciones.Add(("US-2", ServicioId));
    repo.Asignaciones.Add(("US-2", ServicioSinDestino));
    var reloj = new RelojFijo(ahoraInicial);
    return (repo, reloj, new UbicacionService(repo, p ?? Parametros(), reloj));
}
static PublicarPosicionRequest Pos(decimal? lat, decimal? lon) => new() { Latitud = lat, Longitud = lon };

async Task<bool> Lanza<T>(Func<Task> accion) where T : Exception
{
    try { await accion(); return false; } catch (T) { return true; }
}

// S1/S2: publicación aceptada.
await Prueba("S2 publicación aceptada conserva cobrador, servicio, posición y recepción UTC", async () =>
{
    var (repo, _, service) = Crear();
    var r = await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.30m, -57.95m));
    var p = repo.Posiciones.Single();
    return p.UsuarioId == "US-2" && p.ServicioId == ServicioId && p.Latitud == -34.30m && p.Longitud == -57.95m
        && p.RecibidaUtc == ahoraInicial.UtcDateTime && p.RecibidaUtc.Kind == DateTimeKind.Utc
        && r.Id == p.Id && r.ServicioId == ServicioId && r.RecibidaUtc == p.RecibidaUtc
        && r.IntervaloSegundos == null && r.CumplePeriodicidad == null && r.PeriodicidadSegundos == 30 && repo.Guardados == 1;
});
await Prueba("S2 la periodicidad se contrasta con el parámetro (dentro y fuera de periodicidad + margen)", async () =>
{
    var (_, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.30m, -57.95m));
    reloj.Avanzar(TimeSpan.FromSeconds(40));
    var dentro = await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.31m, -57.94m));
    reloj.Avanzar(TimeSpan.FromSeconds(41));
    var fuera = await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.32m, -57.93m));
    return dentro.IntervaloSegundos == 40 && dentro.CumplePeriodicidad == true
        && fuera.IntervaloSegundos == 41 && fuera.CumplePeriodicidad == false;
});

// S1: rechazos sin escrituras ni reemplazo de la última posición.
async Task<bool> RechazaSinEscribir<T>(string publico, string servicio, PublicarPosicionRequest solicitud) where T : Exception
{
    var (repo, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.30m, -57.95m));
    var ultima = repo.Posiciones.Single();
    var guardados = repo.Guardados;
    reloj.Avanzar(TimeSpan.FromSeconds(30));
    var lanzo = await Lanza<T>(() => service.PublicarAsync(publico, servicio, solicitud));
    var consulta = await service.ConsultarAsync(ServicioId);
    return lanzo && repo.Posiciones.Count == 1 && repo.Guardados == guardados
        && consulta.UltimaPosicion?.RecibidaUtc == ultima.RecibidaUtc && consulta.UltimaPosicion.Latitud == -34.30m;
}
await Prueba("S1 cobrador no asignado al servicio: 403 lógico sin escrituras", () =>
    RechazaSinEscribir<UnauthorizedAccessException>(OtroCobrador, ServicioId, Pos(-34.4m, -57.9m)));
await Prueba("S1 identidad sin usuario: 403 lógico sin escrituras", () =>
    RechazaSinEscribir<UnauthorizedAccessException>("publico-inexistente", ServicioId, Pos(-34.4m, -57.9m)));
await Prueba("S1 servicio inexistente: 404 lógico sin escrituras", () =>
    RechazaSinEscribir<KeyNotFoundException>(Cobrador, "SV-NO-EXISTE", Pos(-34.4m, -57.9m)));
await Prueba("S1 latitud ausente: 400 lógico sin escrituras", () =>
    RechazaSinEscribir<ArgumentException>(Cobrador, ServicioId, Pos(null, -57.9m)));
await Prueba("S1 longitud ausente: 400 lógico sin escrituras", () =>
    RechazaSinEscribir<ArgumentException>(Cobrador, ServicioId, Pos(-34.4m, null)));
await Prueba("S1 latitud fuera de rango (90.000001): 400 lógico sin escrituras", () =>
    RechazaSinEscribir<ArgumentException>(Cobrador, ServicioId, Pos(90.000001m, -57.9m)));
await Prueba("S1 longitud fuera de rango (-180.000001): 400 lógico sin escrituras", () =>
    RechazaSinEscribir<ArgumentException>(Cobrador, ServicioId, Pos(-34.4m, -180.000001m)));
await Prueba("S1 límites de rango (90, 180) se aceptan", async () =>
{
    var (repo, _, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(90m, 180m));
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-90m, -180m));
    return repo.Posiciones.Count == 2;
});
await Prueba("S1 más de 6 decimales: 400 lógico (la columna guarda 6)", () =>
    RechazaSinEscribir<ArgumentException>(Cobrador, ServicioId, Pos(-34.1234567m, -57.9m)));
await Prueba("S6 el cobrador lista solo sus servicios asignados; sin usuario es 403 lógico", async () =>
{
    var (_, _, service) = Crear();
    var propios = await service.ListarServiciosAsignadosAsync(Cobrador);
    var ajenos = await service.ListarServiciosAsignadosAsync(OtroCobrador);
    return propios.Select(s => s.Id).OrderBy(x => x).SequenceEqual(new[] { ServicioId, ServicioSinDestino }.OrderBy(x => x))
        && ajenos.Count == 0 && await Lanza<UnauthorizedAccessException>(() => service.ListarServiciosAsignadosAsync("publico-inexistente"));
});

// S3/S4: consulta del pasajero.
await Prueba("S3 sin posiciones: sinDatos, sin llegada y con motivo", async () =>
{
    var (_, _, service) = Crear();
    var c = await service.ConsultarAsync(ServicioId);
    return c.UltimaPosicion == null && !c.Desactualizada && c.LlegadaAproximada == null
        && c.EstadoDemora == EstadoDemora.SinDatos && !string.IsNullOrWhiteSpace(c.Motivo) && c.DatosFicticios
        && c.Servicio.Id == ServicioId && c.Servicio.Destino == "Colonia";
});
await Prueba("S3 consulta de servicio inexistente: 404 lógico", async () =>
{
    var (_, _, service) = Crear();
    return await Lanza<KeyNotFoundException>(() => service.ConsultarAsync("SV-NO-EXISTE"));
});
await Prueba("S4 posición vigente lejos del destino a tiempo: llegada aproximada y enHorario", async () =>
{
    // ~30 km al norte de Colonia a 60 km/h -> ~30 min: 12:00 UTC + 30 = 09:30 local, antes de 10:00.
    var (_, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.1928m, -57.8400m));
    reloj.Avanzar(TimeSpan.FromSeconds(60));
    var c = await service.ConsultarAsync(ServicioId);
    var esperado = new DateTime(2026, 11, 11, 9, 30, 0);
    return c.UltimaPosicion is { AntiguedadSegundos: 60 } && !c.Desactualizada && c.EstadoDemora == EstadoDemora.EnHorario
        && c.LlegadaAproximada is { } l && Math.Abs((l - esperado).TotalMinutes) < 1 && c.Motivo == null;
});
await Prueba("S4 llegada aproximada después de la llegada programada + tolerancia: conDemora", async () =>
{
    // Misma distancia (~30 min) publicada a 12:40 UTC -> 10:10 local > 10:00 + 5.
    var (_, reloj, service) = Crear();
    reloj.Avanzar(TimeSpan.FromMinutes(40));
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.1928m, -57.8400m));
    var c = await service.ConsultarAsync(ServicioId);
    return c.EstadoDemora == EstadoDemora.ConDemora && c.LlegadaAproximada is { } l
        && Math.Abs((l - new DateTime(2026, 11, 11, 10, 10, 0)).TotalMinutes) < 1;
});
await Prueba("S4 llegada dentro de la tolerancia sigue enHorario", async () =>
{
    // Publicada 12:34 UTC -> llega ~10:04 local, dentro de 10:00 + 5.
    var (_, reloj, service) = Crear();
    reloj.Avanzar(TimeSpan.FromMinutes(34));
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.1928m, -57.8400m));
    var c = await service.ConsultarAsync(ServicioId);
    return c.EstadoDemora == EstadoDemora.EnHorario;
});
await Prueba("S3/S4 posición con antigüedad mayor al umbral: desactualizada, sinDatos y sin llegada", async () =>
{
    var (_, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.1928m, -57.8400m));
    reloj.Avanzar(TimeSpan.FromSeconds(121));
    var c = await service.ConsultarAsync(ServicioId);
    return c.UltimaPosicion is { AntiguedadSegundos: 121 } && c.Desactualizada && c.EstadoDemora == EstadoDemora.SinDatos
        && c.LlegadaAproximada == null && !string.IsNullOrWhiteSpace(c.Motivo);
});
await Prueba("S3 antigüedad igual al umbral sigue vigente", async () =>
{
    var (_, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.1928m, -57.8400m));
    reloj.Avanzar(TimeSpan.FromSeconds(120));
    var c = await service.ConsultarAsync(ServicioId);
    return !c.Desactualizada && c.EstadoDemora != EstadoDemora.SinDatos;
});
await Prueba("S4 destino sin referencia configurada: sinDatos sin inventar llegada", async () =>
{
    var (_, _, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioSinDestino, Pos(-34.0m, -57.9m));
    var c = await service.ConsultarAsync(ServicioSinDestino);
    return c.UltimaPosicion != null && !c.Desactualizada && c.EstadoDemora == EstadoDemora.SinDatos
        && c.LlegadaAproximada == null && !string.IsNullOrWhiteSpace(c.Motivo);
});
await Prueba("S3 la consulta toma la recepción más reciente del servicio", async () =>
{
    var (_, reloj, service) = Crear();
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.30m, -57.95m));
    reloj.Avanzar(TimeSpan.FromSeconds(30));
    await service.PublicarAsync(Cobrador, ServicioId, Pos(-34.40m, -57.85m));
    var c = await service.ConsultarAsync(ServicioId);
    return c.UltimaPosicion is { Latitud: -34.40m, Longitud: -57.85m };
});

// S5: parámetros configurables y visibles.
await Prueba("S5 la respuesta expone los parámetros académicos configurados", async () =>
{
    var (_, _, service) = Crear(Parametros(new() { ["Ubicacion:UmbralDesactualizacionSegundos"] = "300", ["Ubicacion:VelocidadPromedioKmH"] = "45.5" }));
    var c = await service.ConsultarAsync(ServicioId);
    return c.Parametros is { PeriodicidadSegundos: 30, UmbralDesactualizacionSegundos: 300, VelocidadPromedioKmH: 45.5, ToleranciaDemoraMinutos: 5 };
});
await Prueba("S5 parámetros inválidos o ausentes detienen el arranque", () =>
{
    var invalidos = new[]
    {
        new Dictionary<string, string?> { ["Ubicacion:PeriodicidadSegundos"] = "0" },
        new Dictionary<string, string?> { ["Ubicacion:UmbralDesactualizacionSegundos"] = null },
        new Dictionary<string, string?> { ["Ubicacion:VelocidadPromedioKmH"] = "0" },
        new Dictionary<string, string?> { ["Ubicacion:ToleranciaDemoraMinutos"] = "-1" },
        new Dictionary<string, string?> { ["Ubicacion:Destinos:Colonia:Latitud"] = "-91" },
    };
    return Task.FromResult(invalidos.All(c => { try { Parametros(c); return false; } catch (InvalidOperationException) { return true; } }));
});
await Prueba("S5 appsettings.json de la API define la sección Ubicacion válida", () =>
{
    var ruta = Path.Combine(Directory.GetCurrentDirectory(), "src/Api/API-Berruti/API-Berruti/appsettings.json");
    var p = ParametrosUbicacion.Desde(new ConfigurationBuilder().AddJsonFile(ruta).Build());
    return Task.FromResult(p.Destinos.ContainsKey("Colonia") && p.Destinos.ContainsKey("Radial de Conchillas"));
});

// RN-12: roles exigidos por endpoint y traducción HTTP del controlador.
await Prueba("RN-12 publicación y servicios asignados exigen Cobrador; consultas exigen Pasajero", () =>
{
    string? Rol(string metodo) => typeof(UbicacionController).GetMethod(metodo)!.GetCustomAttribute<AuthorizeAttribute>()?.Roles;
    var clase = typeof(UbicacionController).GetCustomAttributes<AllowAnonymousAttribute>().Any();
    var anonimos = typeof(UbicacionController).GetMethods().Any(m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null);
    return Task.FromResult(!clase && !anonimos && Rol(nameof(UbicacionController.Publicar)) == "Cobrador"
        && Rol(nameof(UbicacionController.ListarAsignados)) == "Cobrador"
        && Rol(nameof(UbicacionController.ListarServicios)) == "Pasajero" && Rol(nameof(UbicacionController.Consultar)) == "Pasajero");
});
UbicacionController Controlador(IUbicacionService s, string? publico)
{
    var claims = publico == null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, publico), new Claim(ClaimTypes.Role, "Cobrador") };
    return new UbicacionController(s) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) } } };
}
static int? Estado(IActionResult r) => r switch { ObjectResult o => o.StatusCode, StatusCodeResult s => s.StatusCode, ForbidResult => 403, _ => null };
await Prueba("HTTP publicación: 201 aceptada, 403 no asignado, 404 servicio, 400 incompleta, 401 sin identidad", async () =>
{
    var (repo, _, service) = Crear();
    var ok = (await Controlador(service, Cobrador).Publicar(ServicioId, Pos(-34.3m, -57.9m))).Result!;
    var noAsignado = (await Controlador(service, OtroCobrador).Publicar(ServicioId, Pos(-34.3m, -57.9m))).Result!;
    var inexistente = (await Controlador(service, Cobrador).Publicar("SV-NO-EXISTE", Pos(-34.3m, -57.9m))).Result!;
    var incompleta = (await Controlador(service, Cobrador).Publicar(ServicioId, Pos(null, null))).Result!;
    var anonimo = (await Controlador(service, null).Publicar(ServicioId, Pos(-34.3m, -57.9m))).Result!;
    return Estado(ok) == 201 && Estado(noAsignado) == 403 && Estado(inexistente) == 404 && Estado(incompleta) == 400
        && anonimo is UnauthorizedResult && repo.Posiciones.Count == 1;
});
await Prueba("HTTP consulta: 200 existente y 404 inexistente", async () =>
{
    var (_, _, service) = Crear();
    var ok = (await Controlador(service, Cobrador).Consultar(ServicioId)).Result!;
    var no = (await Controlador(service, Cobrador).Consultar("SV-NO-EXISTE")).Result!;
    return Estado(ok) == 200 && no is NotFoundResult;
});

Console.WriteLine($"{pruebas - fallos}/{pruebas} pruebas de geolocalización aprobadas.");
return fallos == 0 ? 0 : 1;

sealed class RelojFijo(DateTimeOffset inicio) : TimeProvider
{
    private DateTimeOffset _ahora = inicio;
    public void Avanzar(TimeSpan lapso) => _ahora += lapso;
    public override DateTimeOffset GetUtcNow() => _ahora;
}

sealed class RepositorioMemoria : IUbicacionRepository
{
    public Dictionary<string, string> Usuarios { get; } = new();
    public List<ServicioUbicacionDto> Servicios { get; } = new();
    public List<(string UsuarioId, string ServicioId)> Asignaciones { get; } = new();
    public List<PosicionRecibida> Posiciones { get; } = new();
    private readonly List<PosicionRecibida> _pendientes = new();
    public int Guardados { get; private set; }

    public Task<string?> BuscarUsuarioIdPorPublicoAsync(string identificadorPublico) => Task.FromResult(Usuarios.GetValueOrDefault(identificadorPublico));
    public Task<ServicioUbicacionDto?> BuscarServicioAsync(string servicioId) => Task.FromResult(Servicios.FirstOrDefault(s => s.Id == servicioId));
    public Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsync() => Task.FromResult<IReadOnlyList<ServicioUbicacionDto>>(Servicios.ToArray());
    public Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsignadosAsync(string usuarioId) =>
        Task.FromResult<IReadOnlyList<ServicioUbicacionDto>>(Servicios.Where(s => Asignaciones.Contains((usuarioId, s.Id))).ToArray());
    public Task<bool> EstaAsignadoAsync(string usuarioId, string servicioId) => Task.FromResult(Asignaciones.Contains((usuarioId, servicioId)));
    public Task<PosicionRecibida?> BuscarUltimaPosicionAsync(string servicioId) =>
        Task.FromResult(Posiciones.Where(p => p.ServicioId == servicioId).OrderByDescending(p => p.RecibidaUtc).FirstOrDefault());
    public Task AgregarPosicionAsync(PosicionRecibida posicion) { _pendientes.Add(posicion); return Task.CompletedTask; }
    public Task GuardarAsync() { Posiciones.AddRange(_pendientes); _pendientes.Clear(); Guardados++; return Task.CompletedTask; }
}
