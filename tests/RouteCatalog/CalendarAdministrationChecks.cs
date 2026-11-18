using API_Berruti.Controllers.Catalogo;
using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Mvc;

// FIN-008-02: administration of feriados and calendar exceptions. They have no dependent Servicios,
// so deleting them is a real delete; changes show up in the day query and the next generation.
internal static class CalendarAdministrationChecks
{
    private static readonly DateOnly Lunes = new(2026, 11, 23);

    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        var datos = Catalog.Data(Catalog.Build(json));
        var repo = new MemoryAdministracionRepository(datos);
        var controller = new CatalogoAdministracionController(new CatalogoAdministracionService(new MemoryCatalogoRepository(datos), repo));
        var lectura = new CatalogoService(new MemoryCatalogoRepository(datos));
        var feriadosIniciales = datos.Feriados.Count;

        // Feriados.
        var antes = await lectura.SalidasDelDiaAsync(Lunes);
        var creado = await controller.CrearFeriado(new FeriadoEscrituraDto { Fecha = Lunes, Nombre = "Feriado de prueba" });
        check(creado.Result is CreatedAtActionResult && datos.Feriados.Any(f => f.Fecha == Lunes.ToDateTime(TimeOnly.MinValue) && f.Nombre == "Feriado de prueba"),
            "admin creates a feriado (201)");
        var despues = await lectura.SalidasDelDiaAsync(Lunes);
        check(despues.Feriado == "Feriado de prueba" && Trips(despues) != Trips(antes), "a new feriado changes the day query to holiday departures");
        await Rejects(check, datos, repo, () => controller.CrearFeriado(new() { Fecha = Lunes, Nombre = "Otro" }), 409, "Ya existe un feriado el 2026-11-23.");
        await Rejects(check, datos, repo, () => controller.CrearFeriado(new() { Nombre = "Sin fecha" }), 400, "La fecha es obligatoria.");
        await Rejects(check, datos, repo, () => controller.CrearFeriado(new() { Fecha = new(2026, 11, 24), Nombre = " " }), 400, "El nombre es obligatorio y tiene hasta 100 caracteres.");
        var renombrado = await controller.ActualizarFeriado(Lunes, new() { Nombre = "Renombrado" });
        check(renombrado.Result is OkObjectResult && datos.Feriados.Single(f => f.Fecha == Lunes.ToDateTime(TimeOnly.MinValue)).Nombre == "Renombrado", "admin renames a feriado (200)");
        await Rejects(check, datos, repo, () => controller.ActualizarFeriado(new(2026, 11, 24), new() { Nombre = "X" }), 404, null);
        check(await controller.EliminarFeriado(Lunes) is NoContentResult && datos.Feriados.Count == feriadosIniciales, "deleting a feriado removes it (204)");
        check(await controller.EliminarFeriado(Lunes) is NotFoundResult, "deleting a missing feriado returns 404");
        check(Trips(await lectura.SalidasDelDiaAsync(Lunes)) == Trips(antes), "after deleting the feriado the day query is back to weekday departures");

