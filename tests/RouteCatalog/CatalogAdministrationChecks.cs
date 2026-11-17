using System.Reflection;
using API_Berruti.Controllers.Catalogo;
using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// FIN-008-01: administration of bloques, rutas (ordered stops) and salidas (increasing times),
// checked through the controller so status codes and messages are part of the contract.
internal static class CatalogAdministrationChecks
{
    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        CheckAuthorization(check);
        var (datos, repo, controller) = Fixture(json);
        var paradas = datos.Paradas.Take(3).Select(p => p.Id).ToList();

        // Bloques.
        var bloqueOk = new BloqueEscrituraDto { Clave = "TEST", Nombre = "Bloque de prueba", Empresa = "Berrutti", VigenteDesde = new(2026, 1, 1), VigenteHasta = new(2026, 12, 31) };
        var creado = await controller.CrearBloque(bloqueOk);
        var bloque = (creado.Result as CreatedAtActionResult)?.Value as BloqueAdministracionDto;
        check(bloque is not null && datos.Bloques.Any(b => b.Id == bloque.Id && b.Clave == "TEST" && b.Activo), "admin creates a bloque (201)");
        await Rejects(check, datos, repo, () => controller.CrearBloque(new() { Clave = "OTRO", Nombre = "X", Empresa = "Y", VigenteDesde = new(2026, 2, 1), VigenteHasta = new(2026, 1, 1) }),
            400, "La vigencia hasta no puede ser anterior a la vigencia desde.");
        await Rejects(check, datos, repo, () => controller.CrearBloque(new() { Nombre = "X", Empresa = "Y" }), 400, "La clave es obligatoria y tiene hasta 16 caracteres.");
        await Rejects(check, datos, repo, () => controller.CrearBloque(bloqueOk), 409, "Ya existe un bloque con la clave TEST.");
        var editado = await controller.ActualizarBloque(bloque!.Id, new() { Clave = "TEST", Nombre = "Renombrado", Empresa = "Berrutti" });
        check(editado.Result is OkObjectResult && datos.Bloques.Single(b => b.Id == bloque.Id).Nombre == "Renombrado", "admin updates a bloque (200)");
        await Rejects(check, datos, repo, () => controller.ActualizarBloque("no-existe", bloqueOk), 404, null);

        // Rutas: stops keep their order; invalid lists are rejected without saving.
        var rutaOk = new RutaEscrituraDto { BloqueId = bloque.Id, Clave = "TEST-IDA", Familia = "Prueba", Nombre = "A → C", ParadaIds = paradas };
        await Rejects(check, datos, repo, () => controller.CrearRuta(new() { BloqueId = bloque.Id, Clave = "R1", Familia = "F", Nombre = "N", ParadaIds = [paradas[0]] }),
            400, "Una ruta necesita al menos dos paradas.");
        await Rejects(check, datos, repo, () => controller.CrearRuta(new() { BloqueId = bloque.Id, Clave = "R1", Familia = "F", Nombre = "N", ParadaIds = [paradas[0], "no-existe"] }),
            400, "La parada no-existe no existe.");
        await Rejects(check, datos, repo, () => controller.CrearRuta(new() { BloqueId = bloque.Id, Clave = "R1", Familia = "F", Nombre = "N", ParadaIds = [paradas[0], paradas[0], paradas[1]] }),
            400, "Una ruta no puede repetir la misma parada de forma consecutiva.");
        await Rejects(check, datos, repo, () => controller.CrearRuta(new() { BloqueId = "no-existe", Clave = "R1", Familia = "F", Nombre = "N", ParadaIds = paradas }),
            400, "El bloque no existe o fue dado de baja.");
        var rutaCreada = (await controller.CrearRuta(rutaOk)).Result as CreatedAtActionResult;
        var ruta = rutaCreada?.Value as RutaAdministracionDto;
        check(ruta is not null && ruta.Paradas.Select(p => p.ParadaId).SequenceEqual(paradas) && ruta.Paradas.Select(p => p.Orden).SequenceEqual([1, 2, 3]),
            "admin creates a ruta with its stops ordered 1..n");
        await Rejects(check, datos, repo, () => controller.CrearRuta(rutaOk), 409, "Ya existe una ruta con la clave TEST-IDA.");

