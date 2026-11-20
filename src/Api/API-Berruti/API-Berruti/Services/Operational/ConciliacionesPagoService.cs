using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public sealed class ConciliacionesPagoService : IConciliacionesPagoService
{
    private readonly IConfirmacionesCompraRepository _confirmaciones;
    private readonly IMercadoPagoCheckoutClient _checkout;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Tolerancia de desfase entre el reloj local y el de Mercado Pago: la búsqueda de recuperación
    /// arranca este margen antes de la creación local del pago (begin_date).
    /// </summary>
    public static readonly TimeSpan MargenDesfaseReloj = TimeSpan.FromMinutes(5);

    public ConciliacionesPagoService(
        IConfirmacionesCompraRepository confirmaciones,
        IMercadoPagoCheckoutClient checkout,
        TimeProvider timeProvider)
    {
        _confirmaciones = confirmaciones;
        _checkout = checkout;
        _timeProvider = timeProvider;
    }

    public async Task<SincronizarPagoResponseDto> SincronizarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (string.IsNullOrWhiteSpace(intentoId))
            throw new ArgumentException("El identificador del intento es obligatorio.", nameof(intentoId));

        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _confirmaciones.ConsultarAsync(identificadorPublico, intentoId, cancellationToken);
        if (snapshot is null)
            throw new KeyNotFoundException("No se encontró un pago para el intento solicitado.");

        var pago = snapshot.Registro.Pago;
        OrdenCheckoutMercadoPago orden;
        try
        {
            if (string.IsNullOrWhiteSpace(pago.OrdenProveedorId))
            {
                var hastaUtc = _timeProvider.GetUtcNow().UtcDateTime;
                if (pago.CreadoUtc.Kind != DateTimeKind.Utc || hastaUtc.Kind != DateTimeKind.Utc || hastaUtc <= pago.CreadoUtc)
                    return CrearRespuesta(snapshot);
                var recuperada = await _checkout.BuscarOrdenAsync(
                    pago.ReferenciaExterna,
                    pago.CreadoUtc - MargenDesfaseReloj,
                    hastaUtc,
                    pago.ImporteEsperado,
                    cancellationToken);
                if (recuperada is null)
                    return CrearRespuesta(snapshot);
                orden = recuperada;
            }
            else
            {
                orden = await _checkout.ConsultarOrdenAsync(
                    pago.OrdenProveedorId,
                    pago.ReferenciaExterna,
                    pago.ImporteEsperado,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFalloDelProveedor(ex))
        {
            return CrearRespuesta(snapshot);
        }

        orden = await CancelarOrdenVencidaSinPagoAsync(snapshot, orden, cancellationToken);

        var resultado = await _confirmaciones.ConciliarAsync(
            identificadorPublico,
            pago.Id,
            orden,
            cancellationToken);
        if (!resultado.RequiereDevolucion)
            return CrearRespuesta(resultado);

        var claveDevolucion = resultado.Registro.Pago.ClaveDevolucion;
        var ordenId = resultado.Registro.Pago.OrdenProveedorId;
        if (!claveDevolucion.HasValue || string.IsNullOrWhiteSpace(ordenId))
            return CrearRespuesta(resultado);

        try
        {
            await _checkout.SolicitarDevolucionCompletaAsync(ordenId, claveDevolucion.Value, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFalloDelProveedor(ex))
        {
            return CrearRespuesta(resultado);
        }

        try
        {
            var verificacion = await _checkout.ConsultarOrdenAsync(
                ordenId,
                resultado.Registro.Pago.ReferenciaExterna,
                resultado.Registro.Pago.ImporteEsperado,
                cancellationToken);
            resultado = await _confirmaciones.ConciliarAsync(
                identificadorPublico,
                resultado.Registro.Pago.Id,
                verificacion,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFalloDelProveedor(ex))
        {
            return CrearRespuesta(resultado);
        }

        return CrearRespuesta(resultado);
    }

    /// <summary>
    /// Con el hold vencido y la orden todavía sin pagar, la cancela en el proveedor para que el pago deje de
    /// bloquear nuevos intentos. Devuelve la orden verificada como cancelada; ante cualquier fallo o respuesta
    /// que no verifique devuelve la orden leída, así que el resto de la conciliación no cambia.
    /// </summary>
    private async Task<OrdenCheckoutMercadoPago> CancelarOrdenVencidaSinPagoAsync(
        ResultadoConciliacionPago snapshot,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken)
    {
        var pago = snapshot.Registro.Pago;
        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (snapshot.Compra is not null
            || pago.Estado is not (EstadoPago.Pendiente or EstadoPago.Desconocido)
            || orden.Estado != EstadoPago.Pendiente
            || orden.ImportePagado != 0m
            || pago.ClaveCreacion == Guid.Empty
            || ReglasInicioPago.HoldVigente(snapshot.Registro.HoldVenceEn, ahoraUtc))
            return orden;

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var cancelada = await _checkout.CancelarOrdenAsync(
                orden.Id,
                ClaveCancelacion(pago.ClaveCreacion),
                pago.ReferenciaExterna,
                pago.ImporteEsperado,
                cancellationToken);
            return EsCancelacionVerificada(cancelada, orden, pago) ? cancelada : orden;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (EsFalloDelProveedor(ex))
        {
            return orden;
        }
    }

    private static bool EsCancelacionVerificada(OrdenCheckoutMercadoPago? cancelada, OrdenCheckoutMercadoPago leida, Pago pago) =>
        cancelada is not null
        && string.Equals(cancelada.Id, leida.Id, StringComparison.Ordinal)
        && string.Equals(cancelada.ReferenciaExterna, pago.ReferenciaExterna, StringComparison.Ordinal)
        && cancelada.Importe == pago.ImporteEsperado
        && cancelada.ImportePagado == 0m
        && string.Equals(cancelada.Moneda, pago.Moneda, StringComparison.Ordinal)
        && cancelada.Estado == EstadoPago.Rechazado;

    /// <summary>
    /// Clave de idempotencia estable de la cancelación de una orden: UUID v5 (RFC 4122, SHA-1) con la
    /// <c>ClaveCreacion</c> persistida del pago como espacio de nombres y el nombre fijo "cancel".
    /// Se deriva de datos ya persistidos, así que no requiere migración y los reintentos del mismo pago
    /// reutilizan la misma clave; es distinta de la clave de creación y de la de devolución.
    /// </summary>
    public static Guid ClaveCancelacion(Guid claveCreacion)
    {
        Span<byte> entrada = stackalloc byte[16 + 6];
        claveCreacion.TryWriteBytes(entrada[..16], bigEndian: true, out _);
        "cancel"u8.CopyTo(entrada[16..]);
        Span<byte> hash = stackalloc byte[20];
        System.Security.Cryptography.SHA1.HashData(entrada, hash);
        Span<byte> bytes = hash[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    private SincronizarPagoResponseDto CrearRespuesta(ResultadoConciliacionPago resultado)
    {
        var registro = resultado.Registro;
        var pago = registro.Pago;
        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var checkoutVigente = pago.Estado == EstadoPago.Pendiente
            && ReglasInicioPago.HoldVigente(registro.HoldVenceEn, ahoraUtc);

        return new SincronizarPagoResponseDto
        {
            Pago = new PagoResponseDto
            {
                PagoId = pago.Id,
                IntentoId = pago.IntentoId,
                Estado = pago.Estado,
                Importe = pago.ImporteEsperado,
                Moneda = pago.Moneda,
                CheckoutUrl = checkoutVigente ? pago.CheckoutUrl : null,
                HoldVenceEn = registro.HoldVenceEn
            },
            Compra = resultado.Compra
        };
    }

    private static bool EsFalloDelProveedor(Exception exception) =>
        exception is HttpRequestException or InvalidOperationException or TimeoutException or OperationCanceledException;
}
