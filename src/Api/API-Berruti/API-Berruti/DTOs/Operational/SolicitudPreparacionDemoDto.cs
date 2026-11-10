using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace API_Berruti.DTOs.Operational;

public class SolicitudPreparacionDemoDto
{
    [JsonRequired] public DateTime Fecha { get; set; }
    [JsonRequired] public TimeSpan HoraSalidaOmbuesRadial { get; set; }
    [JsonRequired] public TimeSpan HoraLlegadaOmbuesRadial { get; set; }
    [JsonRequired, Range(1, int.MaxValue)] public int CapacidadOmbuesRadial { get; set; }
    [JsonRequired, Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal ImporteOmbuesRadial { get; set; }
    [JsonRequired] public DateTime VigenteDesdeOmbuesRadial { get; set; }
    [JsonRequired] public DateTime VigenteHastaOmbuesRadial { get; set; }
    [JsonRequired] public TimeSpan HoraSalidaRadialColonia { get; set; }
    [JsonRequired] public TimeSpan HoraLlegadaRadialColonia { get; set; }
    [JsonRequired, Range(1, int.MaxValue)] public int CapacidadRadialColonia { get; set; }
    [JsonRequired, Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal ImporteRadialColonia { get; set; }
    [JsonRequired] public DateTime VigenteDesdeRadialColonia { get; set; }
    [JsonRequired] public DateTime VigenteHastaRadialColonia { get; set; }
}
