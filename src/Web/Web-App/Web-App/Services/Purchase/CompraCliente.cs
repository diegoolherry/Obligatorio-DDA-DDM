using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Purchase;
using Web_App.Models.Operational;
using Web_App.Services.Authentication;

namespace Web_App.Services.Purchase;

public sealed class CompraCliente
{
    private const string ErrorSeguro = "La API no pudo completar la compra. Revisá la conexión y verificá el estado del mismo intento.";
    private static readonly JsonSerializerOptions Json = CrearJson();
    private readonly ISesionCliente sesion;

    public CompraCliente(ISesionCliente sesion)
    {
        this.sesion = sesion;
    }

    public async Task<ResultadoCompra<IniciarIntentoCompraResponseDto>> CrearIntentoAsync(
        IReadOnlyList<SeleccionTramoCompraDto> tramos)
    {
        var resultado = await EnviarAsync<IniciarIntentoCompraResponseDto>(HttpMethod.Post, "/api/intentos-compra",
            new IniciarIntentoCompraRequestDto { Tramos = tramos.ToList() }, ambigua: true);
        if (resultado.Tipo != TipoResultadoCompra.Exito || resultado.Datos is not { } intento) return resultado;

        return EsIntentoValido(intento)
            ? resultado
            : new(TipoResultadoCompra.Ambiguo, default, resultado.EstadoHttp,
                "No se pudo confirmar el resultado. Verificá el estado del mismo intento; no crees otro.");
    }

    public async Task<ResultadoCompra<IntentoPendienteResponseDto>> ObtenerIntentoPendienteAsync()
    {
        var respuesta = await sesion.SolicitarAutenticadaAsync(HttpMethod.Get, "/api/intentos-compra/pendiente", null);
        if (respuesta.Exito && respuesta.EstadoHttp == (int)HttpStatusCode.NoContent)
            return new(TipoResultadoCompra.SinPendiente, default, respuesta.EstadoHttp, "No hay una compra pendiente.");
        var resultado = Convertir<IntentoPendienteResponseDto>(respuesta, ambigua: false);
        if (resultado.Tipo != TipoResultadoCompra.Exito || resultado.Datos is not { } pendiente) return resultado;
        var valido = EsIntentoValido(pendiente.Intento)
            && (pendiente.Pago is null || EsPagoValido(pendiente.Pago, pendiente.Intento.IntentoId));
        return valido ? resultado : Error<IntentoPendienteResponseDto>(resultado.EstadoHttp, ErrorSeguro);
    }

    public Task<ResultadoCompra<CotizarCompraResponseDto>> ObtenerCotizacionAsync(string intentoId) =>
        EnviarAsync<CotizarCompraResponseDto>(HttpMethod.Get, Ruta(intentoId, "/cotizacion"), cuerpo: null, ambigua: false);

    public async Task<ResultadoCompra<PagoResponseDto>> IniciarPagoAsync(string intentoId, string huella)
    {
        if (!EsIdentificadorValido(intentoId)) return Error<PagoResponseDto>(null, "El intento de compra no es válido.");
        if (!EsHuellaValida(huella)) return Error<PagoResponseDto>(null, "La cotización aceptada no es válida.");
        var ruta = Ruta(intentoId, "/pagos");
        var respuesta = await sesion.SolicitarAutenticadaAsync(HttpMethod.Post, ruta,
            JsonSerializer.Serialize(new IniciarPagoRequestDto { HuellaCotizacionAceptada = huella }, Json));
        if (!respuesta.Exito && respuesta.EstadoHttp == (int)HttpStatusCode.Conflict)
        {
            var actual = await ObtenerCotizacionAsync(intentoId);
            return actual.Tipo == TipoResultadoCompra.Exito
                ? new(TipoResultadoCompra.CotizacionModificada, default, respuesta.EstadoHttp,
                    "El precio cambió. Revisá y aceptá nuevamente la cotización vigente.", actual.Datos)
                : Error<PagoResponseDto>(actual.EstadoHttp, "No se pudo cargar la cotización vigente. No se inició otro pago.");
        }
        var resultado = Convertir<PagoResponseDto>(respuesta, ambigua: true);
        if (resultado.Tipo != TipoResultadoCompra.Exito || resultado.Datos is not { } pago) return resultado;
        return EsPagoValido(pago, intentoId) ? resultado : Ambiguo<PagoResponseDto>(resultado.EstadoHttp);
    }

