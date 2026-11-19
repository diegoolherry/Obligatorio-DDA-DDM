namespace API_Berruti.Models.Operational;

public class HoldAsiento
{
    public string IntentoId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string SalidaId { get; set; } = string.Empty;
    public int NumeroAsiento { get; set; }
    public int OrdenEmbarque { get; set; }
    public int OrdenDesembarque { get; set; }
}
