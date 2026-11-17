using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IPagosService
{
    Task<PagoResponseDto> IniciarAsync(
        string identificadorPublico,
        string intentoId,
        IniciarPagoRequestDto solicitud,
        CancellationToken cancellationToken = default);

    Task<PagoResponseDto?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default);
}
