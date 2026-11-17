using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IConciliacionesPagoService
{
    Task<SincronizarPagoResponseDto> SincronizarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default);
}
