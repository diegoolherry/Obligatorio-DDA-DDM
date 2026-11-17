namespace API_Berruti.DTOs.Operational;

public sealed class CotizarCompraResponseDto
{
    public string IntentoId { get; set; } = string.Empty;
    public ItinerarioDto CotizacionOriginal { get; set; } = new();
    public ItinerarioDto CotizacionActual { get; set; } = new();
    public bool CambioPrecio { get; set; }
    public string HuellaCotizacion { get; set; } = string.Empty;
    public DateTime HoldVenceEn { get; set; }
}