        // Excepciones.
        var bloque = datos.Bloques.First();
        ExcepcionEscrituraDto Excepcion(string? bloqueId, DateOnly? desde, DateOnly? hasta, TipoExcepcion? tipo = TipoExcepcion.SinServicio, string? motivo = "Paro") =>
            new() { BloqueId = bloqueId, Desde = desde, Hasta = hasta, Tipo = tipo, Motivo = motivo };
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion(bloque.Id, Lunes, Lunes.AddDays(-1))), 400, "La fecha hasta no puede ser anterior a la fecha desde.");
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion(bloque.Id, null, Lunes)), 400, "Las fechas desde y hasta son obligatorias.");
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion(bloque.Id, Lunes, Lunes, tipo: null)), 400, "El tipo de excepción es obligatorio.");
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion(bloque.Id, Lunes, Lunes, motivo: "")), 400, "El motivo es obligatorio y tiene hasta 200 caracteres.");
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion("no-existe", Lunes, Lunes)), 400, "El bloque no existe o fue dado de baja.");

        var global = ((await controller.CrearExcepcion(Excepcion(null, Lunes, Lunes))).Result as CreatedAtActionResult)?.Value as ExcepcionAdministracionDto;
        check(global is not null && datos.Excepciones.Any(e => e.Id == global.Id && e.BloqueId == null), "admin creates a global exception (201)");
        var dia = await lectura.SalidasDelDiaAsync(Lunes);
        check(dia.Bloques.All(b => b.Estado == EstadoDia.SinServicio && b.Salidas.Count == 0 && b.Motivo == "Paro"), "a global no-service exception empties the day");
        var gen = new MemoryGeneracionRepository();
        await new GeneracionServiciosService(new MemoryCatalogoRepository(datos), gen)
            .GenerarAsync(new GenerarServiciosDto { Desde = Lunes, Hasta = Lunes, Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento });
        check(gen.Servicios.Count == 0, "the next generation skips a no-service exception day");

        var editada = await controller.ActualizarExcepcion(global!.Id, Excepcion(bloque.Id, Lunes, Lunes, TipoExcepcion.ConsultarAgencia, "Consultar"));
        check(editada.Result is OkObjectResult && datos.Excepciones.Single(e => e.Id == global.Id) is { Tipo: TipoExcepcion.ConsultarAgencia, Motivo: "Consultar" } e2 && e2.BloqueId == bloque.Id,
            "admin updates an exception (200)");
        await Rejects(check, datos, repo, () => controller.ActualizarExcepcion("no-existe", Excepcion(null, Lunes, Lunes)), 404, null);

        var listado = ((await controller.Obtener()).Result as OkObjectResult)?.Value as CatalogoAdministracionDto;
        check(listado is not null && listado.Excepciones.Any(e => e.Id == global.Id) && listado.Feriados.Count == feriadosIniciales,
            "the admin listing includes feriados and exceptions");

        check(await controller.EliminarExcepcion(global.Id) is NoContentResult && datos.Excepciones.All(e => e.Id != global.Id), "deleting an exception removes it (204)");
        check(await controller.EliminarExcepcion(global.Id) is NotFoundResult, "deleting a missing exception returns 404");

        // Exceptions of a deactivated block are hidden and cannot be changed.
        var propia = ((await controller.CrearExcepcion(Excepcion(bloque.Id, Lunes, Lunes))).Result as CreatedAtActionResult)?.Value as ExcepcionAdministracionDto;
        await controller.EliminarBloque(bloque.Id);
        check(await controller.EliminarExcepcion(propia!.Id) is NotFoundResult, "an exception of an inactive bloque returns 404");
        await Rejects(check, datos, repo, () => controller.CrearExcepcion(Excepcion(bloque.Id, Lunes, Lunes)), 400, "El bloque no existe o fue dado de baja.");
    }

    private static string Trips(SalidasDelDiaDto d) => string.Join(",", d.Bloques.SelectMany(b => b.Salidas).Select(s => s.Salida.Id));

    private static async Task Rejects<T>(Action<bool, string> check, DatosCatalogo datos, MemoryAdministracionRepository repo, Func<Task<ActionResult<T>>> accion, int estado, string? mensaje)
    {
        var antes = Snapshot(datos);
        var guardados = repo.Saves;
        var respuesta = (await accion()).Result;
        var (codigo, texto) = respuesta switch
        {
            BadRequestObjectResult r => (400, r.Value as string), ConflictObjectResult r => (409, r.Value as string),
            NotFoundResult => (404, null), _ => (0, null)
        };
        check(codigo == estado && texto == mensaje && Snapshot(datos) == antes && repo.Saves == guardados,
            $"calendar: rejected with {estado} without saving: {mensaje ?? "not found"}");
    }

    private static string Snapshot(DatosCatalogo d) => string.Join("|",
        string.Join(",", d.Feriados.Select(f => $"{f.Fecha:yyyyMMdd}{f.Nombre}")),
        string.Join(",", d.Excepciones.Select(e => $"{e.Id}{e.BloqueId}{e.Desde:yyyyMMdd}{e.Hasta:yyyyMMdd}{e.Tipo}{e.Motivo}")));
}