        // Salidas: the first stop needs a time and times strictly increase along the route.
        SalidaEscrituraDto Salida(params (int Orden, int Hora, int Minuto)[] horas) => new()
        {
            RutaId = ruta!.Id, Dias = DiasOperacion.LunesAViernes,
            Horas = horas.Select(h => new HoraEscrituraDto { Orden = h.Orden, Hora = new TimeOnly(h.Hora, h.Minuto) }).ToList()
        };
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((2, 8, 0), (3, 9, 0))), 400, "La primera parada de la ruta debe tener hora de salida.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((1, 8, 0), (2, 8, 0))), 400, "Las horas deben ser crecientes a lo largo de la ruta.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((1, 8, 0), (3, 7, 50))), 400, "Las horas deben ser crecientes a lo largo de la ruta.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((1, 8, 0), (4, 9, 0))), 400, "La hora de orden 4 no corresponde a una parada de la ruta.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((1, 8, 0), (1, 9, 0))), 400, "Hay más de una hora para la parada de orden 1.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(Salida((1, 8, 0))), 400, "Una salida necesita al menos dos horas.");
        await Rejects(check, datos, repo, () => controller.CrearSalida(new() { RutaId = ruta!.Id, Horas = Salida((1, 8, 0), (2, 9, 0)).Horas }), 400, "Los días de operación son obligatorios.");
        var salida = ((await controller.CrearSalida(Salida((1, 8, 0), (3, 9, 30)))).Result as CreatedAtActionResult)?.Value as SalidaAdministracionDto;
        check(salida is not null && datos.Horas.Where(h => h.SalidaId == salida.Id).Select(h => (h.Orden, h.Hora)).SequenceEqual([(1, new TimeSpan(8, 0, 0)), (3, new TimeSpan(9, 30, 0))]),
            "admin creates a salida with increasing times (201)");
        await Rejects(check, datos, repo, () => controller.ActualizarSalida(salida!.Id, new() { RutaId = datos.Rutas[0].Id, Dias = DiasOperacion.Sabados, Horas = Salida((1, 8, 0), (2, 9, 0)).Horas }),
            400, "Una salida no puede cambiar de ruta.");

        // S5: a salida with generated services can be edited; the change applies to future generations.
        repo.Servicios[salida!.Id] = 3;
        var listado = (await controller.Obtener()).Result as OkObjectResult;
        check((listado?.Value as CatalogoAdministracionDto)?.Bloques.SelectMany(b => b.Rutas).SelectMany(r => r.Salidas).Single(s => s.Id == salida.Id).ServiciosGenerados == 3,
            "the admin listing reports generated services per salida");
        var cambio = await controller.ActualizarSalida(salida.Id, Salida((1, 8, 10), (2, 8, 40), (3, 9, 40)));
        check(cambio.Result is OkObjectResult && datos.Horas.Count(h => h.SalidaId == salida.Id) == 3, "a salida with generated services can be edited (200)");

        // S7: the stop list cannot change while the route has active salidas.
        await Rejects(check, datos, repo, () => controller.ActualizarRuta(ruta!.Id, new() { BloqueId = bloque.Id, Clave = "TEST-IDA", Familia = "Prueba", Nombre = "A → C", ParadaIds = [paradas[1], paradas[0], paradas[2]] }),
            409, "La ruta tiene salidas activas: dalas de baja antes de cambiar sus paradas.");
        var renombrada = await controller.ActualizarRuta(ruta!.Id, rutaOk);
        check(renombrada.Result is OkObjectResult, "a ruta with salidas can be updated while its stops stay the same");

        // S4: deleting is a logical deactivation; generated services and rows stay.
        var baja = await controller.EliminarSalida(salida.Id);
        check(baja is NoContentResult && datos.Salidas.Single(s => s.Id == salida.Id).Activo == false && datos.Horas.Any(h => h.SalidaId == salida.Id),
            "deleting a salida marks it inactive (204) and keeps its rows");
        check(await controller.EliminarSalida(salida.Id) is NotFoundResult, "deleting an inactive salida returns 404");
        var reordenada = await controller.ActualizarRuta(ruta.Id, new() { BloqueId = bloque.Id, Clave = "TEST-IDA", Familia = "Prueba", Nombre = "A → C", ParadaIds = [paradas[2], paradas[1], paradas[0]] });
        check(reordenada.Result is OkObjectResult && datos.ParadasRuta.Where(p => p.RutaId == ruta.Id).OrderBy(p => p.Orden).Select(p => p.ParadaId).SequenceEqual([paradas[2], paradas[1], paradas[0]]),
            "without active salidas the route stops can be reordered");
        check(await controller.EliminarRuta(ruta.Id) is NoContentResult && !datos.Rutas.Single(r => r.Id == ruta.Id).Activo, "deleting a ruta marks it inactive (204)");
        check(await controller.EliminarBloque(bloque.Id) is NoContentResult && !datos.Bloques.Single(b => b.Id == bloque.Id).Activo, "deleting a bloque marks it inactive (204)");
        await Rejects(check, datos, repo, () => controller.CrearRuta(new() { BloqueId = bloque.Id, Clave = "OTRA", Familia = "Prueba", Nombre = "A → C", ParadaIds = paradas }), 400, "El bloque no existe o fue dado de baja.");
        var publico = await new CatalogoService(new MemoryCatalogoRepository(datos)).ObtenerAsync();
        check(publico.Bloques.All(b => b.Clave != "TEST"), "a deleted bloque is hidden from the public catalog");
        check(await controller.EliminarBloque("no-existe") is NotFoundResult, "deleting an unknown bloque returns 404");
    }

    private static void CheckAuthorization(Action<bool, string> check)
    {
        var tipo = typeof(CatalogoAdministracionController);
        check(tipo.GetCustomAttribute<AuthorizeAttribute>()?.Roles == "Administrador", "only administrators reach the catalog administration");
        check(tipo.GetCustomAttribute<AllowAnonymousAttribute>() is null
            && tipo.GetMethods().All(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is null && m.GetCustomAttribute<AuthorizeAttribute>() is null),
            "no catalog administration action overrides the administrator requirement");
        check(tipo.GetCustomAttribute<RouteAttribute>()?.Template == "api/administracion/catalogo", "catalog administration lives under api/administracion/catalogo");
    }

    private static (DatosCatalogo, MemoryAdministracionRepository, CatalogoAdministracionController) Fixture(string json)
    {
        var datos = Catalog.Data(Catalog.Build(json));
        var repo = new MemoryAdministracionRepository(datos);
        return (datos, repo, new CatalogoAdministracionController(new CatalogoAdministracionService(new MemoryCatalogoRepository(datos), repo)));
    }

    // A rejected request returns the expected status and message and leaves the stored catalog unchanged.
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
            $"rejected with {estado} without saving: {mensaje ?? "not found"}");
    }

    private static string Snapshot(DatosCatalogo d) => string.Join("|",
        d.Bloques.Select(b => $"{b.Id}{b.Clave}{b.Nombre}{b.Activo}"), d.Rutas.Select(r => $"{r.Id}{r.Clave}{r.Nombre}{r.Activo}"),
        string.Join(",", d.ParadasRuta.Select(p => $"{p.RutaId}{p.Orden}{p.ParadaId}")), string.Join(",", d.Salidas.Select(s => $"{s.Id}{s.Dias}{s.Activo}")),
        string.Join(",", d.Horas.Select(h => $"{h.SalidaId}{h.Orden}{h.Hora}")));
}

