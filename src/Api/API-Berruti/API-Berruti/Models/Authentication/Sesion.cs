namespace API_Berruti.Models.Authentication;

public class Sesion
{
    public string Huella { get; set; } = string.Empty;
    public string UsuarioId { get; set; } = string.Empty;
    public DateTime CreadaUtc { get; set; }
    public DateTime VenceUtc { get; set; }
    public DateTime? RevocadaUtc { get; set; }
}
