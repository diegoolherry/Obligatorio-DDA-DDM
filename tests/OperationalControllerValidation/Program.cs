using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Mvc;

int checks = 0, failures = 0;
void Check(bool condition, string name)
{
    checks++;
    if (!condition) { failures++; Console.Error.WriteLine($"FAIL {name}"); }
    else Console.WriteLine($"PASS {name}");
}

try
{
    var service = new FakeParadasService();
    var controller = new ParadasController(service);
    service.Update = _ => Task.FromException<ParadaDto>(new ArgumentException("Nombre inválido."));
    var invalid = await controller.Actualizar("parada-1", new ParadaDto { Nombre = "" });
    Check(invalid.Result is BadRequestObjectResult { StatusCode: 400 } bad && Equals(bad.Value, "Los datos de la parada no son válidos."),
        "expected validation ArgumentException maps to safe 400");
    Check(service.UpdateCalls == 1, "validation request reaches actual service exactly once");

    service.Update = _ => Task.FromResult(new ParadaDto { Id = "parada-1", Nombre = "Ombúes" });
    var success = await controller.Actualizar("parada-1", new ParadaDto { Nombre = "Ombúes" });
    Check(success.Result is OkObjectResult { StatusCode: 200 }, "valid update preserves 200");

    service.Update = _ => Task.FromException<ParadaDto>(new KeyNotFoundException());
    var missing = await controller.Actualizar("missing", new ParadaDto { Nombre = "X" });
    Check(missing.Result is NotFoundResult { StatusCode: 404 }, "missing update preserves 404");
}
catch (Exception error)
{
    failures++;
    Console.Error.WriteLine($"FAIL unhandled managed exception: {error.GetType().FullName}: {error.Message}");
}

Console.WriteLine($"{checks - failures}/{checks} PASS");
return failures == 0 ? 0 : 1;

sealed class FakeParadasService : IParadasService
{
    public Func<string, Task<ParadaDto>> Update { get; set; } = _ => throw new InvalidOperationException("Unexpected update");
    public int UpdateCalls { get; private set; }
    public Task<IReadOnlyList<ParadaDto>> ListarAsync() => Task.FromResult<IReadOnlyList<ParadaDto>>([]);
    public Task<ParadaDto> CrearAsync(ParadaDto dto) => Task.FromResult(dto);
    public Task<ParadaDto> ActualizarAsync(string id, ParadaDto dto) { UpdateCalls++; return Update(id); }
    public Task EliminarAsync(string id) => Task.CompletedTask;
}

