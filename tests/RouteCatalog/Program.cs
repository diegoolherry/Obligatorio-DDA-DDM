using System.Reflection;
using API_Berruti.Controllers.Catalogo;
using API_Berruti.Data.Catalogo;
using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Executable checks for FIN-001 (route catalog, calendars and service generation).
// Run from the repository root: dotnet run --project tests/RouteCatalog/RouteCatalog.csproj --no-launch-profile
var failures = new List<string>();
var checkCount = 0;
void Check(bool ok, string name) { checkCount++; if (!ok) failures.Add(name); }

try
{
    var json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "src/Api/API-Berruti/API-Berruti/Data/Catalogo/horarios-invierno-2026.json"));
    TranscriptionChecks.Run(json, Check);
    await CalendarChecks.RunAsync(json, Check);
    await GenerationChecks.RunAsync(json, Check);
}
catch (Exception e)
{
    failures.Add($"unexpected exception: {e}");
}

foreach (var failure in failures) Console.Error.WriteLine($"FAIL: {failure}");
Console.WriteLine($"{checkCount - failures.Count(f => !f.StartsWith("unexpected"))}/{checkCount} checks passed.");
return failures.Count == 0 ? 0 : 1;

internal static class Catalog
{
    public static CatalogoSembrado Build(string json) => HorariosPublicados.Construir(HorariosPublicados.Leer(json), []);

    public static DatosCatalogo Data(CatalogoSembrado c) =>
        new(c.Bloques, c.Rutas, c.ParadasRuta, c.Salidas, c.Horas, c.ParadasNuevas, c.Feriados, c.Excepciones);
}

internal sealed class MemoryCatalogoRepository(DatosCatalogo datos) : ICatalogoRepository
{
    public Task<DatosCatalogo> ObtenerAsync() => Task.FromResult(datos);
}

internal sealed class MemoryGeneracionRepository : IGeneracionServiciosRepository
{
    public List<Tramo> Tramos { get; } = [];
    public List<Servicio> Servicios { get; } = [];
    public int Saves { get; private set; }
    public bool FailWithConflict { get; set; }

    public Task<List<Tramo>> ListarTramosAsync() => Task.FromResult(Tramos.ToList());
    public Task<List<Servicio>> ListarGeneradosAsync(DateTime desde, DateTime hasta) =>
        Task.FromResult(Servicios.Where(s => s.SalidaCatalogoId != null && s.Salida >= desde && s.Salida < hasta).ToList());
    public Task GuardarAsync(IReadOnlyList<Tramo> tramosNuevos, IReadOnlyList<Servicio> serviciosNuevos)
    {
        if (FailWithConflict) throw new GeneracionConcurrenteException(new InvalidOperationException("duplicate"));
        Saves++;
        Tramos.AddRange(tramosNuevos);
        Servicios.AddRange(serviciosNuevos);
        return Task.CompletedTask;
    }
}

