using System.Data;
using API_Berruti.Data;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Operational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API_Berruti.Repositories.Operational;

public sealed class ConfirmacionesCompraRepository(
    BerrutiDbContext contexto,
    IIntentosCompraRepository intentos,
    IContadoresRepository contadores,
    TimeProvider reloj) : IConfirmacionesCompraRepository
{
    public async Task<ResultadoConciliacionPago?> ConsultarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var snapshotIntento = await intentos.ConsultarAsync(identificadorPublico, intentoId, cancellationToken);
        var pago = await PagoActualAsync(intentoId, cancellationToken);
        if (pago is null)
            return null;
        var compra = await CompraActualAsync(pago.IntentoId, cancellationToken);
        return new ResultadoConciliacionPago(
            new RegistroPago(pago, snapshotIntento.HoldVenceEn, false), compra, pago.Estado == EstadoPago.DevolucionPendiente);
    }

    public async Task<ResultadoConciliacionPago> ConciliarAsync(
        string identificadorPublico,
        string pagoId,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orden);
        if (contexto.ChangeTracker.Entries<Pago>().Any(e => e.Entity.Id == pagoId))
            throw new InvalidOperationException("El pago ya estÃ¡ rastreado fuera de esta reconciliaciÃ³n.");
        var inicial = await contexto.Set<Pago>().AsNoTracking().SingleOrDefaultAsync(p => p.Id == pagoId, cancellationToken)
            ?? throw new KeyNotFoundException("El pago no existe.");
        await intentos.ConsultarAsync(identificadorPublico, inicial.IntentoId, cancellationToken);

        await using var tx = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var pagoRastreado = new List<Pago>();
        var comprasNuevas = new List<Compra>();
        var serviciosNuevos = new List<CompraServicio>();
        var ticketsNuevos = new List<Ticket>();
        try
        {
            await BloquearAsync("intento_compra", "intento_id", inicial.IntentoId, tx, cancellationToken);
            var snapshotIntento = await intentos.ConsultarAsync(identificadorPublico, inicial.IntentoId, cancellationToken);
            var intento = await contexto.Set<IntentoCompra>().AsNoTracking()
                .SingleAsync(i => i.Id == inicial.IntentoId, cancellationToken);
            var pago = await contexto.Set<Pago>().AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == pagoId && p.IntentoId == intento.Id, cancellationToken)
                ?? throw new KeyNotFoundException("El pago no existe.");
            var seleccion = await contexto.Set<TramoIntentoCompra>().AsNoTracking()
                .Where(t => t.IntentoId == intento.Id).OrderBy(t => t.Orden).ToListAsync(cancellationToken);
            var salidas = seleccion.Where(t => t.NumeroAsiento.HasValue && t.SalidaId != null)
                .Select(t => t.SalidaId!).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            foreach (var salidaId in salidas)
                await BloquearAsync("salida", "salida_id", salidaId, tx, cancellationToken);

            // All state and ownership is read again after acquiring the serialization locks.
            snapshotIntento = await intentos.ConsultarAsync(identificadorPublico, intento.Id, cancellationToken);
            intento = await contexto.Set<IntentoCompra>().AsNoTracking().SingleAsync(i => i.Id == intento.Id, cancellationToken);
            pago = await contexto.Set<Pago>().AsNoTracking().SingleAsync(p => p.Id == pagoId && p.IntentoId == intento.Id, cancellationToken);
            seleccion = await contexto.Set<TramoIntentoCompra>().AsNoTracking()
                .Where(t => t.IntentoId == intento.Id).OrderBy(t => t.Orden).ToListAsync(cancellationToken);
            var precios = await contexto.Set<TramoPago>().AsNoTracking()
                .Where(t => t.PagoId == pago.Id && t.IntentoId == intento.Id).OrderBy(t => t.Orden).ToListAsync(cancellationToken);

            ValidarOrden(pago, orden);
            if (pago.OrdenProveedorId is not null && !string.Equals(pago.OrdenProveedorId, orden.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("El pago ya estÃ¡ asociado a otra orden del proveedor.");
            var estado = EstadoConciliado(pago.Estado, orden.Estado);
            contexto.Attach(pago);
            pagoRastreado.Add(pago);
            pago.Estado = orden.Estado == EstadoPago.Devuelto ? EstadoPago.Devuelto : estado;
            if (orden.Estado == EstadoPago.Aprobado)
                pago.ImportePagado = Math.Max(pago.ImportePagado, orden.ImportePagado);
            pago.OrdenProveedorId = orden.Id;
            pago.CheckoutUrl = orden.CheckoutUrl ?? pago.CheckoutUrl;
            pago.UltimaVerificacionUtc = TruncarMicrosegundos(reloj.GetUtcNow().UtcDateTime);
            pago.ActualizadoUtc = pago.UltimaVerificacionUtc.Value;
            if (pago.Estado == EstadoPago.Devuelto)
                pago.DevolucionConfirmadaUtc ??= pago.UltimaVerificacionUtc;
            var compraExistente = await CompraActualAsync(intento.Id, cancellationToken);
            if (compraExistente is not null)
            {
                if (compraExistente.PagoId == pago.Id)
                {
                    await contexto.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    return Resultado(pago, snapshotIntento.HoldVenceEn, compraExistente, false);
                }
                if (pago.Estado == EstadoPago.Devuelto)
                {
                    await contexto.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    return Resultado(pago, snapshotIntento.HoldVenceEn, null, false);
                }
                if (pago.Estado == EstadoPago.Aprobado)
                    return await GuardarSeguimientoAsync(pago, snapshotIntento.HoldVenceEn, tx, cancellationToken, pagoRastreado);
                await contexto.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return Resultado(pago, snapshotIntento.HoldVenceEn, null, false);
            }
            var ahoraAntesDeContadores = reloj.GetUtcNow().UtcDateTime;
            var disponible = pago.ImportePagado == pago.ImporteEsperado
                && await VerificarDisponibilidadAsync(intento, seleccion, ahoraAntesDeContadores, cancellationToken);
            var decision = PoliticaConfirmacionCompra.Decidir(
                pago.Estado,
                new DateTimeOffset(snapshotIntento.HoldVenceEn, TimeSpan.Zero),
                new DateTimeOffset(ahoraAntesDeContadores, TimeSpan.Zero),
                disponible);

            if (decision == DecisionConfirmacion.SeguimientoDevolucion)
            {
                await IniciarDevolucionAsync(pago, cancellationToken);
                await contexto.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return Resultado(pago, snapshotIntento.HoldVenceEn, null, true);
            }
            if (decision != DecisionConfirmacion.Confirmar)
            {
                await contexto.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return Resultado(pago, snapshotIntento.HoldVenceEn, null, pago.Estado == EstadoPago.DevolucionPendiente);
            }

            await tx.CreateSavepointAsync("antes_contadores_compra", cancellationToken);
            var compraId = await contadores.IncrementarAsync("CO-");
            var csIds = new List<string>(seleccion.Count);
            var ticketIds = new List<string>(seleccion.Count);
            for (var n = 0; n < seleccion.Count; n++)
            {
                csIds.Add(await contadores.IncrementarAsync("CS-"));
                ticketIds.Add(await contadores.IncrementarAsync("TI-"));
            }
            // Counter locks may have delayed confirmation past the original, immutable hold deadline.
            var ahoraValidacionFinal = reloj.GetUtcNow().UtcDateTime;
            var disponibleFinal = await VerificarDisponibilidadAsync(intento, seleccion, ahoraValidacionFinal, cancellationToken);
            var horaConfirmacionApi = reloj.GetUtcNow().UtcDateTime;
            if (horaConfirmacionApi >= snapshotIntento.HoldVenceEn || !disponibleFinal)
            {
                await tx.RollbackToSavepointAsync("antes_contadores_compra", CancellationToken.None);
                await IniciarDevolucionAsync(pago, cancellationToken);
                await contexto.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return Resultado(pago, snapshotIntento.HoldVenceEn, null, true);
            }

            var confirmadoUtc = TruncarMicrosegundos(horaConfirmacionApi);
            var registro = new RegistroPago(pago, snapshotIntento.HoldVenceEn, false);
            var plan = EmisionCompra.Preparar(registro, precios, seleccion, compraId, csIds, ticketIds, confirmadoUtc);
            contexto.Set<Compra>().Add(plan.Compra);
            contexto.Set<CompraServicio>().AddRange(plan.Servicios);
            contexto.Set<Ticket>().AddRange(plan.Tickets);
            comprasNuevas.Add(plan.Compra);
            serviciosNuevos.AddRange(plan.Servicios);
            ticketsNuevos.AddRange(plan.Tickets);
            pago.UltimaVerificacionUtc = confirmadoUtc;
            pago.ActualizadoUtc = confirmadoUtc;
            await contexto.SaveChangesAsync(cancellationToken);
            var compraConfirmada = await CompraActualAsync(intento.Id, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Resultado(pago, snapshotIntento.HoldVenceEn, compraConfirmada, false);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            foreach (var entity in pagoRastreado.Cast<object>().Concat(comprasNuevas).Concat(serviciosNuevos).Concat(ticketsNuevos))
                contexto.Entry(entity).State = EntityState.Detached;
        }
    }

    private async Task<ResultadoConciliacionPago> GuardarSeguimientoAsync(
        Pago pago, DateTime holdVence, IDbContextTransaction tx, CancellationToken ct, List<Pago> tracked)
    {
        await IniciarDevolucionAsync(pago, ct);
        tracked.Add(pago);
        await contexto.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Resultado(pago, holdVence, null, true);
    }

    private async Task<bool> VerificarDisponibilidadAsync(
        IntentoCompra intento, IReadOnlyList<TramoIntentoCompra> seleccion, DateTime ahoraUtc, CancellationToken ct)
    {
        if (seleccion.Count == 0) return false;
        var servicioIds = seleccion.Select(s => s.ServicioId).ToArray();
        var servicios = await contexto.Servicios.AsNoTracking().Where(s => servicioIds.Contains(s.Id)).ToListAsync(ct);
        if (servicios.Count != seleccion.Count) return false;
        foreach (var tramo in seleccion)
        {
            var servicio = servicios.SingleOrDefault(s => s.Id == tramo.ServicioId);
            if (servicio is null || servicio.TramoId != tramo.TramoId || servicio.Salida != tramo.Salida ||
                servicio.Llegada != tramo.Llegada || servicio.PoliticaAsiento != tramo.PoliticaAsiento ||
                servicio.SalidaId != tramo.SalidaId || servicio.OrdenEmbarque != tramo.OrdenEmbarque ||
                servicio.OrdenDesembarque != tramo.OrdenDesembarque) return false;
            var ruta = await contexto.Tramos.AsNoTracking().SingleOrDefaultAsync(t => t.Id == tramo.TramoId, ct);
            if (ruta is null || ruta.OrigenId != tramo.OrigenId || ruta.DestinoId != tramo.DestinoId) return false;
            if (tramo.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
            {
                if (tramo.SalidaId is null || !tramo.OrdenEmbarque.HasValue || !tramo.OrdenDesembarque.HasValue)
                    return false;
                var origen = await contexto.ParadasSalida.AsNoTracking().SingleOrDefaultAsync(p =>
                    p.SalidaId == tramo.SalidaId && p.ParadaId == tramo.OrigenId && p.Orden == tramo.OrdenEmbarque, ct);
                var destino = await contexto.ParadasSalida.AsNoTracking().SingleOrDefaultAsync(p =>
                    p.SalidaId == tramo.SalidaId && p.ParadaId == tramo.DestinoId && p.Orden == tramo.OrdenDesembarque, ct);
                if (origen is null || destino is null || origen.Orden >= destino.Orden) return false;
            }
            if (tramo.NumeroAsiento is null) continue;
            if (!await contexto.AsientosSalida.AsNoTracking().AnyAsync(a =>
                    a.SalidaId == tramo.SalidaId && a.NumeroAsiento == tramo.NumeroAsiento, ct)) return false;
            var hold = await contexto.HoldsAsiento.AsNoTracking().AnyAsync(h => h.IntentoId == intento.Id && h.Orden == tramo.Orden &&
                h.SalidaId == tramo.SalidaId && h.NumeroAsiento == tramo.NumeroAsiento && h.OrdenEmbarque == tramo.OrdenEmbarque &&
                h.OrdenDesembarque == tramo.OrdenDesembarque, ct);
            if (!hold) return false;
            if (await (from h in contexto.HoldsAsiento.AsNoTracking()
                       join owner in contexto.IntentosCompra.AsNoTracking() on h.IntentoId equals owner.Id
                       where h.IntentoId != intento.Id && owner.HoldVenceUtc > ahoraUtc &&
                             h.SalidaId == tramo.SalidaId && h.NumeroAsiento == tramo.NumeroAsiento &&
                             h.OrdenEmbarque < tramo.OrdenDesembarque && tramo.OrdenEmbarque < h.OrdenDesembarque
                       select h).AnyAsync(ct)) return false;
            var confirmed = OcupacionAsientosCompra.TicketsConfirmados(
                contexto.Set<Ticket>().AsNoTracking(), contexto.Set<CompraServicio>().AsNoTracking(), contexto.TramosIntentoCompra.AsNoTracking());
            if (await confirmed.AnyAsync(OcupacionAsientosCompra.Superpuestos(tramo), ct)) return false;
        }
        return true;
    }

    private async Task<CompraConfirmadaResponseDto?> CompraActualAsync(string intentoId, CancellationToken ct)
    {
        var compra = await contexto.Set<Compra>().AsNoTracking().SingleOrDefaultAsync(c => c.IntentoId == intentoId, ct);
        if (compra is null) return null;
        var tickets = await (from ticket in contexto.Set<Ticket>().AsNoTracking()
                             join servicio in contexto.Set<CompraServicio>().AsNoTracking() on ticket.CompraServicioId equals servicio.Id
                             join tramo in contexto.TramosIntentoCompra.AsNoTracking() on new { servicio.IntentoId, servicio.Orden } equals new { tramo.IntentoId, tramo.Orden }
                             where servicio.CompraId == compra.Id
                             orderby servicio.Orden
                             select new TicketEmitidoDto
                             {
                                 TicketId = ticket.Id, Orden = servicio.Orden, ServicioId = tramo.ServicioId, TramoId = tramo.TramoId,
                                 OrigenNombre = tramo.OrigenNombre, DestinoNombre = tramo.DestinoNombre, Salida = tramo.Salida,
                                 Llegada = tramo.Llegada, Importe = servicio.Importe, PoliticaAsiento = tramo.PoliticaAsiento,
                                 NumeroAsiento = tramo.NumeroAsiento, QrOpaco = ticket.QrOpaco
                             }).ToListAsync(ct);
        return new CompraConfirmadaResponseDto
        {
            CompraId = compra.Id, IntentoId = compra.IntentoId, PagoId = compra.PagoId,
            ConfirmadoUtc = compra.ConfirmadoUtc, ImporteTotal = compra.ImporteTotal, Tickets = tickets
        };
    }

    private async Task<Pago?> PagoActualAsync(string intentoId, CancellationToken ct) =>
        await contexto.Set<Pago>().AsNoTracking().Where(p => p.IntentoId == intentoId)
            .OrderByDescending(p => p.CreadoUtc).ThenByDescending(p => p.Id.Length).ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);

    private async Task BloquearAsync(string tabla, string columna, string id, IDbContextTransaction tx, CancellationToken ct)
    {
        await using var command = contexto.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = $"SELECT {columna} FROM {tabla} WHERE {columna} = @id FOR UPDATE";
        var parameter = command.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = id; command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(ct) is null) throw new KeyNotFoundException("El intento de compra no existe.");
    }

    private static void ValidarOrden(Pago pago, OrdenCheckoutMercadoPago orden)
    {
        if (string.IsNullOrWhiteSpace(orden.Id) || orden.Id.Length > 256 || orden.ReferenciaExterna != pago.Id ||
            orden.Importe != pago.ImporteEsperado || orden.Moneda != "UYU" || pago.Moneda != "UYU" ||
            orden.ImportePagado < 0 || orden.ImportePagado > orden.Importe || !Enum.IsDefined(orden.Estado))
            throw new InvalidOperationException("La respuesta de checkout no coincide con el pago reservado.");
    }

    private static EstadoPago EstadoConciliado(EstadoPago actual, EstadoPago reportado) => actual switch
    {
        EstadoPago.Devuelto => EstadoPago.Devuelto,
        EstadoPago.DevolucionPendiente when reportado != EstadoPago.Devuelto => EstadoPago.DevolucionPendiente,
        EstadoPago.Aprobado when reportado is EstadoPago.Pendiente or EstadoPago.Desconocido or EstadoPago.Rechazado => EstadoPago.Aprobado,
        _ => reportado
    };

    private async Task IniciarDevolucionAsync(Pago pago, CancellationToken ct)
    {
        pago.Estado = EstadoPago.DevolucionPendiente;
        pago.ClaveDevolucion ??= Guid.NewGuid();
        pago.DevolucionSolicitadaUtc ??= TruncarMicrosegundos(reloj.GetUtcNow().UtcDateTime);
        pago.UltimaVerificacionUtc = TruncarMicrosegundos(reloj.GetUtcNow().UtcDateTime);
        pago.ActualizadoUtc = pago.UltimaVerificacionUtc.Value;
        await Task.CompletedTask;
    }

    private static ResultadoConciliacionPago Resultado(Pago pago, DateTime vence, CompraConfirmadaResponseDto? compra, bool refund) =>
        new(new RegistroPago(pago, vence, false), compra, refund);

    private static DateTime TruncarMicrosegundos(DateTime valorUtc) =>
        DateTime.SpecifyKind(new DateTime(valorUtc.Ticks - valorUtc.Ticks % 10, DateTimeKind.Utc), DateTimeKind.Utc);
}
