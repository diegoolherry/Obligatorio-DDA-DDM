using System.Reflection;
using API_Berruti.Controllers.Operational;
using API_Berruti.Data;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Executable checks for the public itinerary search (MVP-003-01).
internal static class ItinerarySearchChecks
{
    private static readonly DateTime Day = new(2031, 5, 12);
    private static DateOnly Date(DateTime day) => DateOnly.FromDateTime(day);

    public static async Task RunAsync(Action<bool, string> check)
    {
        await CheckDemoItineraryAsync(check);
        await CheckTransferWindowAsync(check);
        await CheckSearchDateAsync(check);
        await CheckTariffsAsync(check);
        await CheckAllCombinationsOrderedAsync(check);
        await CheckValidationAsync(check);
        await CheckControllerAsync(check);
        CheckPublicAccess(check);
    }

    private static (MemoryItinerariosRepository Repository, EscenarioOperativo Scenario) Demo()
    {
        var scenario = EscenarioOperativoDemo.Construir(Day);
        return (new MemoryItinerariosRepository(scenario), scenario);
    }

    private static Task<IReadOnlyList<ItinerarioDto>> Search(MemoryItinerariosRepository repository, EscenarioOperativo scenario,
        DateTime? day = null, int origin = 0, int destination = 2) =>
        new ItinerariosService(repository).BuscarAsync(scenario.Paradas[origin].Id, scenario.Paradas[destination].Id, Date(day ?? Day));

    private static async Task CheckDemoItineraryAsync(Action<bool, string> check)
    {
        var (repository, scenario) = Demo();
        var result = await Search(repository, scenario);
        var itinerary = result.Count == 1 ? result[0] : null;
        check(itinerary is not null && itinerary.Tramos.Count == 2, "demo Ombúes→Colonia returns exactly one two-leg itinerary");
        if (itinerary is null || itinerary.Tramos.Count != 2) return;
        var (first, second) = (itinerary.Tramos[0], itinerary.Tramos[1]);
        check(first.Orden == 1 && second.Orden == 2 &&
            first.Origen.Nombre == "Ombúes" && first.Destino.Nombre == "Radial de Conchillas" &&
            second.Origen.Nombre == "Radial de Conchillas" && second.Destino.Nombre == "Colonia" &&
            first.Origen.Id == scenario.Paradas[0].Id && second.Destino.Id == scenario.Paradas[2].Id,
            "demo itinerary legs are ordered and connected through Radial de Conchillas");
        check(itinerary.Transbordo is { Minutos: 15 } transfer && transfer.Parada.Nombre == "Radial de Conchillas" &&
            transfer.Parada.Id == scenario.Paradas[1].Id, "demo itinerary reports a 15-minute transfer at Radial de Conchillas");
        check(first.Salida == Day.AddHours(8) && first.Llegada == Day.AddHours(8).AddMinutes(30) &&
            second.Salida == Day.AddHours(8).AddMinutes(45) && second.Llegada == Day.AddHours(10) &&
            itinerary.Salida == first.Salida && itinerary.Llegada == second.Llegada,
            "demo itinerary keeps local departure and arrival times per leg and overall");
        check(first.Importe == 100.00m && second.Importe == 250.00m && itinerary.PrecioTotal == 350.00m,
            "demo itinerary prices each leg and totals 350");
        check(first.PoliticaAsiento == PoliticaAsiento.SinAsiento && second.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica,
            "demo itinerary reports the seat policy of each leg");
        check(first.ServicioId == scenario.Servicios[0].Id && second.ServicioId == scenario.Servicios[1].Id &&
            first.TramoId == scenario.Tramos[0].Id && second.TramoId == scenario.Tramos[1].Id,
            "demo itinerary references the selected services and tramos");

        var reverse = await Search(repository, scenario, origin: 2, destination: 0);
        check(reverse.Count == 0, "search in the opposite direction returns an empty result, not an error");
    }

    private static async Task CheckTransferWindowAsync(Action<bool, string> check)
    {
        foreach (var (minutes, expected) in new[] { (4, false), (5, true), (30, true), (31, false) })
        {
            var (repository, scenario) = Demo();
            var second = scenario.Servicios[1];
            second.Salida = scenario.Servicios[0].Llegada.AddMinutes(minutes);
            second.Llegada = second.Salida.AddMinutes(75);
            var result = await Search(repository, scenario);
            check(expected
                    ? result.Count == 1 && result[0].Transbordo?.Minutos == minutes
                    : result.Count == 0,
                $"a {minutes}-minute transfer is {(expected ? "accepted" : "rejected")}");
        }

        var (overlap, overlapScenario) = Demo();
        overlapScenario.Servicios[1].Salida = overlapScenario.Servicios[0].Llegada.AddMinutes(-10);
        check((await Search(overlap, overlapScenario)).Count == 0, "a connection departing before the first leg arrives is rejected");
    }

