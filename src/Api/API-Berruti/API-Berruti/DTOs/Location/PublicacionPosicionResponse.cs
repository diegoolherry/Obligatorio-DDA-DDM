namespace API_Berruti.DTOs.Location;

public class PublicacionPosicionResponse
{
    public string Id { get; set; } = string.Empty;
    public string ServicioId { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public DateTime RecibidaUtc { get; set; }
    // Segundos desde la publicación aceptada anterior del servicio; null en la primera.
    public double? IntervaloSegundos { get; set; }
    public int PeriodicidadSegundos { get; set; }
    // Contraste con la periodicidad académica (más su margen); null en la primera publicación.
    public bool? CumplePeriodicidad { get; set; }
}
