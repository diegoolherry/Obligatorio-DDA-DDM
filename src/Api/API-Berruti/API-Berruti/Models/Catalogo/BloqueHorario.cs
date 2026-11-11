namespace API_Berruti.Models.Catalogo;

// Bloque del horario publicado (por ejemplo, «Línea Nueva Palmira / Colonia»). La vigencia es propia de
// cada bloque: un bloque sin fechas publicadas no hereda la de otro.
public class BloqueHorario
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    // URL de la publicación de la que se transcribió el bloque.
    public string FuenteUrl { get; set; } = string.Empty;
}
