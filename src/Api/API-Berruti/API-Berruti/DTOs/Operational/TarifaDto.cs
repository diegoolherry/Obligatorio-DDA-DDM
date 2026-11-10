using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public class TarifaDto
{
    public string? Id { get; set; }
    [Required] public string TramoId { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal Importe { get; set; }
    [Required] public DateTime VigenteDesde { get; set; }
    [Required] public DateTime VigenteHasta { get; set; }
}
