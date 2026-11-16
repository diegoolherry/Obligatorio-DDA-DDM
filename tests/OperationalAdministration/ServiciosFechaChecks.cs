using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Mvc;

// Executable checks for the optional `fecha` filter of GET /api/administracion/servicios (FIN-010-03 / S7).
internal static class ServiciosFechaChecks
{
    private static readonly DateTime Day = new(2031, 5, 12);

    public static async Task RunAsync(Action<bool, string> check)
    {
        var repository = new MemoryServiciosRepository();
        // Out of order on purpose: the filter must also order by Salida.
        foreach (var (id, salida) in new[]
        {
            ("s-tarde", Day.AddHours(22)), ("s-ayer", Day.AddMinutes(-1)), ("s-manana", Day.AddDays(1)),
            ("s-medianoche", Day), ("s-temprano", Day.AddHours(6)), ("s-ultimo-minuto", Day.AddDays(1).AddTicks(-1))
        })
            repository.Services.Add(new Servicio { Id = id, TramoId = "tramo-test", Salida = salida, Llegada = salida.AddHours(1), Capacidad = 30 });
        var controller = new ServiciosController(new ServiciosService(repository));

        var withDate = await controller.Listar("2031-05-12");
        var ids = (withDate.Result as OkObjectResult)?.Value is IReadOnlyList<ServicioDto> list ? list.Select(s => s.Id).ToArray() : null;
        check(ids is not null && ids.SequenceEqual(["s-medianoche", "s-temprano", "s-tarde", "s-ultimo-minuto"]),
            "servicios with fecha returns only that local day, ordered by Salida, inclusive of 00:00 and exclusive of the next day");

        var all = await controller.Listar();
        var allIds = (all.Result as OkObjectResult)?.Value is IReadOnlyList<ServicioDto> every ? every.Select(s => s.Id).ToArray() : null;
        check(allIds is not null && allIds.Length == 6 && allIds.OrderBy(i => i).SequenceEqual(repository.Services.Select(s => s.Id).OrderBy(i => i)),
            "servicios without fecha still returns every service (unchanged)");

        var empty = await controller.Listar("2031-05-20");
        check((empty.Result as OkObjectResult)?.Value is IReadOnlyList<ServicioDto> { Count: 0 }, "servicios with a day without services returns 200 and an empty list");

        foreach (var invalid in new[] { "12/05/2031", "2031-5-12", "2031-13-01", "2031-02-30", "mañana", "", "  ", "2031-05-12T10:00" })
        {
            var calls = repository.ListCalls;
            var response = await controller.Listar(invalid);
            check(response.Result is BadRequestObjectResult { StatusCode: 400, Value: "La fecha debe tener el formato yyyy-MM-dd." } && repository.ListCalls == calls,
                $"servicios with invalid fecha '{invalid}' returns 400 without querying");
        }

        var source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "src/Api/API-Berruti/API-Berruti/Repositories/Operational/ServiciosRepository.cs"));
        var filter = source.IndexOf("Salida >= ", StringComparison.Ordinal);
        check(filter >= 0 && source.Contains("Salida < ") && filter < source.IndexOf("ToListAsync", StringComparison.Ordinal),
            "servicios repository filters Salida in the database query before materializing (structural; MySQL provider not exercised)");
    }
}
