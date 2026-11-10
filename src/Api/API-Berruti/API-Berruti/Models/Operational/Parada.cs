namespace API_Berruti.Models.Operational;

public class Parada
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Nombre { get; set; } = string.Empty;
}
