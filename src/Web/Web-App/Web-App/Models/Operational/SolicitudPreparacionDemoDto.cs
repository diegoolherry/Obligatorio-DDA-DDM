namespace Web_App.Models.Operational;

public class SolicitudPreparacionDemoDto
{
    public DateTime Fecha { get; set; }
    public TimeSpan HoraSalidaOmbuesRadial { get; set; }
    public TimeSpan HoraLlegadaOmbuesRadial { get; set; }
    public int CapacidadOmbuesRadial { get; set; }
    public decimal ImporteOmbuesRadial { get; set; }
    public DateTime VigenteDesdeOmbuesRadial { get; set; }
    public DateTime VigenteHastaOmbuesRadial { get; set; }
    public TimeSpan HoraSalidaRadialColonia { get; set; }
    public TimeSpan HoraLlegadaRadialColonia { get; set; }
    public int CapacidadRadialColonia { get; set; }
    public decimal ImporteRadialColonia { get; set; }
    public DateTime VigenteDesdeRadialColonia { get; set; }
    public DateTime VigenteHastaRadialColonia { get; set; }
}
