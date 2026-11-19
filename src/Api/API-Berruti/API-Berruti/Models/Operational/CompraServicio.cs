namespace API_Berruti.Models.Operational;

public class CompraServicio
{
    public string Id { get; set; } = string.Empty;
    public string CompraId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public string PagoId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public decimal Importe { get; set; }
}
