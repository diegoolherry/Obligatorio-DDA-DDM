namespace API_Berruti.Models.Catalogo;

// Ocurrencia ordenada de una parada dentro de una ruta. El orden es local a la ruta:
// órdenes de rutas distintas no se comparan.
public class ParadaRuta
{
    public string RutaId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string ParadaId { get; set; } = string.Empty;
}
