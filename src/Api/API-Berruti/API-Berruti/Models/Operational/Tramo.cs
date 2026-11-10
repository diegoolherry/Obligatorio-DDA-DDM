namespace API_Berruti.Models.Operational;

public class Tramo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string OrigenId { get; set; } = string.Empty;
    public string DestinoId { get; set; } = string.Empty;
}
