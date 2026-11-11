namespace API_Berruti.Models.Catalogo;

// Ruta dirigida: un sentido y una variante (por ejemplo, «Nueva Palmira → Colonia, por Polancos»).
public class RutaCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string BloqueId { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
