using System.Security.Cryptography;
using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public static class ReglasInicioPago
{
    private const decimal ImporteMaximo = 99_999_999.99m;

    public static bool EsHuellaValida(string? huella) =>
        huella is { Length: 64 } && huella.All(Uri.IsHexDigit);

    public static bool CoincideHuella(string enviada, string actual)
    {
        if (!EsHuellaValida(enviada) || !EsHuellaValida(actual))
            return false;
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(enviada),
            Convert.FromHexString(actual));
    }

    public static bool HoldVigente(DateTime holdVenceUtc, DateTime ahoraUtc)
    {
        ExigirUtc(holdVenceUtc, nameof(holdVenceUtc));
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        return ahoraUtc < holdVenceUtc;
    }

    public static void ValidarReserva(
        IniciarIntentoCompraResponseDto intento,
        CotizarCompraResponseDto cotizacion,
        string huellaAceptada,
        DateTime ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(intento);
        ArgumentNullException.ThrowIfNull(cotizacion);
        if (!EsHuellaValida(huellaAceptada))
            throw new ArgumentException("La huella de cotización aceptada no es válida.", nameof(huellaAceptada));
        if (!EsHuellaValida(cotizacion.HuellaCotizacion))
            throw new InvalidOperationException("La cotización actual no contiene una huella válida.");
        if (!CoincideHuella(huellaAceptada, cotizacion.HuellaCotizacion))
            throw new CotizacionModificadaException(cotizacion);

        ExigirUtc(intento.HoldVenceEn, nameof(intento.HoldVenceEn));
        ExigirUtc(cotizacion.HoldVenceEn, nameof(cotizacion.HoldVenceEn));
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        if (!HoldVigente(intento.HoldVenceEn, ahoraUtc) ||
            !string.Equals(intento.IntentoId, cotizacion.IntentoId, StringComparison.Ordinal) ||
            intento.HoldVenceEn != cotizacion.HoldVenceEn ||
            !SeleccionCoincide(intento.Itinerario, cotizacion.CotizacionOriginal) ||
            !PreciosCoinciden(intento.Itinerario, cotizacion.CotizacionOriginal) ||
            !SeleccionCoincide(intento.Itinerario, cotizacion.CotizacionActual))
            throw new CotizacionNoDisponibleException("La selección o el hold original ya no están disponibles.");

        ValidarImportes(cotizacion.CotizacionActual);
    }

    public static void ValidarImportes(ItinerarioDto itinerario)
    {
        ArgumentNullException.ThrowIfNull(itinerario);
        if (itinerario.Tramos.Count == 0 ||
            itinerario.Tramos.Any(tramo => !ImporteValido(tramo.Importe)) ||
            !ImporteValido(itinerario.PrecioTotal) ||
            itinerario.PrecioTotal <= 0 ||
            itinerario.Tramos.Sum(tramo => tramo.Importe) != itinerario.PrecioTotal ||
            itinerario.Tramos.Select(tramo => tramo.Orden).Distinct().Count() != itinerario.Tramos.Count ||
            itinerario.Tramos.Any(tramo => string.IsNullOrWhiteSpace(tramo.ServicioId)))
            throw new InvalidOperationException("La cotización contiene importes o tramos inconsistentes.");
    }

    private static bool ImporteValido(decimal importe) =>
        importe >= 0 && importe <= ImporteMaximo && decimal.Round(importe, 2, MidpointRounding.ToEven) == importe;

    private static bool SeleccionCoincide(ItinerarioDto original, ItinerarioDto candidata) =>
        original.Tramos.Count > 0 &&
        CotizacionesCompraService.ConservaSeleccion(original, candidata);

    private static bool PreciosCoinciden(ItinerarioDto original, ItinerarioDto candidata) =>
        original.PrecioTotal == candidata.PrecioTotal &&
        original.Tramos.OrderBy(tramo => tramo.Orden).Select(tramo => tramo.Importe)
            .SequenceEqual(candidata.Tramos.OrderBy(tramo => tramo.Orden).Select(tramo => tramo.Importe));

    private static void ExigirUtc(DateTime valor, string nombre)
    {
        if (valor.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe estar en UTC.", nombre);
    }
}
