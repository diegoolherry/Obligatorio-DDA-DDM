using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Operational;

// sinUsar | utilizado | vencido (camelCase en JSON).
public enum EstadoTicket
{
    SinUsar,
    Utilizado,
    Vencido
}

public sealed class TicketPasajeroDto
{
    public string TicketId { get; set; } = string.Empty;
    public EstadoTicket Estado { get; set; }
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
    public int? NumeroAsiento { get; set; }
    public string QrOpaco { get; set; } = string.Empty;
    // Hora UTC del uso; solo con estado «utilizado».
    public DateTime? UsadoUtc { get; set; }
}

public sealed class ListaTicketsPasajeroDto
{
    // Sin usar y no vencidos, por salida ascendente.
    public IReadOnlyList<TicketPasajeroDto> Vigentes { get; set; } = [];
    // Utilizados y vencidos, el más reciente primero.
    public IReadOnlyList<TicketPasajeroDto> Anteriores { get; set; } = [];
}
