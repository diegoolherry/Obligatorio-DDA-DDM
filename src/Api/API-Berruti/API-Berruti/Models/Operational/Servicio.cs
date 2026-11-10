namespace API_Berruti.Models.Operational;

public class Servicio
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string TramoId { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public TimeSpan HoraSalida { get; set; }
    public TimeSpan HoraLlegada { get; set; }
    public int Capacidad { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
}
