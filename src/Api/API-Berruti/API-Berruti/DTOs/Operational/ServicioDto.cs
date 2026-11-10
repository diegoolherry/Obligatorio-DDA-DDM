using System.ComponentModel.DataAnnotations;
using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Operational;

public class ServicioDto
{
    public string? Id { get; set; }
    [Required] public string TramoId { get; set; } = string.Empty;
    [Required] public DateTime Fecha { get; set; }
    [Required] public TimeSpan HoraSalida { get; set; }
    [Required] public TimeSpan HoraLlegada { get; set; }
    [Range(1, int.MaxValue)] public int Capacidad { get; set; }
    [EnumDataType(typeof(PoliticaAsiento))] public PoliticaAsiento PoliticaAsiento { get; set; }
}