    private static async Task CheckSearchDateAsync(Action<bool, string> check)
    {
        var (repository, scenario) = Demo();
        check((await Search(repository, scenario, Day.AddDays(1))).Count == 0 &&
            (await Search(repository, scenario, Day.AddDays(-1))).Count == 0,
            "services of another date are not offered");

        // First leg 23:40-23:50, second leg 00:00 next day: both legs must depart on the searched date.
        var (overnight, overnightScenario) = Demo();
        var (first, second) = (overnightScenario.Servicios[0], overnightScenario.Servicios[1]);
        first.Salida = Day.AddHours(23).AddMinutes(40); first.Llegada = Day.AddHours(23).AddMinutes(50);
        second.Salida = Day.AddDays(1); second.Llegada = Day.AddDays(1).AddHours(1);
        check((await Search(overnight, overnightScenario)).Count == 0 && (await Search(overnight, overnightScenario, Day.AddDays(1))).Count == 0,
            "a connection whose second leg departs the next day is not offered on either date");

        var (late, lateScenario) = Demo();
        lateScenario.Servicios[1].Salida = Day.AddHours(23).AddMinutes(30);
        lateScenario.Servicios[1].Llegada = Day.AddDays(1).AddMinutes(30);
        lateScenario.Servicios[0].Salida = Day.AddHours(22).AddMinutes(50);
        lateScenario.Servicios[0].Llegada = Day.AddHours(23).AddMinutes(10);
        var lateResult = await Search(late, lateScenario);
        check(lateResult.Count == 1 && lateResult[0].Llegada == Day.AddDays(1).AddMinutes(30),
            "a same-day departure may arrive after midnight");
    }

    private static async Task CheckTariffsAsync(Action<bool, string> check)
    {
        var (missing, missingScenario) = Demo();
        missing.Tariffs.RemoveAll(t => t.TramoId == missingScenario.Tramos[1].Id);
        check((await Search(missing, missingScenario)).Count == 0, "an itinerary with a leg without tariff is excluded");

        var (expired, expiredScenario) = Demo();
        expired.Tariffs[1].VigenteDesde = Day.AddYears(-1); expired.Tariffs[1].VigenteHasta = Day.AddDays(-1);
        check((await Search(expired, expiredScenario)).Count == 0, "a tariff that expired before the date is not used");

        var (future, futureScenario) = Demo();
        future.Tariffs[1].VigenteDesde = Day.AddDays(1);
        check((await Search(future, futureScenario)).Count == 0, "a tariff that starts after the date is not used");

        var (edges, edgesScenario) = Demo();
        edges.Tariffs[0].VigenteDesde = Day; edges.Tariffs[0].VigenteHasta = Day;
        check((await Search(edges, edgesScenario)) is [{ PrecioTotal: 350.00m }], "a tariff valid only on the searched date is used");

        var (several, severalScenario) = Demo();
        var tramo = severalScenario.Tramos[0].Id;
        several.Tariffs[0].VigenteDesde = Day.AddDays(-5);
        several.Tariffs.Add(new Tarifa { TramoId = tramo, Importe = 120.00m, VigenteDesde = Day.AddDays(-1), VigenteHasta = Day.AddDays(10) });
        several.Tariffs.Add(new Tarifa { TramoId = tramo, Importe = 300.00m, VigenteDesde = Day.AddDays(-30), VigenteHasta = Day.AddDays(30) });
        var severalResult = await Search(several, severalScenario);
        check(severalResult is [{ PrecioTotal: 370.00m }] && severalResult[0].Tramos[0].Importe == 120.00m,
            "with several tariffs in force the latest VigenteDesde is used");
    }

    private static async Task CheckAllCombinationsOrderedAsync(Action<bool, string> check)
    {
        var (repository, scenario) = Demo();
        var (ombues, colonia) = (scenario.Paradas[0], scenario.Paradas[2]);
        var direct = new Tramo { OrigenId = ombues.Id, DestinoId = colonia.Id };
        repository.Tramos.Add(direct);
        repository.Tariffs.Add(new Tarifa { TramoId = direct.Id, Importe = 300.00m, VigenteDesde = Day, VigenteHasta = Day.AddYears(1) });
        var directService = new Servicio { TramoId = direct.Id, Salida = Day.AddHours(7), Llegada = Day.AddHours(8).AddMinutes(30), PoliticaAsiento = PoliticaAsiento.SinAsiento };
        var laterConnection = new Servicio { TramoId = scenario.Tramos[1].Id, Salida = Day.AddHours(8).AddMinutes(55), Llegada = Day.AddHours(10).AddMinutes(10), PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica };
        // Inserted out of order to prove the service sorts the result.
        repository.Services.Insert(0, laterConnection);
        repository.Services.Add(directService);

        var result = await Search(repository, scenario);
        check(result.Count == 3, "search returns every direct and two-leg combination");
        if (result.Count != 3) return;
        check(result[0].Tramos is [{ } only] && only.ServicioId == directService.Id && result[0].Transbordo is null && result[0].PrecioTotal == 300.00m,
            "a direct itinerary has one leg, no transfer and its own price");
        check(result[1].Tramos[1].ServicioId == scenario.Servicios[1].Id && result[1].Transbordo?.Minutos == 15 &&
            result[2].Tramos[1].ServicioId == laterConnection.Id && result[2].Transbordo?.Minutos == 25,
            "each valid connection becomes its own itinerary");
        check(result.Select(i => (i.Salida, i.Llegada)).SequenceEqual(result.OrderBy(i => i.Salida).ThenBy(i => i.Llegada).Select(i => (i.Salida, i.Llegada))),
            "itineraries are ordered by departure and then arrival");
    }

