using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface ICotizacionesCompraService
{
    Task<CotizarCompraResponseDto> CotizarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default);
}
