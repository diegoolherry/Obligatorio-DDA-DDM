namespace API_Berruti.Models.Operational;

public class IntentoCompra
{
    public string Id { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public DateTime CreadoUtc { get; set; }
    public DateTime HoldVenceUtc { get; set; }
}
