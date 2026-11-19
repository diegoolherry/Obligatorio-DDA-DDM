namespace API_Berruti.Models.Operational;

public class TramoIntentoCompra
{
    public string IntentoId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public string OrigenId { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoId { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public decimal Importe { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
    public int? NumeroAsiento { get; set; }
    public string? SalidaId { get; set; }
    public int? OrdenEmbarque { get; set; }
    public int? OrdenDesembarque { get; set; }
}
