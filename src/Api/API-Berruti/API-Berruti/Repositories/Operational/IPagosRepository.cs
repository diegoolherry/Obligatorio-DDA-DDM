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
}
