namespace API_Berruti.Models.Operational;

public class Ticket
{
    public string Id { get; set; } = string.Empty;
    public string CompraServicioId { get; set; } = string.Empty;
    public string QrOpaco { get; set; } = string.Empty;
    public DateTime EmitidoUtc { get; set; }
}
