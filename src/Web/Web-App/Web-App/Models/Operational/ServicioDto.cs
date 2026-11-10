using System.ComponentModel.DataAnnotations;

namespace Web_App.Models.Operational;

public class ServicioDto
{
    public string? Id { get; set; }
    [Required] public string TramoId { get; set; } = string.Empty;
    [Required] public DateTime Fecha { get; set; }
    [Required] public TimeSpan HoraSalida { get; set; }
    [Required] public TimeSpan HoraLlegada { get; set; }
    [Range(1, int.MaxValue)] public int Capacidad { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
}
