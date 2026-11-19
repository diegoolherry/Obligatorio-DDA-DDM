using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public enum DecisionConfirmacion
{
    Confirmar,
    Esperar,
    NoEmitir,
    SeguimientoDevolucion
}

public static class PoliticaConfirmacionCompra
{
    public static DecisionConfirmacion Decidir(
        EstadoPago estadoPago,
        DateTimeOffset vencimientoHoldUtc,
        DateTimeOffset confirmadoPorApiUtc,
        bool disponibilidadConfirmada)
    {
        ExigirUtc(vencimientoHoldUtc, nameof(vencimientoHoldUtc));
        ExigirUtc(confirmadoPorApiUtc, nameof(confirmadoPorApiUtc));

        return estadoPago switch
        {
            EstadoPago.Aprobado when confirmadoPorApiUtc < vencimientoHoldUtc && disponibilidadConfirmada
                => DecisionConfirmacion.Confirmar,
            EstadoPago.Aprobado => DecisionConfirmacion.SeguimientoDevolucion,
            EstadoPago.Pendiente or EstadoPago.Desconocido => DecisionConfirmacion.Esperar,
            EstadoPago.DevolucionPendiente => DecisionConfirmacion.SeguimientoDevolucion,
            EstadoPago.Rechazado or EstadoPago.Devuelto => DecisionConfirmacion.NoEmitir,
            _ => DecisionConfirmacion.NoEmitir
        };
    }

    private static void ExigirUtc(DateTimeOffset fecha, string nombre)
    {
        if (fecha.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("La fecha debe estar expresada en UTC.", nombre);
        }
    }
}
