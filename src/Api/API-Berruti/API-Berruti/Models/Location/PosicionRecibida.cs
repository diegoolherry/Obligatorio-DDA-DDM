namespace API_Berruti.Models.Location;

// Publicación aceptada desde el celular del cobrador asignado. Las rechazadas nunca se guardan.
public class PosicionRecibida
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string UsuarioId { get; set; } = string.Empty;
    public string ServicioId { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public DateTime RecibidaUtc { get; set; }
}
