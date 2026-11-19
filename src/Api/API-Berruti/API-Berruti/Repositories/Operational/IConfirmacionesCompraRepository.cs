using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IConfirmacionesCompraRepository
{
    Task<ResultadoConciliacionPago?> ConsultarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default);

    Task<ResultadoConciliacionPago> ConciliarAsync(
        string identificadorPublico,
        string pagoId,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken = default);
}
