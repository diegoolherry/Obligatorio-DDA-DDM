using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Operational;

public sealed class PagoResponseDto
{
    public string PagoId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public EstadoPago Estado { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = "UYU";
    public string? CheckoutUrl { get; set; }
    public DateTime HoldVenceEn { get; set; }
}