    public async Task<ResultadoCompra<SincronizarPagoResponseDto>> SincronizarPagoAsync(string intentoId)
    {
        var resultado = await EnviarAsync<SincronizarPagoResponseDto>(HttpMethod.Post, Ruta(intentoId, "/pagos/sincronizar"),
            cuerpo: null, ambigua: true);
        if (resultado.Tipo != TipoResultadoCompra.Exito || resultado.Datos is not { } sincronizacion) return resultado;
        var compraValida = sincronizacion.Compra is null
            || EsCompraValida(sincronizacion.Compra, intentoId, sincronizacion.Pago?.PagoId);
        return EsPagoValido(sincronizacion.Pago, intentoId) && compraValida
            ? resultado : Ambiguo<SincronizarPagoResponseDto>(resultado.EstadoHttp);
    }

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
                estado == TipoResultadoCompra.Ambiguo ? "No se pudo confirmar el resultado. Verificá el estado del mismo intento; no crees otro." :
                respuesta.EstadoHttp == 401 ? "La sesión venció. Ingresá nuevamente." : ErrorSeguro);
        }
        try
        {
            var datos = string.IsNullOrWhiteSpace(respuesta.Contenido) ? default : JsonSerializer.Deserialize<T>(respuesta.Contenido, Json);
            return datos is null
                ? new(ambigua ? TipoResultadoCompra.Ambiguo : TipoResultadoCompra.Error, default, respuesta.EstadoHttp,
                    ambigua ? "No se pudo confirmar el resultado. Verificá el estado del mismo intento; no crees otro." : ErrorSeguro)
                : new(TipoResultadoCompra.Exito, datos, respuesta.EstadoHttp, "Operación completada.");
        }
        catch (JsonException)
        {
            return new(ambigua ? TipoResultadoCompra.Ambiguo : TipoResultadoCompra.Error, default, respuesta.EstadoHttp,
                ambigua ? "No se pudo confirmar el resultado. Verificá el estado del mismo intento; no crees otro." : ErrorSeguro);
        }
    }

    private static ResultadoCompra<T> Error<T>(int? status, string message) =>
        new(TipoResultadoCompra.Error, default, status, message);

    private static bool EsIdentificadorValido(string? identificador) =>
        !string.IsNullOrWhiteSpace(identificador)
        && System.Text.RegularExpressions.Regex.IsMatch(identificador, "^[A-Za-z0-9-]+$");

    private static string Ruta(string intentoId, string sufijo) =>
        EsIdentificadorValido(intentoId)
            ? $"/api/intentos-compra/{intentoId}{sufijo}" : string.Empty;

    private static bool EsIntentoValido(IniciarIntentoCompraResponseDto? intento) =>
        intento is not null
        && EsIdentificadorValido(intento.IntentoId)
        && intento.Itinerario?.Tramos is { Count: > 0 } legs
        && legs.All(leg => leg is not null && leg.Orden > 0 && !string.IsNullOrWhiteSpace(leg.ServicioId))
        && EsUtcNoPredeterminado(intento.HoldVenceEn);

    private static bool EsPagoValido(PagoResponseDto? pago, string intentoId) =>
        pago is not null
        && EsIdentificadorValido(pago.PagoId)
        && EsIdentificadorValido(pago.IntentoId)
        && string.Equals(pago.IntentoId, intentoId, StringComparison.Ordinal)
        && Enum.IsDefined(typeof(EstadoPago), pago.Estado)
        && EsUtcNoPredeterminado(pago.HoldVenceEn);

    private static bool EsCompraValida(CompraConfirmadaResponseDto? compra, string intentoId, string? pagoId) =>
        compra is not null
        && EsIdentificadorValido(compra.CompraId)
        && string.Equals(compra.IntentoId, intentoId, StringComparison.Ordinal)
        && EsIdentificadorValido(compra.PagoId)
        && string.Equals(compra.PagoId, pagoId, StringComparison.Ordinal)
        && EsUtcNoPredeterminado(compra.ConfirmadoUtc)
        && compra.Tickets is { Count: > 0 } tickets
        && tickets.All(ticket => ticket is not null
            && EsIdentificadorValido(ticket.TicketId)
            && ticket.Orden > 0
            && !string.IsNullOrWhiteSpace(ticket.ServicioId)
            && !string.IsNullOrWhiteSpace(ticket.TramoId)
            && !string.IsNullOrWhiteSpace(ticket.QrOpaco));

    private static bool EsUtcNoPredeterminado(DateTime fecha) =>
        fecha != default && fecha.Kind == DateTimeKind.Utc;

    private static ResultadoCompra<T> Ambiguo<T>(int? status) =>
        new(TipoResultadoCompra.Ambiguo, default, status,
            "No se pudo confirmar el resultado. Verificá el estado del mismo intento; no crees otro.");

    private static bool EsHuellaValida(string huella) =>
        huella.Length == 64 && huella.All(Uri.IsHexDigit);

    private static JsonSerializerOptions CrearJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
