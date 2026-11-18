namespace Web_App.Models.Catalogo;

// Contratos de GET/POST/PUT api/administracion/catalogo (FIN-008). Copia de los contratos de la API; no referencia sus entidades.
// Reutiliza DiasOperacion, TipoExcepcion, FeriadoDto y ParadaRutaDto de CatalogoDto.cs.
public class CatalogoAdministracionDto
{
    public List<BloqueAdministracionDto> Bloques { get; set; } = [];
    public List<FeriadoDto> Feriados { get; set; } = [];
    public List<ExcepcionAdministracionDto> Excepciones { get; set; } = [];
}

public class BloqueAdministracionDto
{
    public string Id { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public string FuenteUrl { get; set; } = string.Empty;
    public List<RutaAdministracionDto> Rutas { get; set; } = [];
}

public class RutaAdministracionDto
{
    public string Id { get; set; } = string.Empty;
    public string BloqueId { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<ParadaRutaDto> Paradas { get; set; } = [];
    public List<SalidaAdministracionDto> Salidas { get; set; } = [];
}

public class SalidaAdministracionDto
{
    public string Id { get; set; } = string.Empty;
    public string RutaId { get; set; } = string.Empty;
    public DiasOperacion Dias { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
    public List<HoraEscrituraDto> Horas { get; set; } = [];
    // Servicios ya generados: editar la salida no los cambia, solo afecta a generaciones futuras.
    public int ServiciosGenerados { get; set; }
}

public class ExcepcionAdministracionDto
{
    public string Id { get; set; } = string.Empty;
    // Nulo: aplica a todos los bloques.
    public string? BloqueId { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public TipoExcepcion Tipo { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class BloqueEscrituraDto
{
    public string? Clave { get; set; }
    public string? Nombre { get; set; }
    public string? Empresa { get; set; }
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public string? FuenteUrl { get; set; }
}

public class RutaEscrituraDto
{
    public string? BloqueId { get; set; }
    public string? Clave { get; set; }
    public string? Familia { get; set; }
    public string? Nombre { get; set; }
    // Paradas en el orden de recorrido; la primera es el orden 1.
    public List<string> ParadaIds { get; set; } = [];
}

public class SalidaEscrituraDto
{
    public string? RutaId { get; set; }
    public DiasOperacion? Dias { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
    // Horas por orden de parada; las paradas sin hora publicada no se envían.
    public List<HoraEscrituraDto> Horas { get; set; } = [];
}

public class HoraEscrituraDto
{
    public int Orden { get; set; }
    public TimeOnly? Hora { get; set; }
}

public class FeriadoEscrituraDto
{
    // En PUT la fecha va en la ruta: no se puede cambiar.
    public DateOnly? Fecha { get; set; }
    public string? Nombre { get; set; }
}

public class ExcepcionEscrituraDto
{
    public string? BloqueId { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public TipoExcepcion? Tipo { get; set; }
    public string? Motivo { get; set; }
}
