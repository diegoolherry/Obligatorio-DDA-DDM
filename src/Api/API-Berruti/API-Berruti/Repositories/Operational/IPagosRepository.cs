using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IPagosRepository
{
    Task<RegistroPago?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default);

    Task<RegistroPago> ReservarAsync(
        string identificadorPublico,
        CotizarCompraResponseDto cotizacion,
        string huellaAceptada,
        CancellationToken cancellationToken = default);

    Task<RegistroPago> RegistrarOrdenAsync(
        string identificadorPublico,
        string pagoId,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rechaza un pago reservado cuyo envío al proveedor falló de forma definitiva antes de crear la orden.
    /// Solo aplica a pagos desconocidos, sin orden del proveedor y sin importe pagado.
    /// </summary>
    Task<RegistroPago> RechazarSinOrdenAsync(
        string identificadorPublico,
        string pagoId,
        CancellationToken cancellationToken = default);
}
