namespace Web_App.Models.Operational;

public class TarifaDto
{
    public string? Id { get; set; }
    public string TramoId { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public DateTime VigenteDesde { get; set; }
    public DateTime VigenteHasta { get; set; }
}
