namespace API_Berruti.DTOs.Location;

public class ServicioUbicacionDto
{
    public string Id { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    // Hora local de Uruguay, igual que ServicioDto.
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
}
