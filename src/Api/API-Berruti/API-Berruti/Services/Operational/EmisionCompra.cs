using System.Security.Cryptography;
using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public static class EmisionCompra
{
    public static PlanEmisionCompra Preparar(
        RegistroPago registro,
        IReadOnlyList<TramoPago> precios,
        IReadOnlyList<TramoIntentoCompra> seleccion,
        string compraId,
        IReadOnlyList<string> compraServicioIds,
        IReadOnlyList<string> ticketIds,
        DateTime confirmadoUtc)
    {
        ArgumentNullException.ThrowIfNull(registro);
        ArgumentNullException.ThrowIfNull(precios);
        ArgumentNullException.ThrowIfNull(seleccion);
        ArgumentNullException.ThrowIfNull(compraServicioIds);
        ArgumentNullException.ThrowIfNull(ticketIds);

        var pago = registro.Pago ?? throw new ArgumentException("El registro debe incluir un pago.", nameof(registro));
        ExigirIdentificador(compraId, nameof(compraId));
        ExigirUtc(confirmadoUtc, nameof(confirmadoUtc));
        ExigirUtc(registro.HoldVenceEn, nameof(registro));

        if (pago.Estado != EstadoPago.Aprobado
            || pago.ImporteEsperado <= 0m
            || pago.ImportePagado != pago.ImporteEsperado
            || !string.Equals(pago.Moneda, "UYU", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(pago.Id)
            || string.IsNullOrWhiteSpace(pago.IntentoId)
            || string.IsNullOrWhiteSpace(pago.OrdenProveedorId)
            || confirmadoUtc >= registro.HoldVenceEn)
        {
            throw new InvalidOperationException("El pago no cumple las condiciones para emitir la compra.");
        }

        if (seleccion.Count == 0
            || precios.Count != seleccion.Count
            || compraServicioIds.Count != seleccion.Count
            || ticketIds.Count != seleccion.Count)
        {
            throw new ArgumentException("Los precios, tramos e identificadores deben tener la misma cantidad no vacía.");
        }

        ExigirIdsUnicos(compraServicioIds, nameof(compraServicioIds));
        ExigirIdsUnicos(ticketIds, nameof(ticketIds));

        var total = 0m;
        for (var index = 0; index < seleccion.Count; index++)
        {
            var leg = seleccion[index];
            var price = precios[index];
            ExigirIdentificador(compraServicioIds[index], nameof(compraServicioIds));
            ExigirIdentificador(ticketIds[index], nameof(ticketIds));
            if (leg is null
                || price is null
                || leg.Orden != index + 1
                || price.Orden != index + 1
                || !string.Equals(leg.IntentoId, pago.IntentoId, StringComparison.Ordinal)
                || !string.Equals(price.IntentoId, pago.IntentoId, StringComparison.Ordinal)
                || !string.Equals(price.PagoId, pago.Id, StringComparison.Ordinal)
                || price.Importe < 0m
                || string.IsNullOrWhiteSpace(leg.ServicioId)
                || string.IsNullOrWhiteSpace(leg.TramoId)
                || string.IsNullOrWhiteSpace(leg.OrigenId)
                || string.IsNullOrWhiteSpace(leg.OrigenNombre)
                || string.IsNullOrWhiteSpace(leg.DestinoId)
                || string.IsNullOrWhiteSpace(leg.DestinoNombre)
                || leg.Llegada <= leg.Salida
                || (leg.PoliticaAsiento == PoliticaAsiento.SinAsiento && leg.NumeroAsiento is not null)
                || (leg.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica
                    && (!leg.NumeroAsiento.HasValue || leg.NumeroAsiento.Value <= 0 || string.IsNullOrWhiteSpace(leg.SalidaId)))
                || !Enum.IsDefined(leg.PoliticaAsiento))
            {
                throw new ArgumentException("Cada tramo debe coincidir con el pago y contener datos de selección válidos.");
            }

            total = checked(total + price.Importe);
        }

        if (total != pago.ImporteEsperado)
            throw new InvalidOperationException("El total de los tramos congelados no coincide con el pago aprobado.");

        var compra = new Compra
        {
            Id = compraId,
            IntentoId = pago.IntentoId,
            PagoId = pago.Id,
            ImporteTotal = total,
            ConfirmadoUtc = confirmadoUtc
        };
        var servicios = new List<CompraServicio>(seleccion.Count);
        var tickets = new List<Ticket>(seleccion.Count);
        for (var index = 0; index < seleccion.Count; index++)
        {
            var price = precios[index];
            servicios.Add(new CompraServicio
            {
                Id = compraServicioIds[index],
                CompraId = compra.Id,
                IntentoId = compra.IntentoId,
                PagoId = pago.Id,
                Orden = index + 1,
                Importe = price.Importe
            });
            tickets.Add(new Ticket
            {
                Id = ticketIds[index],
                CompraServicioId = compraServicioIds[index],
                EmitidoUtc = confirmadoUtc,
                QrOpaco = Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            });
        }

        return new PlanEmisionCompra(compra, servicios.AsReadOnly(), tickets.AsReadOnly());
    }

    private static void ExigirIdsUnicos(IReadOnlyList<string> ids, string nombre)
    {
        if (ids.Any(string.IsNullOrWhiteSpace)
            || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
        {
            throw new ArgumentException("Los identificadores deben ser no vacíos y únicos.", nombre);
        }
    }

    private static void ExigirIdentificador(string? value, string nombre)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El identificador es obligatorio.", nombre);
    }

    private static void ExigirUtc(DateTime value, string nombre)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe estar expresada en UTC.", nombre);
    }
}
