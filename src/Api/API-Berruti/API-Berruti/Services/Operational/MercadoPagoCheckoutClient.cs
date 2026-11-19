using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using API_Berruti.Models.Operational;

namespace API_Berruti.Services.Operational;

public sealed class MercadoPagoCheckoutClient : IMercadoPagoCheckoutClient
{
    private static readonly Uri OrdersEndpoint = new("https://api.mercadopago.com/v1/orders");
    private static readonly Uri UserProfileEndpoint = new("https://api.mercadolibre.com/users/me");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly long _expectedSellerId;
    private readonly string _expectedSiteId;

    public MercadoPagoCheckoutClient(HttpClient httpClient, ParametrosMercadoPago parametros)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(parametros);
        _accessToken = parametros.AccessToken;
        _expectedSellerId = parametros.ExpectedSellerId;
        _expectedSiteId = parametros.ExpectedSiteId;
        _httpClient.Timeout = RequestTimeout;
    }

    public async Task<OrdenCheckoutMercadoPago> CrearOrdenAsync(
        Guid claveIdempotencia,
        string referenciaExterna,
        decimal importe,
        CancellationToken cancellationToken = default)
    {
        ValidarClaveIdempotencia(claveIdempotencia);
        ValidarDatosEsperados(referenciaExterna, importe);
        await VerificarIdentidadTestAsync(cancellationToken);

        var body = new CrearOrdenRequest
        {
            Type = "online",
            ProcessingMode = "manual",
            TotalAmount = importe.ToString("F2", CultureInfo.InvariantCulture),
            ExternalReference = referenciaExterna
        };
        using var request = CrearRequest(HttpMethod.Post, OrdersEndpoint);
        request.Headers.Add("X-Idempotency-Key", claveIdempotencia.ToString("D"));
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var document = await EnviarAsync(request, cancellationToken);
        var orden = LeerOrden(document.RootElement, expectedId: null, referenciaExterna, importe);
        var checkoutUrl = LeerStringOpcional(document.RootElement, "checkout_url");
        if (!EsCheckoutUrlSeguro(checkoutUrl))
            throw ErrorProveedor();

        return orden with { CheckoutUrl = checkoutUrl };
    }

    public async Task<OrdenCheckoutMercadoPago> ConsultarOrdenAsync(
        string ordenId,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        ValidarIdOrden(ordenId);
        ValidarDatosEsperados(referenciaExternaEsperada, importeEsperado);
        await VerificarIdentidadTestAsync(cancellationToken);

        var endpoint = new Uri($"{OrdersEndpoint.AbsoluteUri}/{Uri.EscapeDataString(ordenId)}");
        using var request = CrearRequest(HttpMethod.Get, endpoint);
        using var document = await EnviarAsync(request, cancellationToken);
        var orden = LeerOrden(document.RootElement, ordenId, referenciaExternaEsperada, importeEsperado);
        var checkoutUrl = LeerStringOpcional(document.RootElement, "checkout_url");
        if (checkoutUrl is not null && !EsCheckoutUrlSeguro(checkoutUrl))
            throw ErrorProveedor();

        return orden with { CheckoutUrl = checkoutUrl };
    }

    public async Task<OrdenCheckoutMercadoPago?> BuscarOrdenAsync(
        string referenciaExternaEsperada,
        DateTime desdeUtc,
        DateTime hastaUtc,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        ValidarDatosEsperados(referenciaExternaEsperada, importeEsperado);
        if (desdeUtc.Kind != DateTimeKind.Utc || hastaUtc.Kind != DateTimeKind.Utc || hastaUtc <= desdeUtc)
            throw new ArgumentException("La ventana de recuperación debe ser UTC y creciente.");

        await VerificarIdentidadTestAsync(cancellationToken);

        static string Formatear(DateTime valorUtc) =>
            new DateTimeOffset(valorUtc).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        var query = string.Join("&", new[]
        {
            $"begin_date={Uri.EscapeDataString(Formatear(desdeUtc))}",
            $"end_date={Uri.EscapeDataString(Formatear(hastaUtc))}",
            $"external_reference={Uri.EscapeDataString(referenciaExternaEsperada)}",
            "limit=2",
            "offset=0"
        });
        using var request = CrearRequest(HttpMethod.Get, new Uri($"{OrdersEndpoint.AbsoluteUri}?{query}"));
        using var document = await EnviarAsync(request, cancellationToken);

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array
            || !root.TryGetProperty("paging", out var paging)
            || paging.ValueKind != JsonValueKind.Object
            || !TryReadInt(paging, "total", out var total)
            || !TryReadInt(paging, "total_pages", out var totalPages)
            || !TryReadInt(paging, "offset", out var offset)
            || !TryReadInt(paging, "limit", out var limit)
            || total < 0 || totalPages < 0 || offset != 0 || limit <= 0 || total > limit
            || total != data.GetArrayLength())
            throw ErrorProveedor();

        if (total == 0)
        {
            if (totalPages > 1) throw ErrorProveedor();
            return null;
        }
        if (total != 1 || totalPages != 1)
            throw ErrorProveedor();

        var orden = LeerOrden(data[0], expectedId: null, referenciaExternaEsperada, importeEsperado);
        ValidarIdOrden(orden.Id);
        return await ConsultarOrdenAsync(orden.Id, referenciaExternaEsperada, importeEsperado, cancellationToken);
    }

    public async Task SolicitarDevolucionCompletaAsync(
        string ordenId,
        Guid claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ValidarIdOrden(ordenId);
        ValidarClaveIdempotencia(claveIdempotencia);
        await VerificarIdentidadTestAsync(cancellationToken);

        var endpoint = new Uri($"{OrdersEndpoint.AbsoluteUri}/{Uri.EscapeDataString(ordenId)}/refund");
        using var request = CrearRequest(HttpMethod.Post, endpoint);
        request.Headers.Add("X-Idempotency-Key", claveIdempotencia.ToString("D"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw ErrorProveedor();
    }

    private HttpRequestMessage CrearRequest(HttpMethod method, Uri endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task VerificarIdentidadTestAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserProfileEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw ErrorProveedor();

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("id", out var id)
                || id.ValueKind != JsonValueKind.Number
                || !id.TryGetInt64(out var sellerId)
                || sellerId != _expectedSellerId
                || !root.TryGetProperty("site_id", out var siteId)
                || siteId.ValueKind != JsonValueKind.String
                || !string.Equals(siteId.GetString(), _expectedSiteId, StringComparison.Ordinal)
                || !root.TryGetProperty("tags", out var tags)
                || tags.ValueKind != JsonValueKind.Array
                || !tags.EnumerateArray().Any(tag =>
                    tag.ValueKind == JsonValueKind.String
                    && string.Equals(tag.GetString(), "test_user", StringComparison.Ordinal)))
                throw ErrorProveedor();
        }
        catch (JsonException)
        {
            throw ErrorProveedor();
        }
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
        var currency = LeerStringRequerido(root, "currency");
        var status = LeerStringRequerido(root, "status");
        var paidAmount = root.TryGetProperty("total_paid_amount", out _)
            ? LeerDecimalRequerido(root, "total_paid_amount")
            : string.Equals(status, "created", StringComparison.Ordinal)
                ? 0m
                : throw ErrorProveedor();
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

    private static bool TryReadInt(JsonElement root, string name, out int value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetInt32(out value);

        return element.ValueKind == JsonValueKind.String
            && int.TryParse(element.GetString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static string? LeerStringOpcional(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw ErrorProveedor();
        return value.GetString();
    }

    private static bool EsCheckoutUrlSeguro(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttps
            && string.Equals(uri.Host, "www.mercadopago.com.uy", StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    private static decimal LeerDecimalRequerido(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
            throw ErrorProveedor();

        var parsed = value.ValueKind switch
        {
            JsonValueKind.String => decimal.TryParse(value.GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsedString)
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

    private static void ValidarClaveIdempotencia(Guid claveIdempotencia)
    {
        if (claveIdempotencia == Guid.Empty)
            throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(claveIdempotencia));
    }

    private static InvalidOperationException ErrorProveedor() =>
        new("Mercado Pago no devolvió una respuesta válida para la operación.");

    private sealed class CrearOrdenRequest
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("processing_mode")]
        public required string ProcessingMode { get; init; }

        [JsonPropertyName("total_amount")]
        public required string TotalAmount { get; init; }

        [JsonPropertyName("external_reference")]
        public required string ExternalReference { get; init; }
    }

}