    private static async Task CheckValidationAsync(Action<bool, string> check)
    {
        var (repository, scenario) = Demo();
        var service = new ItinerariosService(repository);
        var (ombues, colonia) = (scenario.Paradas[0].Id, scenario.Paradas[2].Id);
        foreach (var (name, origin, destination) in new[] { ("missing origin", (string?)null, colonia), ("blank destination", ombues, " "), ("equal stops", ombues, ombues) })
        {
            var rejected = false;
            try { await service.BuscarAsync(origin, destination, Date(Day)); } catch (ArgumentException) { rejected = true; }
            check(rejected, $"search with {name} is rejected as invalid");
        }
        var notFound = false;
        try { await service.BuscarAsync("parada-inexistente", colonia, Date(Day)); } catch (KeyNotFoundException) { notFound = true; }
        check(notFound, "search with an unknown stop reports not found");
    }

    private static async Task CheckControllerAsync(Action<bool, string> check)
    {
        var (repository, scenario) = Demo();
        var controller = new ItinerariosController(new ItinerariosService(repository));
        var (ombues, colonia) = (scenario.Paradas[0].Id, scenario.Paradas[2].Id);

        foreach (var fecha in new string?[] { null, "", "2031-13-01", "12/05/2031", "2031-05-12T08:00" })
        {
            var callsBefore = repository.Calls;
            var response = await controller.Buscar(ombues, colonia, fecha);
            check(response.Result is BadRequestObjectResult && repository.Calls == callsBefore,
                $"fecha '{fecha ?? "null"}' returns 400 without querying data");
        }
        check((await controller.Buscar(ombues, ombues, "2031-05-12")).Result is BadRequestObjectResult, "equal stops return 400");
        check((await controller.Buscar("parada-inexistente", colonia, "2031-05-12")).Result is NotFoundObjectResult, "unknown stop returns 404");
        var ok = (await controller.Buscar(ombues, colonia, "2031-05-12")).Result as OkObjectResult;
        check(ok?.Value is IReadOnlyList<ItinerarioDto> { Count: 1 }, "valid search returns 200 with the itineraries");
        var empty = (await controller.Buscar(ombues, colonia, "2031-05-13")).Result as OkObjectResult;
        check(empty?.Value is IReadOnlyList<ItinerarioDto> { Count: 0 }, "search without matches returns 200 with an empty list");
    }

    private static void CheckPublicAccess(Action<bool, string> check)
    {
        foreach (var (type, route) in new[] { (typeof(ItinerariosController), "api/itinerarios"), (typeof(ParadasPublicasController), "api/paradas") })
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            check(type.GetCustomAttribute<AllowAnonymousAttribute>() is not null && type.GetCustomAttribute<AuthorizeAttribute>() is null &&
                methods.All(m => m.GetCustomAttribute<AuthorizeAttribute>() is null) &&
                type.GetCustomAttribute<RouteAttribute>()?.Template == route &&
                methods.All(m => m.GetCustomAttribute<HttpGetAttribute>() is not null) &&
                !methods.Any(m => m.GetCustomAttributes().Any(a => a is HttpPostAttribute or HttpPutAttribute or HttpDeleteAttribute)),
                $"{route} is anonymous and read-only");
        }
    }
}

internal sealed class MemoryItinerariosRepository(EscenarioOperativo scenario) : IBusquedaItinerariosRepository
{
    public List<Parada> Stops { get; } = [.. scenario.Paradas];
    public List<Tramo> Tramos { get; } = [.. scenario.Tramos];
    public List<Servicio> Services { get; } = [.. scenario.Servicios];
    public List<Tarifa> Tariffs { get; } = [.. scenario.Tarifas];
    public int Calls { get; private set; }

    // Deliberately unfiltered: the service must enforce the date and tariff rules itself.
    public Task<List<Parada>> ListarParadasAsync() { Calls++; return Task.FromResult(Stops.ToList()); }
    public Task<List<Tramo>> ListarTramosAsync() { Calls++; return Task.FromResult(Tramos.ToList()); }
    public Task<List<Servicio>> ListarServiciosAsync(DateTime desde, DateTime hasta) { Calls++; return Task.FromResult(Services.ToList()); }
    public Task<List<Tarifa>> ListarTarifasVigentesAsync(DateTime dia) { Calls++; return Task.FromResult(Tariffs.ToList()); }
}
