namespace API_Berruti.DTOs.Operational;

public sealed class SincronizarPagoResponseDto
{
    public PagoResponseDto Pago { get; set; } = new();
    public CompraConfirmadaResponseDto? Compra { get; set; }
}
