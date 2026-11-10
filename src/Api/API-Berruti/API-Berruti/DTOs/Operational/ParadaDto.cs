using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public class ParadaDto
{
    public string? Id { get; set; }
    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; set; } = string.Empty;
}
