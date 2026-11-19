using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IIntentosCompraService
{
    Task<IniciarIntentoCompraResponseDto> CrearAsync(string identificadorPublico, IniciarIntentoCompraRequestDto solicitud, CancellationToken cancellationToken = default);
    Task<IniciarIntentoCompraResponseDto> ConsultarAsync(string identificadorPublico, string intentoId, CancellationToken cancellationToken = default);
}
