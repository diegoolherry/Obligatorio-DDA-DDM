namespace Web_App.Models.Operational;

// Copia del contrato público de GET /api/itinerarios (ver docs/reference/api.md). Horas locales de Uruguay.
public class ItinerarioDto
{
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public decimal PrecioTotal { get; set; }
    public TransbordoDto? Transbordo { get; set; }
    public List<TramoItinerarioDto> Tramos { get; set; } = [];
}

public class TramoItinerarioDto
{
    public int Orden { get; set; }
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public ParadaDto Origen { get; set; } = new();
    public ParadaDto Destino { get; set; } = new();
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public decimal Importe { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
}

public class TransbordoDto
{
    public ParadaDto Parada { get; set; } = new();
    public int Minutos { get; set; }
}
