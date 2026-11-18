namespace API_Berruti.Models.Catalogo;

// Ruta dirigida: un sentido y una variante (por ejemplo, «Nueva Palmira → Colonia, por Polancos»).
public class RutaCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string BloqueId { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    // FIN-008: eliminar desde la administración es una baja lógica; los Servicios ya generados se conservan.
    public bool Activo { get; set; } = true;
}
