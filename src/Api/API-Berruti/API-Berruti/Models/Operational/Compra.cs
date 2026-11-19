namespace API_Berruti.Models.Operational;

public class Compra
{
    public string Id { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public string PagoId { get; set; } = string.Empty;
    public decimal ImporteTotal { get; set; }
    public DateTime ConfirmadoUtc { get; set; }
}
