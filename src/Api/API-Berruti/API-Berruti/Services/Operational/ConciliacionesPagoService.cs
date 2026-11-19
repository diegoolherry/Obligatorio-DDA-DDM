using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public sealed class ConciliacionesPagoService : IConciliacionesPagoService
{
    private readonly IConfirmacionesCompraRepository _confirmaciones;
    private readonly IMercadoPagoCheckoutClient _checkout;
    private readonly TimeProvider _timeProvider;

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
                    pago.Id,
                    pago.CreadoUtc,
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
                    pago.Id,
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
                resultado.Registro.Pago.Id,
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
