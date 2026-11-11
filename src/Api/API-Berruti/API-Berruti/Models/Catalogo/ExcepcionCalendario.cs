namespace API_Berruti.Models.Catalogo;

public enum TipoExcepcion
{
    SinServicio,
    ConsultarAgencia
}

// Período (fechas inclusivas) en que no se ofrece el horario publicado. BloqueId nulo aplica a todos los bloques.
public class ExcepcionCalendario
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string? BloqueId { get; set; }
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public TipoExcepcion Tipo { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
