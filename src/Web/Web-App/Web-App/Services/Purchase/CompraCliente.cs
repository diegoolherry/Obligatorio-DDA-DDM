using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Purchase;
using Web_App.Models.Operational;
using Web_App.Services.Authentication;

namespace Web_App.Services.Purchase;

public sealed class CompraCliente(ISesionCliente sesion)
{
    private const string ErrorSeguro = "La API no pudo completar la compra. Revise la conexión y verifique el estado del mismo intento.";
    private static readonly JsonSerializerOptions Json = CrearJson();

    public async Task<ResultadoCompra<IniciarIntentoCompraResponseDto>> CrearIntentoAsync(
        IReadOnlyList<SeleccionTramoCompraDto> tramos)
    {
        var resultado = await EnviarAsync<IniciarIntentoCompraResponseDto>(HttpMethod.Post, "/api/intentos-compra",
            new IniciarIntentoCompraRequestDto { Tramos = tramos.ToList() }, ambigua: true);
        if (resultado.Tipo != TipoResultadoCompra.Exito || resultado.Datos is not { } intento) return resultado;

        var respuestaValida = EsIntentoValido(intento.IntentoId)
            && intento.Itinerario?.Tramos is { Count: > 0 } legs
            && legs.All(leg => leg is not null && leg.Orden > 0 && !string.IsNullOrWhiteSpace(leg.ServicioId))
            && intento.HoldVenceEn != default && intento.HoldVenceEn.Kind == DateTimeKind.Utc;
        return respuestaValida
            ? resultado
            : new(TipoResultadoCompra.Ambiguo, default, resultado.EstadoHttp,
                "No se pudo confirmar el resultado. Verifique el estado del mismo intento; no cree otro.");
    }

    public Task<ResultadoCompra<CotizarCompraResponseDto>> ObtenerCotizacionAsync(string intentoId) =>
        EnviarAsync<CotizarCompraResponseDto>(HttpMethod.Get, Ruta(intentoId, "/cotizacion"), cuerpo: null, ambigua: false);

    public async Task<ResultadoCompra<PagoResponseDto>> IniciarPagoAsync(string intentoId, string huella)
    {
        if (!EsHuellaValida(huella)) return Error<PagoResponseDto>(null, "La cotización aceptada no es válida.");
        var ruta = Ruta(intentoId, "/pagos");
        var respuesta = await sesion.SolicitarAutenticadaAsync(HttpMethod.Post, ruta,
            JsonSerializer.Serialize(new IniciarPagoRequestDto { HuellaCotizacionAceptada = huella }, Json));
        if (!respuesta.Exito && respuesta.EstadoHttp == (int)HttpStatusCode.Conflict)
        {
            var actual = await ObtenerCotizacionAsync(intentoId);
            return actual.Tipo == TipoResultadoCompra.Exito
                ? new(TipoResultadoCompra.CotizacionModificada, default, respuesta.EstadoHttp,
                    "El precio cambió. Revise y aceptá nuevamente la cotización vigente.", actual.Datos)
                : Error<PagoResponseDto>(actual.EstadoHttp, "No se pudo cargar la cotización vigente. No se inició otro pago.");
        }
        return Convertir<PagoResponseDto>(respuesta, ambigua: true);
    }

    public Task<ResultadoCompra<SincronizarPagoResponseDto>> SincronizarPagoAsync(string intentoId) =>
        EnviarAsync<SincronizarPagoResponseDto>(HttpMethod.Post, Ruta(intentoId, "/pagos/sincronizar"),
            cuerpo: null, ambigua: true);

    private async Task<ResultadoCompra<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object? cuerpo, bool ambigua)
    {
        if (string.IsNullOrEmpty(ruta)) return Error<T>(null, "El intento de compra no es válido.");
        var json = cuerpo is null ? null : JsonSerializer.Serialize(cuerpo, Json);
        var respuesta = await sesion.SolicitarAutenticadaAsync(metodo, ruta, json);
        return Convertir<T>(respuesta, ambigua);
    }

    private static ResultadoCompra<T> Convertir<T>(RespuestaApiAdministracion respuesta, bool ambigua)
    {
        if (!respuesta.Exito)
        {
            var estado = ambigua && (!respuesta.EstadoHttp.HasValue || respuesta.EstadoHttp >= 500)
                ? TipoResultadoCompra.Ambiguo : TipoResultadoCompra.Error;
            return new(estado, default, respuesta.EstadoHttp,
                estado == TipoResultadoCompra.Ambiguo ? "No se pudo confirmar el resultado. Verifique el estado del mismo intento; no crees otro." :
                respuesta.EstadoHttp == 401 ? "La sesión venció. Ingrese nuevamente." : ErrorSeguro);
        }
        try
        {
            var datos = string.IsNullOrWhiteSpace(respuesta.Contenido) ? default : JsonSerializer.Deserialize<T>(respuesta.Contenido, Json);
            return datos is null
                ? new(ambigua ? TipoResultadoCompra.Ambiguo : TipoResultadoCompra.Error, default, respuesta.EstadoHttp,
                    ambigua ? "No se pudo confirmar el resultado. Verifique el estado del mismo intento; no crees otro." : ErrorSeguro)
                : new(TipoResultadoCompra.Exito, datos, respuesta.EstadoHttp, "Operación completada.");
        }
        catch (JsonException)
        {
            return new(ambigua ? TipoResultadoCompra.Ambiguo : TipoResultadoCompra.Error, default, respuesta.EstadoHttp,
                ambigua ? "No se pudo confirmar el resultado. Verifique el estado del mismo intento; no crees otro." : ErrorSeguro);
        }
    }

    private static ResultadoCompra<T> Error<T>(int? status, string message) =>
        new(TipoResultadoCompra.Error, default, status, message);

    private static bool EsIntentoValido(string? intentoId) =>
        !string.IsNullOrWhiteSpace(intentoId) && System.Text.RegularExpressions.Regex.IsMatch(intentoId, "^[A-Za-z0-9-]+$");

    private static string Ruta(string intentoId, string sufijo) =>
        EsIntentoValido(intentoId)
            ? $"/api/intentos-compra/{intentoId}{sufijo}" : string.Empty;

    private static bool EsHuellaValida(string huella) =>
        huella.Length == 64 && huella.All(Uri.IsHexDigit);

    private static JsonSerializerOptions CrearJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
