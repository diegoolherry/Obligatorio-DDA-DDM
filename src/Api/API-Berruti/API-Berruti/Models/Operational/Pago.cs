namespace API_Berruti.Models.Operational;

public class Pago
{
    public string Id { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public decimal ImporteEsperado { get; set; }
    public decimal ImportePagado { get; set; }
    public string Moneda { get; set; } = "UYU";
    public EstadoPago Estado { get; set; } = EstadoPago.Desconocido;
    public Guid ClaveCreacion { get; set; }

    /// <summary>
    /// Valor enviado a Mercado Pago como external_reference: único global (GUID ya persistido),
    /// no el contador local PG-n, para que dos bases con la misma cuenta TEST no colisionen.
    /// </summary>
    public string ReferenciaExterna => ClaveCreacion.ToString("D");
    public Guid? ClaveDevolucion { get; set; }
    public string? OrdenProveedorId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime ActualizadoUtc { get; set; }
    public DateTime? UltimaVerificacionUtc { get; set; }
    public DateTime? DevolucionSolicitadaUtc { get; set; }
    public DateTime? DevolucionConfirmadaUtc { get; set; }
}
