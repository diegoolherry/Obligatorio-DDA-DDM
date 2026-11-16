using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public class SeleccionTramoCompraDto
{
    [Range(1, int.MaxValue)]
    public int Orden { get; set; }

    [Required]
    public string ServicioId { get; set; } = string.Empty;
}
