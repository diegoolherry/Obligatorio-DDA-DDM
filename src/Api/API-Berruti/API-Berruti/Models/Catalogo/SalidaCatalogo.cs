namespace API_Berruti.Models.Catalogo;

// Salida publicada de una ruta para un tipo de día. Codigos conserva la leyenda del horario (O, C, M, P, S, SE).
public class SalidaCatalogo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string RutaId { get; set; } = string.Empty;
    public DiasOperacion Dias { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
}
