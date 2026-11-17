using Web_App.Models.Operational;

namespace Web_App.Models.Purchase;

public enum EstadoPago { Pendiente, Desconocido, Aprobado, Rechazado, DevolucionPendiente, Devuelto }

public sealed class SeleccionTramoCompraDto
{
    public int Orden { get; set; }
    public string ServicioId { get; set; } = string.Empty;
}

public sealed class IniciarIntentoCompraRequestDto
{
    public List<SeleccionTramoCompraDto> Tramos { get; set; } = [];
}

public sealed class IniciarIntentoCompraResponseDto
{
    public string IntentoId { get; set; } = string.Empty;
    public ItinerarioDto Itinerario { get; set; } = new();
    public DateTime HoldVenceEn { get; set; }
}

public sealed class CotizarCompraResponseDto
{
    public string IntentoId { get; set; } = string.Empty;
    public ItinerarioDto CotizacionOriginal { get; set; } = new();
    public ItinerarioDto CotizacionActual { get; set; } = new();
    public bool CambioPrecio { get; set; }
    public string HuellaCotizacion { get; set; } = string.Empty;
    public DateTime HoldVenceEn { get; set; }
}

public sealed class IniciarPagoRequestDto
{
    public string HuellaCotizacionAceptada { get; set; } = string.Empty;
}

public sealed class PagoResponseDto
{
    public string PagoId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public EstadoPago Estado { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = "UYU";
    public string? CheckoutUrl { get; set; }
    public DateTime HoldVenceEn { get; set; }
}

public sealed class TicketEmitidoDto
{
    public string TicketId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
    public decimal Importe { get; set; }
    public PoliticaAsiento PoliticaAsiento { get; set; }
    public int? NumeroAsiento { get; set; }
    public string QrOpaco { get; set; } = string.Empty;
}

public sealed class CompraConfirmadaResponseDto
{
    public string CompraId { get; set; } = string.Empty;
    public string IntentoId { get; set; } = string.Empty;
    public string PagoId { get; set; } = string.Empty;
    public DateTime ConfirmadoUtc { get; set; }
    public decimal ImporteTotal { get; set; }
    public List<TicketEmitidoDto> Tickets { get; set; } = [];
}

public sealed class SincronizarPagoResponseDto
{
    public PagoResponseDto Pago { get; set; } = new();
    public CompraConfirmadaResponseDto? Compra { get; set; }
}

public enum TipoResultadoCompra { Exito, CotizacionModificada, Ambiguo, Error }
public sealed record ResultadoCompra<T>(TipoResultadoCompra Tipo, T? Datos, int? EstadoHttp, string Mensaje, CotizarCompraResponseDto? Cotizacion = null);
