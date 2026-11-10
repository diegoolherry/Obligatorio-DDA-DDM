namespace Web_App.Models.Operational;

public class TramoDto
{
    public string? Id { get; set; }
    public string OrigenId { get; set; } = string.Empty;
    public string DestinoId { get; set; } = string.Empty;
    public string? OrigenNombre { get; set; }
    public string? DestinoNombre { get; set; }
}
