using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public sealed class PagosService : IPagosService
{
    private readonly IPagosRepository _pagos;
    private readonly ICotizacionesCompraService _cotizaciones;
    private readonly IMercadoPagoCheckoutClient _checkout;
    private readonly TimeProvider _timeProvider;

    public PagosService(
        IPagosRepository pagos,
        ICotizacionesCompraService cotizaciones,
        IMercadoPagoCheckoutClient checkout,
        TimeProvider timeProvider)
    {
        _pagos = pagos;
        _cotizaciones = cotizaciones;
        _checkout = checkout;
        _timeProvider = timeProvider;
    }
    public async Task<PagoResponseDto> IniciarAsync(
        string identificadorPublico,
        string intentoId,
        IniciarPagoRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (string.IsNullOrWhiteSpace(intentoId))
            throw new ArgumentException("El identificador del intento es obligatorio.", nameof(intentoId));
        ArgumentNullException.ThrowIfNull(solicitud);
        if (!ReglasInicioPago.EsHuellaValida(solicitud.HuellaCotizacionAceptada))
            throw new ArgumentException("La huella de cotización aceptada no es válida.", nameof(solicitud));

        cancellationToken.ThrowIfCancellationRequested();
        var actual = await _pagos.ConsultarActualAsync(identificadorPublico, intentoId, cancellationToken);
        if (actual is not null && actual.Pago.Estado != EstadoPago.Rechazado)
            return CrearRespuesta(actual);

        var cotizacion = await _cotizaciones.CotizarAsync(identificadorPublico, intentoId, cancellationToken);
        if (!string.Equals(cotizacion.IntentoId, intentoId, StringComparison.Ordinal) ||
            !ReglasInicioPago.EsHuellaValida(cotizacion.HuellaCotizacion))
            throw new InvalidOperationException("La cotización actual no corresponde al intento solicitado.");
        if (!ReglasInicioPago.CoincideHuella(solicitud.HuellaCotizacionAceptada, cotizacion.HuellaCotizacion))
            throw new CotizacionModificadaException(cotizacion);

        var reserva = await _pagos.ReservarAsync(
            identificadorPublico,
            cotizacion,
            solicitud.HuellaCotizacionAceptada,
            cancellationToken);
        if (!reserva.EsNuevo)
            return CrearRespuesta(reserva);

        OrdenCheckoutMercadoPago orden;
        try
        {
            orden = await _checkout.CrearOrdenAsync(
                reserva.Pago.ClaveCreacion,
                reserva.Pago.ReferenciaExterna,
                reserva.Pago.ImporteEsperado,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is FalloPreEnvioProveedorException or ArgumentException)
        {
            // Fallo definitivo antes de enviar la orden: no hay nada que conciliar, así que se libera el intento.
            await _pagos.RechazarSinOrdenAsync(identificadorPublico, reserva.Pago.Id, CancellationToken.None);
            throw new FalloPreEnvioProveedorException(ex);
        }
        catch (Exception ex) when (EsFalloAmbiguoDelProveedor(ex))
        {
            return CrearRespuesta(reserva);
        }

        if (!OrdenCoincideConPago(orden, reserva.Pago))
            return CrearRespuesta(reserva);

        var registrado = await _pagos.RegistrarOrdenAsync(
            identificadorPublico,
            reserva.Pago.Id,
            orden,
            cancellationToken);
        return CrearRespuesta(registrado);
    }

    public async Task<PagoResponseDto?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (string.IsNullOrWhiteSpace(intentoId))
            throw new ArgumentException("El identificador del intento es obligatorio.", nameof(intentoId));

        cancellationToken.ThrowIfCancellationRequested();
        var actual = await _pagos.ConsultarActualAsync(identificadorPublico, intentoId, cancellationToken);
        return actual is null ? null : CrearRespuesta(actual);
    }

    private PagoResponseDto CrearRespuesta(RegistroPago registro)
    {
        var pago = registro.Pago;
        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var urlVigente = pago.Estado == EstadoPago.Pendiente &&
            ReglasInicioPago.HoldVigente(registro.HoldVenceEn, ahoraUtc);
        return new PagoResponseDto
        {
            PagoId = pago.Id,
            IntentoId = pago.IntentoId,
            Estado = pago.Estado,
            Importe = pago.ImporteEsperado,
            Moneda = pago.Moneda,
            CheckoutUrl = urlVigente ? pago.CheckoutUrl : null,
            HoldVenceEn = registro.HoldVenceEn
        };
    }

    private static bool OrdenCoincideConPago(OrdenCheckoutMercadoPago? orden, Pago pago) =>
        orden is not null &&
        !string.IsNullOrWhiteSpace(orden.Id) &&
        orden.Id.Length <= 256 &&
        string.Equals(orden.ReferenciaExterna, pago.ReferenciaExterna, StringComparison.Ordinal) &&
        orden.Importe == pago.ImporteEsperado &&
        string.Equals(orden.Moneda, pago.Moneda, StringComparison.Ordinal) &&
        orden.ImportePagado >= 0 && orden.ImportePagado <= pago.ImporteEsperado &&
        Enum.IsDefined(orden.Estado) &&
        EsCheckoutUrlSeguro(orden.CheckoutUrl);

    private static bool EsCheckoutUrlSeguro(string? valor)
    {
        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri))
            return false;
        return uri.Scheme == Uri.UriSchemeHttps &&
            string.Equals(uri.Host, "www.mercadopago.com.uy", StringComparison.OrdinalIgnoreCase) &&
            uri.IsDefaultPort &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            string.IsNullOrEmpty(uri.Fragment);
    }

    private static bool EsFalloAmbiguoDelProveedor(Exception exception) =>
        exception is HttpRequestException or InvalidOperationException or TimeoutException or OperationCanceledException;
}
