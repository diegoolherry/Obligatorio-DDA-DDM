using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Mvc;

// FIN-012: un bloqueo de fila que vence en MySQL llega como DbUpdateException; la escritura debe responder 409, no 500.
internal static class LockTimeoutChecks
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var controller = new ParadasController(new ParadasQueFallaPorBloqueo());
        var actualizar = await controller.Actualizar("p1", new ParadaDto { Nombre = "X" });
        check(actualizar.Result is ConflictObjectResult, "FIN-012 PUT parada con bloqueo vencido responde 409");
        var eliminar = await controller.Eliminar("p1");
        check(eliminar is ConflictObjectResult, "FIN-012 DELETE parada con bloqueo vencido responde 409");
    }

    private sealed class ParadasQueFallaPorBloqueo : IParadasService
    {
        public Task<IReadOnlyList<ParadaDto>> ListarAsync() => throw new NotSupportedException();
        public Task<ParadaDto> CrearAsync(ParadaDto dto) => throw new NotSupportedException();
        public Task<ParadaDto> ActualizarAsync(string id, ParadaDto dto) => throw new Microsoft.EntityFrameworkCore.DbUpdateException();
        public Task EliminarAsync(string id) => throw new Microsoft.EntityFrameworkCore.DbUpdateException();
    }
}
