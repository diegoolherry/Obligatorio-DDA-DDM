namespace API_Berruti.Models.Operational;

public class Ticket
{
    public string Id { get; set; } = string.Empty;
    public string CompraServicioId { get; set; } = string.Empty;
    public string QrOpaco { get; set; } = string.Empty;
    public DateTime EmitidoUtc { get; set; }
    // MVP-005: uso único. Nulo = sin usar; ambos campos se escriben juntos, en un solo UPDATE condicional.
    public DateTime? UsadoUtc { get; set; }
    public string? ValidadoPorUsuarioId { get; set; }
}
