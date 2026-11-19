using System.Linq.Expressions;
using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public static class OcupacionAsientosCompra
{
    public static IQueryable<TramoIntentoCompra> TicketsConfirmados(
        IQueryable<Ticket> tickets,
        IQueryable<CompraServicio> compraServicios,
        IQueryable<TramoIntentoCompra> seleccion)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(compraServicios);
        ArgumentNullException.ThrowIfNull(seleccion);

        return tickets
            .Join(compraServicios,
                ticket => ticket.CompraServicioId,
                servicio => servicio.Id,
                (_, servicio) => servicio)
            .Join(seleccion,
                servicio => new { servicio.IntentoId, servicio.Orden },
                tramo => new { tramo.IntentoId, tramo.Orden },
                (_, tramo) => tramo);
    }

    public static Expression<Func<TramoIntentoCompra, bool>> Superpuestos(TramoIntentoCompra candidato)
        => CrearPredicado(candidato, compararAsiento: true);

    public static Expression<Func<TramoIntentoCompra, bool>> IntervalosSuperpuestos(TramoIntentoCompra candidato)
        => CrearPredicado(candidato, compararAsiento: false);

    private static Expression<Func<TramoIntentoCompra, bool>> CrearPredicado(
        TramoIntentoCompra candidato,
        bool compararAsiento)
    {
        ArgumentNullException.ThrowIfNull(candidato);
        if (candidato.PoliticaAsiento != PoliticaAsiento.AsignacionAutomatica
            || string.IsNullOrWhiteSpace(candidato.SalidaId)
            || !candidato.OrdenEmbarque.HasValue
            || !candidato.OrdenDesembarque.HasValue
            || candidato.OrdenEmbarque.Value >= candidato.OrdenDesembarque.Value
            || (compararAsiento && !candidato.NumeroAsiento.HasValue))
        {
            return _ => false;
        }

        var intentoId = candidato.IntentoId;
        var salidaId = candidato.SalidaId;
        var numeroAsiento = candidato.NumeroAsiento.GetValueOrDefault();
        var embarque = candidato.OrdenEmbarque.Value;
        var desembarque = candidato.OrdenDesembarque.Value;

        if (!compararAsiento)
        {
            return existente => existente.IntentoId != intentoId
                && existente.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica
                && existente.SalidaId == salidaId
                && existente.OrdenEmbarque.HasValue
                && existente.OrdenDesembarque.HasValue
                && embarque < existente.OrdenDesembarque.Value
                && existente.OrdenEmbarque.Value < desembarque;
        }

        return existente => existente.IntentoId != intentoId
            && existente.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica
            && existente.SalidaId == salidaId
            && existente.NumeroAsiento == numeroAsiento
            && existente.OrdenEmbarque.HasValue
            && existente.OrdenDesembarque.HasValue
            && embarque < existente.OrdenDesembarque.Value
            && existente.OrdenEmbarque.Value < desembarque;
    }
}
