using API_Berruti.Models.Catalogo;
using API_Berruti.Services.Catalogo;

namespace API_Berruti.DTOs.Catalogo;

public class CatalogoDto
{
    public List<BloqueCatalogoDto> Bloques { get; set; } = [];
    public List<FeriadoDto> Feriados { get; set; } = [];
    public List<ExcepcionDto> Excepciones { get; set; } = [];
}

public class BloqueCatalogoDto
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public string FuenteUrl { get; set; } = string.Empty;
    public List<RutaCatalogoDto> Rutas { get; set; } = [];
}

public class RutaCatalogoDto
{
    public string Clave { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<ParadaRutaDto> Paradas { get; set; } = [];
    public List<SalidaCatalogoDto> Salidas { get; set; } = [];
}

public class ParadaRutaDto
{
    public int Orden { get; set; }
    public string ParadaId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class SalidaCatalogoDto
{
    public string Id { get; set; } = string.Empty;
    public DiasOperacion Dias { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
    // Solo las paradas con hora publicada; la primera es la salida y las demás son aproximadas.
    public List<HoraParadaDto> Horas { get; set; } = [];
}

public class HoraParadaDto
{
    public int Orden { get; set; }
    public string Parada { get; set; } = string.Empty;
    public TimeOnly Hora { get; set; }
    public bool Aproximada { get; set; }
}

public class FeriadoDto
{
    public DateOnly Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ExcepcionDto
{
    public string? Bloque { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public TipoExcepcion Tipo { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class SalidasDelDiaDto
{
    public DateOnly Fecha { get; set; }
    public string? Feriado { get; set; }
    public List<BloqueDelDiaDto> Bloques { get; set; } = [];
}

public class BloqueDelDiaDto
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public EstadoDia Estado { get; set; }
    public string? Motivo { get; set; }
    public List<SalidaDelDiaDto> Salidas { get; set; } = [];
}

public class SalidaDelDiaDto
{
    public string RutaClave { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public SalidaCatalogoDto Salida { get; set; } = new();
}
