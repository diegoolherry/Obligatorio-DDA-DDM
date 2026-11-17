namespace API_Berruti.Models.Operational;

public sealed record OrdenCheckoutMercadoPago
{
    public required string Id { get; init; }
    public required string ReferenciaExterna { get; init; }
    public required decimal Importe { get; init; }
    public required decimal ImportePagado { get; init; }
    public required string Moneda { get; init; }
    public required EstadoPago Estado { get; init; }
}
