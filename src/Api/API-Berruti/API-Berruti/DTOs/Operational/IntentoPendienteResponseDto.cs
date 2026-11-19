namespace API_Berruti.DTOs.Operational;

/// <summary>
/// Intento de compra del pasajero que todavía requiere atención: hold vigente o pago no terminal.
/// <see cref="Pago"/> es null cuando el intento aún no tiene pago.
/// </summary>
public sealed class IntentoPendienteResponseDto
{
    public IniciarIntentoCompraResponseDto Intento { get; set; } = new();
    public PagoResponseDto? Pago { get; set; }
}