internal static class TranscriptionChecks
{
    public static void Run(string json, Action<bool, string> check)
    {
        var archivo = HorariosPublicados.Leer(json);
        check(HorariosPublicados.Validar(archivo).Count == 0, "versioned transcription is consistent");
        check(archivo.Fuente?.Url == "http://www.berruttiturismo.com.uy/horarios.html", "transcription documents the published source URL");
        check(new[] { "\"tarifa", "\"capacidad", "\"importe", "\"vehiculo", "\"flota" }.All(clave => !json.Contains(clave, StringComparison.OrdinalIgnoreCase)),
            "transcription has no fare, capacity or fleet fields");

        var rutas = archivo.Bloques!.SelectMany(b => b.Rutas!).ToList();
        var familias = rutas.Select(r => r.Familia).ToHashSet();
        check(familias.SetEquals(["Nueva Palmira–Colonia", "Conexiones de Ombúes y Conchillas", "Carmelo–Mercedes", "Nueva Palmira–Carmelo", "Nueva Palmira–Agraciada"]),
            "the five approved families are present");
        foreach (var familia in familias.Where(f => f != "Conexiones de Ombúes y Conchillas"))
            check(rutas.Where(r => r.Familia == familia).Select(r => r.Paradas!.First()).Distinct().Count() == 2, $"{familia} has both directions");

        var polancos = rutas.Where(r => r.Clave.EndsWith("-P")).ToList();
        check(polancos.Select(r => r.Clave).ToHashSet().SetEquals(["NP-COL-P", "COL-NP-P", "NP-CAR-P", "CAR-NP-P"]), "Polancos variants exist in both directions");
        check(polancos.All(r => r.Paradas!.Contains("Polancos") && r.Salidas!.All(s => s.Codigos!.Split('-').Contains("P"))), "Polancos variants carry the stop and the P code");
        check(polancos.All(r => r.Salidas!.All(s => s.Horas![r.Paradas!.IndexOf("Polancos")] is null)), "Polancos has no invented time");
        check(rutas.Where(r => !r.Clave.EndsWith("-P")).All(r => r.Salidas!.All(s => s.Codigos is null || !s.Codigos.Split('-').Contains("P"))),
            "trips marked P only live in Polancos variants");

        int Count(string ruta, string dias) => rutas.Single(r => r.Clave == ruta).Salidas!.Count(s => s.Dias == dias);
        check(Count("NP-COL", "LunesAViernes") == 9 && Count("NP-COL-P", "LunesAViernes") == 1 && Count("COL-NP", "LunesAViernes") == 9 && Count("COL-NP-P", "LunesAViernes") == 1,
            "Palmira–Colonia weekday block has 10 trips per direction");
        check(Count("NP-COL", "Sabados") == 4 && Count("COL-NP", "Sabados") == 4 && Count("NP-COL", "DomingosYFeriados") == 3 && Count("COL-NP", "DomingosYFeriados") == 4,
            "Palmira–Colonia weekend counts match the source");
        check(Count("CAR-MER", "LunesAViernes") == 1 && Count("CAR-MER", "LunesASabado") == 1 && Count("MER-CAR", "LunesASabado") == 1,
            "Carmelo–Mercedes keeps weekday and Monday–Saturday trips");
        check(rutas.Single(r => r.Clave == "NP-AGR").Salidas!.Single().Dias == "LunesYViernes", "Agraciada runs Mondays and Fridays only");
        var primera = rutas.Single(r => r.Clave == "NP-COL").Salidas!.First();
        check(primera.Horas!.SequenceEqual(["05:20", "05:50", "06:15", "07:05"]), "first Palmira–Colonia weekday trip matches the source");
        var nota = rutas.Single(r => r.Clave == "COL-NP").Salidas!.Single(s => s.Horas![0] == "20:15" && s.Dias == "LunesAViernes");
        check(nota.Nota == "Finaliza en Paraguay y Jacinto Laguna", "trip footnote (2) is preserved");

        var bloques = archivo.Bloques!.ToDictionary(b => b.Clave);
        check(bloques["NP-COL"].VigenteDesde == "2026-03-02" && bloques["NP-COL"].VigenteHasta == "2026-12-20", "main block validity is 02/03/26–20/12/26");
        check(bloques["CAR-MER"].VigenteDesde == "2026-03-02" && bloques["CAR-MER"].VigenteHasta is null, "Carmelo–Mercedes does not inherit the end date");
        check(bloques["NP-CAR"].VigenteHasta is null && bloques["NP-AGR"].VigenteHasta is null, "other blocks do not inherit the main validity");

        // The validator rejects inconsistent transcriptions with a precise reason.
        var invalido = json.Replace("[\"05:20\", \"05:50\", \"06:15\", \"07:05\"]", "[\"05:20\", \"05:10\", \"06:15\", \"07:05\"]");
        check(Rejects(invalido, "NP-COL salida 1: las horas deben crecer"), "decreasing times are rejected");
        check(Rejects(json.Replace("[\"05:20\", \"05:50\", \"06:15\", \"07:05\"]", "[\"05:20\", \"05:50\", \"06:15\"]"), "NP-COL salida 1: tiene 3 horas para 4 paradas"),
            "time count must match the stop count");
        check(Rejects(json.Replace("\"horas\": [\"12:00\", null]", "\"horas\": [null, \"12:00\"]"), "NP-AGR salida 1: la primera parada debe tener la hora de salida publicada"),
            "first stop needs the published departure");
        check(Rejects(json.Replace("\"codigos\": \"P-O\"", "\"codigos\": \"P-X\""), "código fuera de la leyenda (X)"), "unknown codes are rejected");
        check(Rejects(json.Replace("\"dias\": \"LunesYViernes\", \"horas\": [\"12:00\"", "\"dias\": \"Martes\", \"horas\": [\"12:00\""), "tipo de día desconocido (Martes)"),
            "unknown day types are rejected");
    }

