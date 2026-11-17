using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public sealed class MercadoPagoCheckoutClient : IMercadoPagoCheckoutClient
{
    private static readonly Uri OrdersEndpoint = new("https://api.mercadopago.com/v1/orders");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;

    public MercadoPagoCheckoutClient(HttpClient httpClient, ParametrosMercadoPago parametros)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(parametros);
        _accessToken = parametros.AccessToken;
        _httpClient.Timeout = RequestTimeout;
    }

    public async Task<OrdenCheckoutMercadoPago> ConsultarOrdenAsync(
        string ordenId,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        ValidarIdOrden(ordenId);
        ValidarDatosEsperados(referenciaExternaEsperada, importeEsperado);

        var endpoint = new Uri($"{OrdersEndpoint.AbsoluteUri}/{Uri.EscapeDataString(ordenId)}");
        using var request = CrearRequest(HttpMethod.Get, endpoint);
        using var document = await EnviarAsync(request, cancellationToken);
        return LeerOrden(document.RootElement, ordenId, referenciaExternaEsperada, importeEsperado);
    }

    private HttpRequestMessage CrearRequest(HttpMethod method, Uri endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<JsonDocument> EnviarAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw ErrorProveedor();

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            throw ErrorProveedor();
        }
    }

    private static OrdenCheckoutMercadoPago LeerOrden(
        JsonElement root,
        string? expectedId,
        string expectedReference,
        decimal expectedAmount)
    {
        var id = LeerStringRequerido(root, "id");
        var reference = LeerStringRequerido(root, "external_reference");
        var amount = LeerDecimalRequerido(root, "total_amount");
        var paidAmount = LeerDecimalRequerido(root, "total_paid_amount");
        var currency = LeerStringRequerido(root, "currency");
        var status = LeerStringRequerido(root, "status");
        var statusDetail = LeerStringOpcional(root, "status_detail");

        if (id.Length > 256
            || (expectedId is not null && !string.Equals(id, expectedId, StringComparison.Ordinal))
            || !string.Equals(reference, expectedReference, StringComparison.Ordinal)
            || amount != expectedAmount
            || paidAmount < 0
            || paidAmount > amount
            || !string.Equals(currency, "UYU", StringComparison.Ordinal))
            throw ErrorProveedor();

        if (root.TryGetProperty("live_mode", out var liveMode)
            && (liveMode.ValueKind != JsonValueKind.False))
            throw ErrorProveedor();

        return new OrdenCheckoutMercadoPago
        {
            Id = id,
            ReferenciaExterna = reference,
            Importe = amount,
            ImportePagado = paidAmount,
            Moneda = currency,
            Estado = MapearEstado(status, statusDetail, paidAmount, amount)
        };
    }

    private static EstadoPago MapearEstado(string status, string? statusDetail, decimal paidAmount, decimal expectedAmount) => (status, statusDetail) switch
    {
        ("processed", "accredited") when paidAmount == expectedAmount => EstadoPago.Aprobado,
        ("processed", "refunded") or ("refunded", "refunded") => EstadoPago.Devuelto,
        ("processed", "partially_refunded") => EstadoPago.DevolucionPendiente,
        ("created", "created") => EstadoPago.Pendiente,
        ("processing", "pending_review_manual") or ("processing", "in_process") => EstadoPago.Pendiente,
        ("action_required", "waiting_capture") => EstadoPago.Pendiente,
        ("failed", "bad_filled_card_data")
            or ("failed", "invalid_card_token")
            or ("failed", "high_risk")
            or ("failed", "rejected_by_issuer")
            or ("failed", "required_call_for_authorize")
            or ("failed", "max_attempts_exceeded")
            or ("failed", "card_disabled")
            or ("failed", "card_insufficient_amount")
            or ("failed", "amount_limit_exceeded")
            or ("failed", "invalid_installments")
            or ("failed", "processing_error") => EstadoPago.Rechazado,
        ("canceled", "canceled") => EstadoPago.Rechazado,
        _ => EstadoPago.Desconocido
    };

    private static string LeerStringRequerido(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
            throw ErrorProveedor();
        return value.GetString()!;
    }

    private static string? LeerStringOpcional(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw ErrorProveedor();
        return value.GetString();
    }

    private static decimal LeerDecimalRequerido(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            throw ErrorProveedor();

        var parsed = value.ValueKind switch
        {
            JsonValueKind.String => decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedString)
                ? parsedString
                : throw ErrorProveedor(),
            JsonValueKind.Number => value.TryGetDecimal(out var parsedNumber)
                ? parsedNumber
                : throw ErrorProveedor(),
            _ => throw ErrorProveedor()
        };
        return parsed;
    }

    private static void ValidarDatosEsperados(string referencia, decimal importe)
    {
        if (string.IsNullOrWhiteSpace(referencia)
            || referencia.Length > 256
            || !string.Equals(referencia, referencia.Trim(), StringComparison.Ordinal)
            || importe <= 0
            || decimal.Round(importe, 2, MidpointRounding.ToEven) != importe)
            throw new ArgumentException("La referencia y el importe de la orden no son válidos.");
    }

    private static void ValidarIdOrden(string ordenId)
    {
        if (string.IsNullOrWhiteSpace(ordenId)
            || ordenId.Length > 256
            || ordenId is "." or ".."
            || !string.Equals(ordenId, ordenId.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("El identificador de orden no es válido.", nameof(ordenId));
    }

    private static InvalidOperationException ErrorProveedor() =>
        new("Mercado Pago no devolvió una respuesta válida para la operación.");

}
