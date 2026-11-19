using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Operational;

public sealed class CompraConfirmadaResponseDto
{
    public string CompraId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public string PagoId { get; set; } = string.Empty;
    public DateTime ConfirmadoUtc { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<TicketEmitidoDto> Tickets { get; set; } = [];
}