internal sealed class MemoryAdministracionRepository(DatosCatalogo datos) : ICatalogoAdministracionRepository
{
    public Dictionary<string, int> Servicios { get; } = [];
    public int Saves { get; private set; }

    public Task<Dictionary<string, int>> ContarServiciosPorSalidaAsync() => Task.FromResult(new Dictionary<string, int>(Servicios));
    public Task AgregarBloqueAsync(BloqueHorario bloque) { Saves++; datos.Bloques.Add(bloque); return Task.CompletedTask; }
    public Task ActualizarBloqueAsync(BloqueHorario bloque) { Saves++; Replace(datos.Bloques, b => b.Id == bloque.Id, bloque); return Task.CompletedTask; }
    public Task AgregarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta> paradas) { Saves++; datos.Rutas.Add(ruta); datos.ParadasRuta.AddRange(paradas); return Task.CompletedTask; }
    public Task ActualizarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta>? paradas)
    {
        Saves++;
        Replace(datos.Rutas, r => r.Id == ruta.Id, ruta);
        if (paradas is not null) { datos.ParadasRuta.RemoveAll(p => p.RutaId == ruta.Id); datos.ParadasRuta.AddRange(paradas); }
        return Task.CompletedTask;
    }
    public Task AgregarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas) { Saves++; datos.Salidas.Add(salida); datos.Horas.AddRange(horas); return Task.CompletedTask; }
    public Task ActualizarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas)
    {
        Saves++;
        Replace(datos.Salidas, s => s.Id == salida.Id, salida);
        datos.Horas.RemoveAll(h => h.SalidaId == salida.Id);
        datos.Horas.AddRange(horas);
        return Task.CompletedTask;
    }

    private static void Replace<T>(List<T> lista, Predicate<T> criterio, T nuevo) => lista[lista.FindIndex(criterio)] = nuevo;
}
