namespace API_Berruti.Models.Operational;

public class TramoPago
{
    public string PagoId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public decimal Importe { get; set; }
}
