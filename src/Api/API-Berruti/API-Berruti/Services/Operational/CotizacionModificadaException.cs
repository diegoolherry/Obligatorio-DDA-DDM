using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public sealed class CotizacionModificadaException(CotizarCompraResponseDto cotizacion)
    : InvalidOperationException("La cotización cambió y debe ser aceptada nuevamente.")
{
    public CotizarCompraResponseDto Cotizacion { get; } = cotizacion;
}