    private static bool Rejects(string json, string fragment)
    {
        try { HorariosPublicados.Leer(json); return false; }
        catch (InvalidOperationException e) { return e.Message.Contains(fragment); }
    }
}

internal static class CalendarChecks
{
    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        var service = new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(Catalog.Build(json))));

        async Task<SalidasDelDiaDto> Day(string fecha) => await service.SalidasDelDiaAsync(DateOnly.Parse(fecha));
        static BloqueDelDiaDto Block(SalidasDelDiaDto d, string clave) => d.Bloques.Single(b => b.Clave == clave);
        static IEnumerable<string> Trips(BloqueDelDiaDto b, string ruta) =>
            b.Salidas.Where(s => s.RutaClave == ruta).Select(s => s.Salida.Horas[0].Hora.ToString("HH:mm"));

        var miercoles = await Day("2026-11-11");
        check(Block(miercoles, "NP-COL").Estado == EstadoDia.Opera && Trips(Block(miercoles, "NP-COL"), "NP-COL").Count() == 9, "weekday runs Monday–Friday trips");
        check(Trips(Block(miercoles, "CAR-MER"), "CAR-MER").SequenceEqual(["06:00", "13:00"]), "weekday includes Monday–Saturday trips");
        check(Block(miercoles, "NP-AGR").Salidas.Count == 0, "Agraciada does not run on Wednesday");
        check(Block(await Day("2026-11-09"), "NP-AGR").Salidas.Count == 2 && Block(await Day("2026-11-13"), "NP-AGR").Salidas.Count == 2, "Agraciada runs Monday and Friday");

        var sabado = await Day("2026-11-14");
        check(Trips(Block(sabado, "NP-COL"), "NP-COL").SequenceEqual(["05:20", "11:05", "16:25", "19:20"]), "Saturday runs Saturday trips");
        check(Trips(Block(sabado, "CAR-MER"), "CAR-MER").SequenceEqual(["13:00"]) && Trips(Block(sabado, "CAR-MER"), "MER-CAR").SequenceEqual(["17:50"]), "Saturday runs Monday–Saturday trips only");

        var domingo = await Day("2026-11-15");
        check(Trips(Block(domingo, "NP-COL"), "NP-COL").SequenceEqual(["11:05", "17:15", "19:20"]), "Sunday runs Sundays-and-holidays trips");
        check(Block(domingo, "CAR-MER").Salidas.Count == 0, "Carmelo–Mercedes does not run on Sundays");

        var feriado = await Day("2026-08-25");
        check(feriado.Feriado == "Declaratoria de la Independencia", "holiday name is reported");
        check(Trips(Block(feriado, "NP-COL"), "NP-COL").SequenceEqual(["11:05", "17:15", "19:20"]), "weekday holiday uses the Sundays-and-holidays schedule");
        check(Block(feriado, "CAR-MER").Salidas.Count == 0 && Block(feriado, "NP-AGR").Salidas.Count == 0, "Carmelo–Mercedes and Agraciada do not run on holidays");
        check(Trips(Block(await Day("2026-07-18"), "NP-COL"), "NP-COL").SequenceEqual(["11:05", "17:15", "19:20"]), "Saturday holiday uses the Sundays-and-holidays schedule");

        var despues = await Day("2026-12-21");
        check(Block(despues, "NP-COL").Estado == EstadoDia.FueraDeVigencia && Block(despues, "NP-COL").Salidas.Count == 0, "main block stops after 20/12/26");
        check(Block(despues, "CAR-MER").Estado == EstadoDia.Opera && Block(despues, "CAR-MER").Salidas.Count == 4, "20/12/26 applies only to the main block");
        var antes = await Day("2026-03-01");
        check(Block(antes, "NP-COL").Estado == EstadoDia.FueraDeVigencia && Block(antes, "CAR-MER").Estado == EstadoDia.FueraDeVigencia, "blocks do not run before 02/03/26");
        check(Block(antes, "NP-CAR").Estado == EstadoDia.Opera, "a block without published validity is not limited by another block");
        check(Block(await Day("2026-03-02"), "NP-COL").Estado == EstadoDia.Opera && Block(await Day("2026-12-20"), "NP-COL").Estado == EstadoDia.Opera, "validity bounds are inclusive");

        var mayo = await Day("2026-05-01");
        check(mayo.Bloques.All(b => b.Estado == EstadoDia.SinServicio && b.Salidas.Count == 0), "1 May has no service in any block");
        var navidad = await Day("2026-12-25");
        check(Block(navidad, "CAR-MER").Estado == EstadoDia.SinServicio, "no-service exception wins over the holiday schedule");
        var turismo = await Day("2026-04-01");
        check(turismo.Bloques.All(b => b.Estado == EstadoDia.ConsultarAgencia && b.Salidas.Count == 0 && b.Motivo!.Contains("Turismo")), "Tourism Week asks to consult the agency, without invented trips");
        check(Block(await Day("2026-04-06"), "NP-COL").Estado == EstadoDia.Opera, "Tourism Week ends on 05/04");

        // Exceptions can target one block only.
        var sembrado = Catalog.Build(json);
        var colonia = sembrado.Bloques.Single(b => b.Clave == "NP-COL");
        sembrado.Excepciones.Add(new ExcepcionCalendario { BloqueId = colonia.Id, Desde = new DateTime(2026, 11, 11), Hasta = new DateTime(2026, 11, 11), Tipo = TipoExcepcion.SinServicio, Motivo = "Paro" });
        var porBloque = await new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(sembrado))).SalidasDelDiaAsync(new DateOnly(2026, 11, 11));
        check(Block(porBloque, "NP-COL").Estado == EstadoDia.SinServicio && Block(porBloque, "CAR-MER").Estado == EstadoDia.Opera, "block exceptions do not affect other blocks");

        // Catalog view: ordered stops per route, approximate arrivals, no invented times.
        var catalogo = await service.ObtenerAsync();
        var rutas = catalogo.Bloques.SelectMany(b => b.Rutas).ToDictionary(r => r.Clave);
        check(rutas["NP-COL-P"].Paradas.Select(p => p.Nombre).SequenceEqual(["Nueva Palmira", "Polancos", "Carmelo", "Radial de Conchillas", "Colonia"])
            && rutas["NP-COL-P"].Paradas.Select(p => p.Orden).SequenceEqual([1, 2, 3, 4, 5]), "route stops are ordered locally");
        check(rutas["COL-NP-P"].Paradas.Single(p => p.Nombre == "Polancos").Orden == 4 && rutas["NP-COL-P"].Paradas.Single(p => p.Nombre == "Polancos").Orden == 2,
            "each direction has its own order for the same stop");
        var polancos = rutas["NP-COL-P"].Salidas.First();
        check(polancos.Horas.Select(h => h.Orden).SequenceEqual([1, 3, 4, 5]), "stops without a published time have no time");
        check(!polancos.Horas[0].Aproximada && polancos.Horas.Skip(1).All(h => h.Aproximada), "only the departure is exact; later times are approximate");
        check(catalogo.Feriados.Count == 5 && catalogo.Excepciones.Count == 4, "catalog exposes holidays and exceptions");

        var controller = new CatalogoController(service);
        check((await controller.Salidas("11/11/2026")).Result is BadRequestObjectResult && (await controller.Salidas(null)).Result is BadRequestObjectResult, "invalid dates return 400");
        check((await controller.Salidas("2026-11-11")).Result is OkObjectResult, "valid date returns 200");
        check(typeof(CatalogoController).GetCustomAttribute<AllowAnonymousAttribute>() is not null, "catalog query is public");
    }
}

