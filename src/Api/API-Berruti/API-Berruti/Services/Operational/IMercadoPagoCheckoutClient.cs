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

    /// <summary>
    /// Cancela en el proveedor una orden sin pagar (POST /v1/orders/{id}/cancel). Solo prospera si la orden
    /// sigue en estado <c>created</c> sin importe pagado y si la respuesta verificada queda <c>canceled</c>;
    /// en cualquier otro caso lanza y no devuelve una orden.
    /// </summary>
    Task<OrdenCheckoutMercadoPago> CancelarOrdenAsync(
        string ordenId,
        Guid claveIdempotencia,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default);
}
