using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public interface IMercadoPagoCheckoutClient
{
    Task<OrdenCheckoutMercadoPago> ConsultarOrdenAsync(
        string ordenId,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default);
}