internal static class GenerationChecks
{
    private static readonly GenerarServiciosDto Lunes = new()
    {
        Desde = new DateOnly(2026, 11, 9), Hasta = new DateOnly(2026, 11, 9), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento
    };

    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        var sembrado = Catalog.Build(json);
        var datos = Catalog.Data(sembrado);
        var repo = new MemoryGeneracionRepository();
        var service = new GeneracionServiciosService(new MemoryCatalogoRepository(datos), repo);

        // Expected count: Monday trips with k published times generate k(k-1)/2 services each.
        var catalogo = new CatalogoService(new MemoryCatalogoRepository(datos));
        var dia = await catalogo.SalidasDelDiaAsync(new DateOnly(2026, 11, 9));
        var esperado = dia.Bloques.SelectMany(b => b.Salidas).Sum(s => s.Salida.Horas.Count * (s.Salida.Horas.Count - 1) / 2);

        var resultado = await service.GenerarAsync(Lunes);
        check(resultado.ServiciosCreados == esperado && repo.Servicios.Count == esperado && esperado > 0, $"generates one service per timed stop pair ({esperado})");
        check(repo.Servicios.All(s => s.Capacidad == 40 && s.PoliticaAsiento == PoliticaAsiento.SinAsiento && s.Llegada > s.Salida && s.SalidaCatalogoId != null),
            "generated services use the requested capacity and policy");

