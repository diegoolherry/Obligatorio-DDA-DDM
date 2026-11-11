namespace API_Berruti.DTOs.Location;

// Nullable para distinguir una coordenada ausente de 0.
public class PublicarPosicionRequest
{
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
}
