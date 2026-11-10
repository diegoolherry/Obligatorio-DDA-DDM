namespace API_Berruti.Models.Operational;

public class Servicio
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string TramoId { get; set; } = string.Empty;
    // Hora local de Uruguay, sin conversión a UTC.
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public int Capacidad { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
}