        var paradas = sembrado.ParadasNuevas.ToDictionary(p => p.Nombre, p => p.Id);
        var tramo = repo.Tramos.Single(t => t.OrigenId == paradas["Nueva Palmira"] && t.DestinoId == paradas["Colonia"]);
        check(repo.Servicios.Any(s => s.TramoId == tramo.Id && s.Salida == new DateTime(2026, 11, 9, 5, 20, 0) && s.Llegada == new DateTime(2026, 11, 9, 7, 5, 0)),
            "Palmira→Colonia 05:20 arrives 07:05");
        check(repo.Servicios.Where(s => s.Salida == new DateTime(2026, 11, 9, 5, 20, 0) && repo.Tramos.Single(t => t.Id == s.TramoId).OrigenId == paradas["Nueva Palmira"]).Count() == 3,
            "a four-stop trip yields three services from its origin");
        check(!repo.Tramos.Any(t => t.OrigenId == paradas["Polancos"] || t.DestinoId == paradas["Polancos"]), "stops without time generate no services");
        check(!repo.Tramos.Any(t => t.OrigenId == paradas["Ombúes"] || t.OrigenId == paradas["Agraciada"]), "blocks without arrival times generate no services");
        check(repo.Tramos.GroupBy(t => (t.OrigenId, t.DestinoId)).All(g => g.Count() == 1), "each origin/destination pair gets one tramo");

        var antes = repo.Servicios.Count;
        var repetido = await service.GenerarAsync(Lunes);
        check(repetido.ServiciosCreados == 0 && repetido.ServiciosExistentes == esperado && repetido.TramosCreados == 0 && repo.Servicios.Count == antes,
            "repeating a generation creates nothing");

