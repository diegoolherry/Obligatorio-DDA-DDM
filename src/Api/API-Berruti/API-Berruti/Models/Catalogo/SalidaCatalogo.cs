namespace API_Berruti.Models.Catalogo;

// Salida publicada de una ruta para un tipo de día. Codigos conserva la leyenda del horario (O, C, M, P, S, SE).
public class SalidaCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string RutaId { get; set; } = string.Empty;
    public DiasOperacion Dias { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
    // FIN-008: eliminar desde la administración es una baja lógica; los Servicios ya generados se conservan.
    public bool Activo { get; set; } = true;
}
