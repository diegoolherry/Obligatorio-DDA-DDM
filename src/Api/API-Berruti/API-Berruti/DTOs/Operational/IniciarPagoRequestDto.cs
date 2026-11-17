using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public sealed class IniciarPagoRequestDto
{
    [Required]
    [RegularExpression("^[a-fA-F0-9]{64}$")]
    public string HuellaCotizacionAceptada { get; set; } = string.Empty;
}