        var mayo = await service.GenerarAsync(new GenerarServiciosDto { Desde = new DateOnly(2026, 5, 1), Hasta = new DateOnly(2026, 5, 1), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento });
        check(mayo.ServiciosCreados == 0, "no-service days generate nothing");

        // Existing tramos are reused, not duplicated.
        var conTramo = new MemoryGeneracionRepository();
        var existente = new Tramo { OrigenId = paradas["Nueva Palmira"], DestinoId = paradas["Colonia"] };
        conTramo.Tramos.Add(existente);
        await new GeneracionServiciosService(new MemoryCatalogoRepository(datos), conTramo).GenerarAsync(Lunes);
        check(conTramo.Tramos.Count(t => t.OrigenId == existente.OrigenId && t.DestinoId == existente.DestinoId) == 1
            && conTramo.Servicios.Any(s => s.TramoId == existente.Id), "existing tramos are reused");

        foreach (var (dto, mensaje) in new (GenerarServiciosDto, string)[]
        {
            (new() { Desde = Lunes.Desde, Hasta = Lunes.Hasta, PoliticaAsiento = PoliticaAsiento.SinAsiento }, "La capacidad es obligatoria y debe ser al menos 1."),
            (new() { Desde = Lunes.Desde, Hasta = Lunes.Hasta, Capacidad = 0, PoliticaAsiento = PoliticaAsiento.SinAsiento }, "La capacidad es obligatoria y debe ser al menos 1."),
            (new() { Desde = Lunes.Desde, Hasta = Lunes.Hasta, Capacidad = 40 }, "La política de asiento es obligatoria."),
            (new() { Desde = Lunes.Desde, Hasta = new DateOnly(2026, 11, 8), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento }, "La fecha hasta no puede ser anterior a la fecha desde."),
            (new() { Desde = new DateOnly(2026, 11, 1), Hasta = new DateOnly(2026, 12, 2), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento }, "El rango no puede superar 31 días."),
            (new() { Hasta = Lunes.Hasta, Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento }, "Las fechas desde y hasta son obligatorias.")
        })
        {
            var vacio = new MemoryGeneracionRepository();
            var respuesta = await new GeneracionServiciosController(new GeneracionServiciosService(new MemoryCatalogoRepository(datos), vacio)).Generar(dto);
            check(respuesta.Result is BadRequestObjectResult { Value: string texto } && texto == mensaje && vacio.Saves == 0 && vacio.Servicios.Count == 0,
                $"rejected without saving: {mensaje}");
        }
        check(await Accepts31DaysAsync(datos), "a 31-day range is accepted");

        var conflicto = new MemoryGeneracionRepository { FailWithConflict = true };
        var choque = await new GeneracionServiciosController(new GeneracionServiciosService(new MemoryCatalogoRepository(datos), conflicto)).Generar(Lunes);
        check(choque.Result is ConflictObjectResult && conflicto.Servicios.Count == 0, "a concurrent generation returns 409 without partial data");

        var autorizacion = typeof(GeneracionServiciosController).GetCustomAttribute<AuthorizeAttribute>();
        check(autorizacion?.Roles == "Administrador" && typeof(GeneracionServiciosController).GetCustomAttribute<AllowAnonymousAttribute>() is null,
            "only administrators generate services");
    }

    private static async Task<bool> Accepts31DaysAsync(DatosCatalogo datos)
    {
        var repo = new MemoryGeneracionRepository();
        var r = await new GeneracionServiciosService(new MemoryCatalogoRepository(datos), repo).GenerarAsync(new GenerarServiciosDto
        {
            Desde = new DateOnly(2026, 11, 1), Hasta = new DateOnly(2026, 12, 1), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica
        });
        return r.ServiciosCreados > 0 && repo.Saves == 1;
    }
}
