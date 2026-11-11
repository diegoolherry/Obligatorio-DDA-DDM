using System.Text.Json.Nodes;
using API_Berruti.Controllers.Catalogo;
using API_Berruti.Data.Catalogo;
using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Mvc;

// Adversarial checks for FIN-001: hostile transcriptions, calendar edges, generator edges and malformed requests.
internal static class AdversarialChecks
{
    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        TranscriptionAttacks(json, check);
        await GeneratorEdgesAsync(json, check);
        await ControllerInputsAsync(json, check);
        await UnloadedYearAsync(json, check);
    }

    private static JsonNode Doc(string json) => JsonNode.Parse(json)!;
    private static JsonObject Ruta(JsonNode doc, string clave) =>
        doc["bloques"]!.AsArray().SelectMany(b => b!["rutas"]!.AsArray()).Single(r => (string?)r!["clave"] == clave)!.AsObject();
    private static JsonObject Bloque(JsonNode doc, string clave) =>
        doc["bloques"]!.AsArray().Single(b => (string?)b!["clave"] == clave)!.AsObject();
    private static JsonArray PrimerasHoras(JsonNode doc, string ruta) => Ruta(doc, ruta)["salidas"]![0]!["horas"]!.AsArray();

    private static string Rejection(string json)
    {
        try { HorariosPublicados.Leer(json); return "<accepted>"; }
        catch (Exception e) { return e.Message; }
    }

    private static void Rejects(Action<bool, string> check, Func<JsonNode, JsonNode> mutate, string json, string fragment, string name)
    {
        var message = Rejection(mutate(Doc(json)).ToJsonString());
        check(message.Contains(fragment), $"{name} (got: {message.Split(Environment.NewLine).Last()})");
    }

    private static void TranscriptionAttacks(string json, Action<bool, string> check)
    {
        Rejects(check, d => { PrimerasHoras(d, "NP-COL")[1] = "05:20"; return d; }, json, "NP-COL salida 1: las horas deben crecer", "equal consecutive times are rejected");
        foreach (var hora in new[] { "24:00", "5:20", "07:60", "07:05:00", "", "7h05" })
            Rejects(check, d => { PrimerasHoras(d, "NP-COL")[3] = hora; return d; }, json, $"NP-COL salida 1: hora inválida ({hora})", $"malformed time '{hora}' is rejected");
        Rejects(check, d => { Ruta(d, "AGR-NP")["clave"] = "NP-COL"; return d; }, json, "Ruta repetida: NP-COL", "duplicate route keys across blocks are rejected");
        Rejects(check, d => { Bloque(d, "NP-AGR")["clave"] = "NP-CAR"; return d; }, json, "Bloque repetido: NP-CAR", "duplicate block keys are rejected");
        Rejects(check, d => { Ruta(d, "NP-COL")["paradas"]![1] = "Nueva Palmira"; return d; }, json, "NP-COL: parada repetida de forma consecutiva (Nueva Palmira)", "consecutive repeated stops are rejected");
        Rejects(check, d => { Ruta(d, "NP-COL")["paradas"]![2] = " "; return d; }, json, "NP-COL: parada sin nombre", "blank stop names are rejected");
        Rejects(check, d => { var r = Ruta(d, "NP-AGR"); r["paradas"] = new JsonArray("Nueva Palmira"); r["salidas"]![0]!["horas"] = new JsonArray("12:00"); return d; }, json,
            "NP-AGR: una ruta necesita al menos dos paradas", "single-stop routes are rejected");
        Rejects(check, d => { Ruta(d, "NP-AGR")["salidas"] = new JsonArray(); return d; }, json, "NP-AGR: la ruta no tiene salidas", "routes without departures are rejected");
        Rejects(check, d => { Bloque(d, "NP-AGR")["rutas"] = new JsonArray(); return d; }, json, "NP-AGR: el bloque no tiene rutas", "blocks without routes are rejected");
        Rejects(check, d => { Bloque(d, "NP-COL")["vigenteHasta"] = "2026-03-01"; return d; }, json, "NP-COL: vigencia invertida", "inverted block validity is rejected");
        Rejects(check, d => { Bloque(d, "NP-COL")["vigenteDesde"] = "02/03/2026"; return d; }, json, "NP-COL: vigencia desde inválida", "non-ISO validity dates are rejected");
        Rejects(check, d => { d["excepciones"]![0]!["bloque"] = "XX"; return d; }, json, "Excepción de un bloque inexistente: XX", "exceptions for unknown blocks are rejected");
        Rejects(check, d => { d["excepciones"]![1]!["hasta"] = "2026-03-29"; return d; }, json, "Excepción con rango invertido: 2026-03-30–2026-03-29", "inverted exceptions are rejected");
        Rejects(check, d => { d["excepciones"]![1]!["tipo"] = "Paro"; return d; }, json, "Excepción con tipo desconocido: Paro", "unknown exception types are rejected");
        Rejects(check, d => { d["excepciones"]![1]!["motivo"] = ""; return d; }, json, "Excepción sin motivo: 2026-03-30", "exceptions need a reason");
        Rejects(check, d => { d["feriados"]![1]!["fecha"] = "2026-01-01"; return d; }, json, "Feriado repetido: 2026-01-01", "duplicate holidays are rejected");
        Rejects(check, d => { d["feriados"]![1]!["fecha"] = "2026-02-30"; return d; }, json, "Feriado con fecha inválida: 2026-02-30", "impossible holiday dates are rejected");
        Rejects(check, d => { d["fuente"]!["url"] = ""; return d; }, json, "Falta la URL de la fuente.", "a transcription without source is rejected");
        Rejects(check, d => { d["bloques"] = new JsonArray(); return d; }, json, "La transcripción no tiene bloques.", "an empty catalog is rejected");
        check(Rejection("null") != "<accepted>" && Rejection("{") != "<accepted>" && Rejection("{}") != "<accepted>", "null, truncated and empty documents are rejected");

        // Stops are reused by exact name, so the demo scenario and the catalog share Colonia.
        var colonia = new Parada { Nombre = "Colonia" };
        var sembrado = HorariosPublicados.Construir(HorariosPublicados.Leer(json), [colonia]);
        check(!sembrado.ParadasNuevas.Any(p => p.Nombre == "Colonia") && sembrado.ParadasRuta.Any(p => p.ParadaId == colonia.Id), "existing stops are reused by name");
        check(sembrado.ParadasNuevas.Select(p => p.Nombre).Distinct().Count() == sembrado.ParadasNuevas.Count, "each new stop is created once");
    }

    private static GenerarServiciosDto Rango(string desde, string hasta) => new()
    {
        Desde = DateOnly.Parse(desde), Hasta = DateOnly.Parse(hasta), Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento
    };

    private static async Task GeneratorEdgesAsync(string json, Action<bool, string> check)
    {
        var sembrado = Catalog.Build(json);
        var datos = Catalog.Data(sembrado);
        var paradas = sembrado.ParadasNuevas.ToDictionary(p => p.Id, p => p.Nombre);
        MemoryGeneracionRepository Repo() => new();
        GeneracionServiciosService Service(MemoryGeneracionRepository r) => new(new MemoryCatalogoRepository(datos), r);
        string Origen(MemoryGeneracionRepository r, Servicio s) => paradas[r.Tramos.Single(t => t.Id == s.TramoId).OrigenId];
        string Destino(MemoryGeneracionRepository r, Servicio s) => paradas[r.Tramos.Single(t => t.Id == s.TramoId).DestinoId];

        var fueraDeRango = new GenerarServiciosDto { Desde = new DateOnly(2026, 11, 9), Hasta = new DateOnly(2026, 11, 9), Capacidad = 40, PoliticaAsiento = (PoliticaAsiento)99 };
        var vacio = Repo();
        var respuesta = await new GeneracionServiciosController(Service(vacio)).Generar(fueraDeRango);
        check(respuesta.Result is BadRequestObjectResult && vacio.Saves == 0, "an undefined seat policy is rejected without saving");

        // Crossing the end of the main block: Palmira–Colonia stops on 20/12, Carmelo–Mercedes keeps running.
        var fin = Repo();
        await Service(fin).GenerarAsync(Rango("2026-12-19", "2026-12-22"));
        check(!fin.Servicios.Any(s => s.Salida.Date > new DateTime(2026, 12, 20) && Destino(fin, s) == "Colonia"), "no Colonia services after 20/12/26");
        check(fin.Servicios.Any(s => s.Salida.Date == new DateTime(2026, 12, 21) && Destino(fin, s) == "Mercedes"), "Carmelo–Mercedes services continue after 20/12/26");
        check(fin.Servicios.All(s => s.Salida >= new DateTime(2026, 12, 19) && s.Salida < new DateTime(2026, 12, 23) && s.Llegada > s.Salida && s.Llegada.Date == s.Salida.Date),
            "every generated service lies inside the requested range and arrives the same day");

        // Overlapping ranges only add the missing day.
        var solapado = Repo();
        var primero = await Service(solapado).GenerarAsync(Rango("2026-11-09", "2026-11-10"));
        var dia11 = await Service(Repo()).GenerarAsync(Rango("2026-11-11", "2026-11-11"));
        var dia10 = await Service(Repo()).GenerarAsync(Rango("2026-11-10", "2026-11-10"));
        var segundo = await Service(solapado).GenerarAsync(Rango("2026-11-10", "2026-11-11"));
        check(segundo.ServiciosCreados == dia11.ServiciosCreados && segundo.ServiciosExistentes == dia10.ServiciosCreados && segundo.TramosCreados == 0
            && solapado.Servicios.Count == primero.ServiciosCreados + dia11.ServiciosCreados, "overlapping ranges only create the missing day");

        var feriado = Repo();
        await Service(feriado).GenerarAsync(Rango("2026-08-25", "2026-08-25"));
        check(feriado.Servicios.Count > 0 && !feriado.Servicios.Any(s => Destino(feriado, s) is "Mercedes" or "Dolores" || Origen(feriado, s) is "Mercedes" or "Dolores"),
            "a weekday holiday generates no Carmelo–Mercedes services");
        check(feriado.Servicios.Where(s => Origen(feriado, s) == "Nueva Palmira" && Destino(feriado, s) == "Colonia").Select(s => s.Salida.TimeOfDay).Order()
            .SequenceEqual([new TimeSpan(7, 45, 0), new TimeSpan(11, 5, 0), new TimeSpan(17, 15, 0), new TimeSpan(19, 20, 0)]), "a weekday holiday uses the Sunday departures");

        // Overlapping exceptions: "no service" wins over "consult the agency", whatever the load order.
        foreach (var orden in new[] { false, true })
        {
            var superpuesto = Catalog.Build(json);
            var agencia = new API_Berruti.Models.Catalogo.ExcepcionCalendario { Desde = new DateTime(2026, 4, 30), Hasta = new DateTime(2026, 5, 2), Tipo = API_Berruti.Models.Catalogo.TipoExcepcion.ConsultarAgencia, Motivo = "Paro" };
            if (orden) superpuesto.Excepciones.Insert(0, agencia); else superpuesto.Excepciones.Add(agencia);
            var consulta = await new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(superpuesto))).SalidasDelDiaAsync(new DateOnly(2026, 5, 1));
            check(consulta.Bloques.All(x => x.Estado == EstadoDia.SinServicio && x.Motivo == "1° de mayo: no hay servicios"), $"no-service wins over consult-agency (agency first: {orden})");
            var vecino = await new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(superpuesto))).SalidasDelDiaAsync(new DateOnly(2026, 5, 2));
            check(vecino.Bloques.All(x => x.Estado == EstadoDia.ConsultarAgencia), $"the agency exception still applies on the next day (agency first: {orden})");
        }

        var turismo = Repo();
        var semana = await Service(turismo).GenerarAsync(Rango("2026-03-30", "2026-04-05"));
        check(semana.ServiciosCreados == 0 && turismo.Saves == 0, "Tourism Week generates nothing and saves nothing");

        // A route that visits the same stop twice must not create a tramo whose origin equals its destination.
        var circular = Doc(json);
        var ruta = Ruta(circular, "NP-AGR");
        ruta["paradas"] = new JsonArray("Nueva Palmira", "Agraciada", "Nueva Palmira");
        ruta["salidas"] = new JsonArray(new JsonObject { ["dias"] = "LunesYViernes", ["horas"] = new JsonArray("12:00", "12:30", "13:00") });
        var sembradoCircular = HorariosPublicados.Construir(HorariosPublicados.Leer(circular.ToJsonString()), []);
        var repoCircular = Repo();
        await new GeneracionServiciosService(new MemoryCatalogoRepository(Catalog.Data(sembradoCircular)), repoCircular).GenerarAsync(Rango("2026-11-09", "2026-11-09"));
        check(repoCircular.Tramos.All(t => t.OrigenId != t.DestinoId), "a stop visited twice never yields a tramo from a stop to itself");
        var catalogo = await new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(sembradoCircular))).ObtenerAsync();
        check(catalogo.Bloques.SelectMany(b => b.Rutas).Single(r => r.Clave == "NP-AGR").Paradas.Select(p => p.Orden).SequenceEqual([1, 2, 3]),
            "repeated visits keep their own order");
    }

    // Decision (Diego, option a): a year without loaded holidays shows no departures and says so.
    private static async Task UnloadedYearAsync(string json, Action<bool, string> check)
    {
        var datos = Catalog.Data(Catalog.Build(json));
        var service = new CatalogoService(new MemoryCatalogoRepository(datos));
        var anio = await service.SalidasDelDiaAsync(new DateOnly(2027, 1, 1));
        var colonia = anio.Bloques.Single(b => b.Clave == "NP-COL");
        check(colonia.Estado == EstadoDia.FueraDeVigencia && colonia.Salidas.Count == 0, "2027: the main block reports its own validity end");
        check(anio.Bloques.Where(b => b.Clave != "NP-COL").All(b => b.Estado == EstadoDia.CalendarioNoCargado && b.Salidas.Count == 0
            && b.Motivo == "No está cargado el calendario de feriados de 2027."), "2027: blocks without an end date show no departures and say the calendar is not loaded");
        var marzo = await service.SalidasDelDiaAsync(new DateOnly(2027, 3, 3));
        check(marzo.Bloques.Where(b => b.Clave != "NP-COL").All(b => b.Estado == EstadoDia.CalendarioNoCargado), "2027: an ordinary weekday is also not offered");
        check((await service.SalidasDelDiaAsync(new DateOnly(2026, 12, 31))).Bloques.Single(b => b.Clave == "CAR-MER").Estado == EstadoDia.Opera,
            "2026 stays loaded until 31/12");
        var repo = new MemoryGeneracionRepository();
        var generados = await new GeneracionServiciosService(new MemoryCatalogoRepository(datos), repo).GenerarAsync(Rango("2027-01-04", "2027-01-08"));
        check(generados.ServiciosCreados == 0 && repo.Saves == 0, "2027: generation creates nothing");
    }

    private static async Task ControllerInputsAsync(string json, Action<bool, string> check)
    {
        var controller = new CatalogoController(new CatalogoService(new MemoryCatalogoRepository(Catalog.Data(Catalog.Build(json)))));
        foreach (var fecha in new[] { "2026-02-30", "2026-11-11T00:00", " 2026-11-11", "2026-1-5", "", "2026/11/11", "11-11-2026" })
            check((await controller.Salidas(fecha)).Result is BadRequestObjectResult, $"date '{fecha}' returns 400");
        check((await controller.Salidas("2024-02-29")).Result is OkObjectResult { Value: SalidasDelDiaDto { Bloques: var b } } && b.Where(x => x.Clave != "NP-CAR" && x.Clave != "NP-AGR").All(x => x.Salidas.Count == 0),
            "a date before every published validity returns no departures for blocks with validity");
    }
}
