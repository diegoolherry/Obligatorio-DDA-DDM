namespace API_Berruti.DTOs.Operational;

public class IniciarIntentoCompraResponseDto
{
    public string IntentoId { get; set; } = string.Empty;
    public ItinerarioDto Itinerario { get; set; } = new();

    // El contrato expresa esta fecha y hora en UTC.
    public DateTime HoldVenceEn { get; set; }
}
