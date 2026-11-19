using System.Data;
using System.Globalization;
using API_Berruti.Data;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Identifiers;
using API_Berruti.Services.Operational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API_Berruti.Repositories.Operational;

public sealed class PagosRepository : IPagosRepository
{
    private const string PrefijoPago = "PG-";
    private readonly BerrutiDbContext contexto;
    private readonly IIntentosCompraRepository intentos;
    private readonly IContadoresRepository contadores;
    private readonly ICorrelativo correlativo;
    private readonly TimeProvider timeProvider;

    public PagosRepository(
        BerrutiDbContext contexto,
        IIntentosCompraRepository intentos,
        IContadoresRepository contadores,
        ICorrelativo correlativo,
        TimeProvider timeProvider)
    {
        this.contexto = contexto;
        this.intentos = intentos;
        this.contadores = contadores;
        this.correlativo = correlativo;
        this.timeProvider = timeProvider;
    }

    public async Task<RegistroPago?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var intento = await intentos.ConsultarAsync(identificadorPublico, intentoId, cancellationToken);
        var pagos = await contexto.Set<Pago>().AsNoTracking()
            .Where(pago => pago.IntentoId == intentoId)
            .ToListAsync(cancellationToken);
        var actual = SeleccionarMasReciente(pagos);
        return actual is null ? null : new RegistroPago(actual, intento.HoldVenceEn, false);
    }

    public async Task<RegistroPago> ReservarAsync(
        string identificadorPublico,
        CotizarCompraResponseDto cotizacion,
        string huellaAceptada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cotizacion);
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));

        // Establish ownership before taking a lock, then verify it again while the attempt is locked.
        await intentos.ConsultarAsync(identificadorPublico, cotizacion.IntentoId, cancellationToken);
        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var pagosAgregados = new List<Pago>();
        var tramosAgregados = new List<TramoPago>();
        try
        {
            await BloquearIntentoAsync(cotizacion.IntentoId, transaccion, cancellationToken);
            var intento = await intentos.ConsultarAsync(identificadorPublico, cotizacion.IntentoId, cancellationToken);
            var existentes = await contexto.Set<Pago>().AsNoTracking()
                .Where(pago => pago.IntentoId == cotizacion.IntentoId)
                .ToListAsync(cancellationToken);
            var bloqueante = SeleccionarMasReciente(existentes.Where(pago => pago.Estado != EstadoPago.Rechazado));
            if (bloqueante is not null)
            {
                await transaccion.CommitAsync(cancellationToken);
                return new RegistroPago(bloqueante, intento.HoldVenceEn, false);
            }

            var ahoraUtc = TruncarMicrosegundos(timeProvider.GetUtcNow().UtcDateTime);
            ReglasInicioPago.ValidarReserva(intento, cotizacion, huellaAceptada, ahoraUtc);

            var nuevoPagoId = await contadores.IncrementarAsync(PrefijoPago);
            ValidarIdentificadorCorrelativo(nuevoPagoId);
            var pago = new Pago
            {
                Id = nuevoPagoId,
                IntentoId = intento.IntentoId,
                ImporteEsperado = cotizacion.CotizacionActual.PrecioTotal,
                ImportePagado = 0m,
                Moneda = "UYU",
                Estado = EstadoPago.Desconocido,
                ClaveCreacion = Guid.NewGuid(),
                CreadoUtc = ahoraUtc,
                ActualizadoUtc = ahoraUtc
            };
            var tramos = cotizacion.CotizacionActual.Tramos.OrderBy(tramo => tramo.Orden)
                .Select(tramo => new TramoPago
                {
                    PagoId = pago.Id,
                    IntentoId = pago.IntentoId,
                    Orden = tramo.Orden,
                    Importe = tramo.Importe
                }).ToList();

            contexto.Set<Pago>().Add(pago);
            contexto.Set<TramoPago>().AddRange(tramos);
            pagosAgregados.Add(pago);
            tramosAgregados.AddRange(tramos);
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            Desvincular(pagosAgregados, tramosAgregados);
            return new RegistroPago(pago, intento.HoldVenceEn, true);
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                Desvincular(pagosAgregados, tramosAgregados);
            }
            throw;
        }
    }

    public async Task<RegistroPago> RegistrarOrdenAsync(
        string identificadorPublico,
        string pagoId,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orden);
        if (string.IsNullOrWhiteSpace(pagoId))
            throw new ArgumentException("El identificador del pago es obligatorio.", nameof(pagoId));

        var pagoInicial = await contexto.Set<Pago>().AsNoTracking()
            .SingleOrDefaultAsync(pago => pago.Id == pagoId, cancellationToken)
            ?? throw new KeyNotFoundException("El pago no existe.");
        await intentos.ConsultarAsync(identificadorPublico, pagoInicial.IntentoId, cancellationToken);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await BloquearIntentoAsync(pagoInicial.IntentoId, transaccion, cancellationToken);
            var intento = await intentos.ConsultarAsync(identificadorPublico, pagoInicial.IntentoId, cancellationToken);
            var pago = await contexto.Set<Pago>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == pagoId && item.IntentoId == intento.IntentoId, cancellationToken)
                ?? throw new KeyNotFoundException("El pago no existe.");
            ValidarOrden(pago, orden);
            if (pago.OrdenProveedorId is not null && !string.Equals(pago.OrdenProveedorId, orden.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("El pago ya está asociado a otra orden de proveedor.");

            pago.OrdenProveedorId = orden.Id;
            pago.CheckoutUrl = orden.CheckoutUrl;
            pago.ActualizadoUtc = TruncarMicrosegundos(timeProvider.GetUtcNow().UtcDateTime);
            var estadoActualizado = ReglasInicioPago.EstadoTrasCrearOrden(pago.Estado, pago.ImportePagado, orden);
            var cambiaEstado = pago.Estado != estadoActualizado;
            if (cambiaEstado)
                pago.Estado = estadoActualizado;

            var entrada = contexto.Attach(pago);
            entrada.Property(item => item.OrdenProveedorId).IsModified = true;
            entrada.Property(item => item.CheckoutUrl).IsModified = true;
            entrada.Property(item => item.ActualizadoUtc).IsModified = true;
            if (cambiaEstado)
                entrada.Property(item => item.Estado).IsModified = true;
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            contexto.Entry(pago).State = EntityState.Detached;
            return new RegistroPago(pago, intento.HoldVenceEn, false);
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                var tracked = contexto.ChangeTracker.Entries<Pago>().FirstOrDefault(entry => entry.Entity.Id == pagoId);
                if (tracked is not null)
                    tracked.State = EntityState.Detached;
            }
            throw;
        }
    }

    public async Task<RegistroPago> RechazarSinOrdenAsync(
        string identificadorPublico,
        string pagoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pagoId))
            throw new ArgumentException("El identificador del pago es obligatorio.", nameof(pagoId));

        var pagoInicial = await contexto.Set<Pago>().AsNoTracking()
            .SingleOrDefaultAsync(pago => pago.Id == pagoId, cancellationToken)
            ?? throw new KeyNotFoundException("El pago no existe.");
        await intentos.ConsultarAsync(identificadorPublico, pagoInicial.IntentoId, cancellationToken);

        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await BloquearIntentoAsync(pagoInicial.IntentoId, transaccion, cancellationToken);
            var intento = await intentos.ConsultarAsync(identificadorPublico, pagoInicial.IntentoId, cancellationToken);
            var pago = await contexto.Set<Pago>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == pagoId && item.IntentoId == intento.IntentoId, cancellationToken)
                ?? throw new KeyNotFoundException("El pago no existe.");

            // Solo un pago sin rastro en el proveedor puede rechazarse sin conciliar.
            if (pago.Estado != EstadoPago.Desconocido || pago.OrdenProveedorId is not null || pago.ImportePagado != 0m)
            {
                await transaccion.CommitAsync(cancellationToken);
                return new RegistroPago(pago, intento.HoldVenceEn, false);
            }

            pago.Estado = EstadoPago.Rechazado;
            pago.ActualizadoUtc = TruncarMicrosegundos(timeProvider.GetUtcNow().UtcDateTime);
            var entrada = contexto.Attach(pago);
            entrada.Property(item => item.Estado).IsModified = true;
            entrada.Property(item => item.ActualizadoUtc).IsModified = true;
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            contexto.Entry(pago).State = EntityState.Detached;
            return new RegistroPago(pago, intento.HoldVenceEn, false);
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                var tracked = contexto.ChangeTracker.Entries<Pago>().FirstOrDefault(entry => entry.Entity.Id == pagoId);
                if (tracked is not null)
                    tracked.State = EntityState.Detached;
            }
            throw;
        }
    }

    private async Task BloquearIntentoAsync(
        string intentoId,
        IDbContextTransaction transaccion,
        CancellationToken cancellationToken)
    {
        await using var comando = contexto.Database.GetDbConnection().CreateCommand();
        comando.Transaction = transaccion.GetDbTransaction();
        comando.CommandText = "SELECT intento_id FROM intento_compra WHERE intento_id = @intentoId FOR UPDATE";
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@intentoId";
        parametro.Value = intentoId;
        comando.Parameters.Add(parametro);
        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        if (resultado is null || resultado == DBNull.Value)
            throw new KeyNotFoundException("El intento de compra no existe.");
    }

    private static Pago? SeleccionarMasReciente(IEnumerable<Pago> pagos) => pagos
        .OrderByDescending(pago => pago.CreadoUtc)
        .ThenByDescending(pago => pago.Id.Length)
        .ThenByDescending(pago => pago.Id, StringComparer.Ordinal)
        .FirstOrDefault();

    private static void ValidarOrden(Pago pago, OrdenCheckoutMercadoPago orden)
    {
        if (string.IsNullOrWhiteSpace(orden.Id) || orden.Id.Length > 256 ||
            !string.Equals(orden.ReferenciaExterna, pago.ReferenciaExterna, StringComparison.Ordinal) ||
            orden.Importe != pago.ImporteEsperado ||
            !string.Equals(orden.Moneda, pago.Moneda, StringComparison.Ordinal) ||
            orden.ImportePagado < 0 || orden.ImportePagado > pago.ImporteEsperado ||
            !Enum.IsDefined(orden.Estado))
            throw new InvalidOperationException("La respuesta de checkout no coincide con el pago reservado.");
    }

    private void ValidarIdentificadorCorrelativo(string identificador)
    {
        const string prefijo = PrefijoPago;
        if (!identificador.StartsWith(prefijo, StringComparison.Ordinal) ||
            !int.TryParse(identificador.AsSpan(prefijo.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var sufijo) ||
            sufijo < 1 ||
            !string.Equals(correlativo.Siguiente(prefijo, sufijo - 1), identificador, StringComparison.Ordinal))
            throw new InvalidOperationException("El contador devolvió un identificador de pago inválido.");
    }

    private static DateTime TruncarMicrosegundos(DateTime valorUtc) =>
        DateTime.SpecifyKind(new DateTime(valorUtc.Ticks - valorUtc.Ticks % 10, DateTimeKind.Utc), DateTimeKind.Utc);

    private void Desvincular(IEnumerable<Pago> pagos, IEnumerable<TramoPago> tramos)
    {
        foreach (var tramo in tramos)
            contexto.Entry(tramo).State = EntityState.Detached;
        foreach (var pago in pagos)
            contexto.Entry(pago).State = EntityState.Detached;
    }
}
