using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public class TramoDto
{
    public string? Id { get; set; }
    [Required] public string OrigenId { get; set; } = string.Empty;
    [Required] public string DestinoId { get; set; } = string.Empty;
    public string? OrigenNombre { get; set; }
    public string? DestinoNombre { get; set; }
}
