namespace API_Berruti.Models.Operational;

public class Tarifa
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string TramoId { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public DateTime VigenteDesde { get; set; }
    public DateTime VigenteHasta { get; set; }
}
