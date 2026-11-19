using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Operational;

public sealed class TicketEmitidoDto
{
    public string TicketId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public decimal Importe { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
    public int? NumeroAsiento { get; set; }
    public string QrOpaco { get; set; } = string.Empty;
}
