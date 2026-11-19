using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public sealed class CotizacionesCompraService(
    IIntentosCompraService intentos,
    IItinerariosService itinerarios,
    TimeProvider timeProvider) : ICotizacionesCompraService
{
    private const string Moneda = "UYU";

    public async Task<CotizarCompraResponseDto> CotizarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (string.IsNullOrWhiteSpace(intentoId))
            throw new ArgumentException("El identificador del intento es obligatorio.", nameof(intentoId));

        cancellationToken.ThrowIfCancellationRequested();
        var intento = await intentos.ConsultarAsync(identificadorPublico, intentoId, cancellationToken);
        if (intento.HoldVenceEn.Kind != DateTimeKind.Utc)
            throw new InvalidOperationException("El vencimiento persistido del hold debe estar en UTC.");
        var holdVenceUtc = intento.HoldVenceEn;
        AsegurarVigente(holdVenceUtc);

        var original = intento.Itinerario;
        ValidarCotizacion(original);
        var secuenciaOriginal = original.Tramos.OrderBy(tramo => tramo.Orden).ToArray();
        var fechaLocal = DateOnly.FromDateTime(secuenciaOriginal[0].Salida);
        var ofertas = await itinerarios.BuscarAsync(
            secuenciaOriginal[0].Origen.Id,
            secuenciaOriginal[^1].Destino.Id,
            fechaLocal);
        cancellationToken.ThrowIfCancellationRequested();
        AsegurarVigente(holdVenceUtc);

        var idsOriginales = secuenciaOriginal.Select(tramo => tramo.ServicioId).ToArray();
        var actual = ofertas.FirstOrDefault(oferta =>
            oferta.Tramos.OrderBy(tramo => tramo.Orden)
                .Select(tramo => tramo.ServicioId)
                .SequenceEqual(idsOriginales, StringComparer.Ordinal));
        if (actual is null || !ConservaSeleccion(original, actual))
            throw new CotizacionNoDisponibleException("La selección original ya no está disponible.");
        ValidarCotizacion(actual);

        var cambioPrecio = original.PrecioTotal != actual.PrecioTotal ||
            !secuenciaOriginal.Select(tramo => tramo.Importe)
                .SequenceEqual(actual.Tramos.OrderBy(tramo => tramo.Orden).Select(tramo => tramo.Importe));
        return new CotizarCompraResponseDto
        {
            IntentoId = intento.IntentoId,
            CotizacionOriginal = original,
            CotizacionActual = actual,
            CambioPrecio = cambioPrecio,
            HuellaCotizacion = CalcularHuella(intento.IntentoId, holdVenceUtc, actual),
            HoldVenceEn = holdVenceUtc
        };
    }

    private void AsegurarVigente(DateTime holdVenceUtc)
    {
        if (timeProvider.GetUtcNow().UtcDateTime >= holdVenceUtc)
            throw new CotizacionNoDisponibleException("El hold original venció.");
    }

    internal static bool ConservaSeleccion(ItinerarioDto original, ItinerarioDto actual)
    {
        var originales = original.Tramos.OrderBy(tramo => tramo.Orden).ToArray();
        var actuales = actual.Tramos.OrderBy(tramo => tramo.Orden).ToArray();
        if (originales.Length != actuales.Length || original.Salida != actual.Salida || original.Llegada != actual.Llegada)
            return false;
        return originales.Zip(actuales).All(par =>
            par.First.Orden == par.Second.Orden &&
            string.Equals(par.First.ServicioId, par.Second.ServicioId, StringComparison.Ordinal) &&
            string.Equals(par.First.TramoId, par.Second.TramoId, StringComparison.Ordinal) &&
            string.Equals(par.First.Origen.Id, par.Second.Origen.Id, StringComparison.Ordinal) &&
            string.Equals(par.First.Destino.Id, par.Second.Destino.Id, StringComparison.Ordinal) &&
            par.First.Salida == par.Second.Salida && par.First.Llegada == par.Second.Llegada &&
            par.First.PoliticaAsiento == par.Second.PoliticaAsiento);
    }

    private static void ValidarCotizacion(ItinerarioDto cotizacion)
    {
        if (cotizacion.Tramos.Count == 0 || cotizacion.Tramos.Any(tramo =>
                string.IsNullOrWhiteSpace(tramo.ServicioId) || string.IsNullOrWhiteSpace(tramo.TramoId) ||
                !TienePrecisionMonetariaValida(tramo.Importe)) ||
            cotizacion.Tramos.Select(tramo => tramo.Orden).Distinct().Count() != cotizacion.Tramos.Count ||
            !TienePrecisionMonetariaValida(cotizacion.PrecioTotal) ||
            cotizacion.Tramos.Sum(tramo => tramo.Importe) != cotizacion.PrecioTotal)
            throw new InvalidOperationException("La cotización almacenada u ofrecida es inconsistente.");
    }

    private static bool TienePrecisionMonetariaValida(decimal importe) =>
        importe >= 0 && decimal.Round(importe, 2, MidpointRounding.ToEven) == importe;

    private static string CalcularHuella(string intentoId, DateTime holdVenceUtc, ItinerarioDto actual)
    {
        var contenido = new
        {
            IntentoId = intentoId,
            HoldVenceUtc = holdVenceUtc.ToString("O", CultureInfo.InvariantCulture),
            Moneda,
            Tramos = actual.Tramos.OrderBy(tramo => tramo.Orden).Select(tramo => new
            {
                tramo.Orden,
                tramo.ServicioId,
                Importe = tramo.Importe.ToString("F2", CultureInfo.InvariantCulture)
            }),
            PrecioTotal = actual.PrecioTotal.ToString("F2", CultureInfo.InvariantCulture)
        };
        var json = JsonSerializer.Serialize(contenido);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
