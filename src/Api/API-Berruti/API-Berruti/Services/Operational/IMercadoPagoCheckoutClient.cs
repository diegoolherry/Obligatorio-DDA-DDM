using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public interface IMercadoPagoCheckoutClient
{
    Task<OrdenCheckoutMercadoPago> CrearOrdenAsync(
        Guid claveIdempotencia,
        string referenciaExterna,
        decimal importe,
        CancellationToken cancellationToken = default);

    Task<OrdenCheckoutMercadoPago> ConsultarOrdenAsync(
        string ordenId,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default);

    Task<OrdenCheckoutMercadoPago?> BuscarOrdenAsync(
        string referenciaExternaEsperada,
        DateTime desdeUtc,
        DateTime hastaUtc,
        decimal importeEsperado,
        CancellationToken cancellationToken = default);

    Task SolicitarDevolucionCompletaAsync(
        string ordenId,
        Guid claveIdempotencia,
        CancellationToken cancellationToken = default);
}
