using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

try
{
    if (args.Length == 1 && args[0] == "--verify-failure-exit")
        throw new InvalidOperationException("CONTROLLED-TEST-HARNESS-FAILURE");

    var checks = 0;
    var failures = 0;

void Check(bool condition, string name)
{
    checks++;
    if (condition)
    {
        Console.WriteLine($"PASS: {name}");
    }
    else
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name}");
    }
}

if (args.Length == 1 && args[0] == "--mercado-pago-test-identity")
{
    await RunMercadoPagoIdentityChecksAsync();
    Console.WriteLine($"{checks - failures}/{checks} focused provider identity and search checks passed.");
    return failures == 0 ? 0 : 1;
}

async Task RunMercadoPagoIdentityChecksAsync()
{
    const string opaqueTestToken = "mock-token-not-a-credential";
    const string sellerId = "987654321";
    const string siteId = "SITE_TEST_FIXTURE";
    const string validIdentity = "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"normal\",\"test_user\"]}";
    const string validOrder = "{\"id\":\"order-1\",\"external_reference\":\"attempt-reference\",\"total_amount\":\"125.50\",\"total_paid_amount\":\"125.50\",\"currency\":\"UYU\",\"status\":\"processed\",\"status_detail\":\"accredited\",\"checkout_url\":\"https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1\"}";
    const string validSearch = "{\"data\":[],\"paging\":{\"total\":0,\"total_pages\":0,\"offset\":0,\"limit\":2}}";

    ParametrosMercadoPago CreateParameters(string token = opaqueTestToken, bool confirmed = true,
        string configuredSeller = sellerId, string configuredSite = siteId)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MercadoPago:AccessToken"] = token,
            ["MercadoPago:TestCredentialOriginConfirmed"] = confirmed.ToString(),
            ["MercadoPago:ExpectedSellerId"] = configuredSeller,
            ["MercadoPago:ExpectedSiteId"] = configuredSite
        }).Build();
        return ParametrosMercadoPago.Desde(configuration);
    }

    var opaqueTokenAccepted = false;
    try
    {
        opaqueTokenAccepted = CreateParameters().AccessToken == opaqueTestToken;
    }
    catch (InvalidOperationException)
    {
        // The focused assertion below reports the unsupported credential format as RED.
    }
    Check(opaqueTokenAccepted, "explicitly confirmed opaque test credential is not classified by token prefix");

    foreach (var invalidConfiguration in new (string Name, string Token, bool Confirmed, string Seller, string Site)[]
    {
        ("missing token", "", true, sellerId, siteId),
        ("missing test-origin confirmation", opaqueTestToken, false, sellerId, siteId),
        ("missing expected seller", opaqueTestToken, true, "", siteId),
        ("invalid expected seller", opaqueTestToken, true, "not-numeric", siteId),
        ("missing expected site", opaqueTestToken, true, sellerId, "")
    })
    {
        var rejected = false;
        try
        {
            _ = CreateParameters(invalidConfiguration.Token, invalidConfiguration.Confirmed,
                invalidConfiguration.Seller, invalidConfiguration.Site);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Check(rejected, $"configuration fails closed for {invalidConfiguration.Name}");
    }

    var operations = new (string Name, Func<MercadoPagoCheckoutClient, Task> Invoke)[]
    {
        ("create", client => client.CrearOrdenAsync(Guid.NewGuid(), "attempt-reference", 125.50m)),
        ("get", client => client.ConsultarOrdenAsync("order-1", "attempt-reference", 125.50m)),
        ("search", client => client.BuscarOrdenAsync("attempt-reference",
            new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 17, 12, 1, 0, DateTimeKind.Utc), 125.50m)),
        ("refund", client => client.SolicitarDevolucionCompletaAsync("order-1", Guid.NewGuid()))
    };

    foreach (var operation in operations)
    {
        var validRequests = new List<Uri>();
        var identityAuthorizationValid = false;
        var validHandler = new MockHttpMessageHandler((request, _) =>
        {
            validRequests.Add(request.RequestUri!);
            if (request.RequestUri!.Host == "api.mercadopago.com")
            {
                var body = request.RequestUri.Query.Contains("external_reference", StringComparison.Ordinal)
                    ? validSearch : validOrder;
                return JsonResponse(HttpStatusCode.OK, body);
            }
            return JsonResponse(HttpStatusCode.InternalServerError, "unexpected endpoint");
        })
        {
            IdentityResponseFactory = (request, _) =>
            {
                identityAuthorizationValid = request.Method == HttpMethod.Get
                    && request.RequestUri == new Uri("https://api.mercadolibre.com/users/me")
                    && request.Headers.Authorization?.Scheme == "Bearer"
                    && request.Headers.Authorization.Parameter == "TEST-contract-token";
                return JsonResponse(HttpStatusCode.OK, validIdentity);
            }
        };
        var validClient = new MercadoPagoCheckoutClient(new HttpClient(validHandler), CreateParameters("TEST-contract-token"));
        await operation.Invoke(validClient);
        Check(validHandler.IdentityRequestCount == 1 && validRequests.Count == 1
            && validRequests[0].Host == "api.mercadopago.com",
            $"{operation.Name} verifies identity before its provider request");
        Check(identityAuthorizationValid, $"{operation.Name} sends the private token only to the fixed identity endpoint");

        var invalidIdentities = new (string Name, HttpStatusCode Status, string Body)[]
        {
            ("missing test_user tag", HttpStatusCode.OK, "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"normal\"]}"),
            ("wrong seller", HttpStatusCode.OK, "{\"id\":987654322,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}"),
            ("wrong site", HttpStatusCode.OK, "{\"id\":987654321,\"site_id\":\"OTHER_SITE\",\"tags\":[\"test_user\"]}"),
            ("missing seller ID", HttpStatusCode.OK, "{\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}"),
            ("non-numeric seller ID", HttpStatusCode.OK, "{\"id\":\"987654321\",\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}"),
            ("missing site", HttpStatusCode.OK, "{\"id\":987654321,\"tags\":[\"test_user\"]}"),
            ("null tags", HttpStatusCode.OK, "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":null}"),
            ("null profile", HttpStatusCode.OK, "null"),
            ("malformed identity", HttpStatusCode.OK, "not-json"),
            ("provider error", HttpStatusCode.Unauthorized, "{}"),
            ("redirect", HttpStatusCode.Redirect, "{}")
        };

        foreach (var invalidIdentity in invalidIdentities)
        {
            var providerRequests = 0;
            var invalidHandler = new MockHttpMessageHandler((request, _) =>
            {
                providerRequests++;
                if (request.RequestUri!.Query.Contains("external_reference", StringComparison.Ordinal))
                    return JsonResponse(HttpStatusCode.OK, validSearch);
                return request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/refund", StringComparison.Ordinal)
                    ? new HttpResponseMessage(HttpStatusCode.NoContent)
                    : JsonResponse(HttpStatusCode.OK, validOrder);
            })
            {
                IdentityResponseFactory = (_, _) => JsonResponse(invalidIdentity.Status, invalidIdentity.Body)
            };
            var invalidClient = new MercadoPagoCheckoutClient(new HttpClient(invalidHandler), CreateParameters("TEST-contract-token"));
            var rejected = false;
            try
            {
                await operation.Invoke(invalidClient);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected && invalidHandler.IdentityRequestCount == 1 && providerRequests == 0,
                $"{operation.Name} fails closed before provider call for {invalidIdentity.Name}");
        }
    }

    const string searchOrder = "{\"id\":\"order-1\",\"external_reference\":\"attempt-reference\",\"total_amount\":\"125.50\",\"total_paid_amount\":\"125.50\",\"currency\":\"UYU\",\"status\":\"processed\",\"status_detail\":\"accredited\"}";
    static string SearchPage(string data, string paging) => $"{{\"data\":[{data}],\"paging\":{{{paging}}}}}";

    var zeroSearchRejected = false;
    try
    {
        var zeroSearch = await SearchWithResponseAsync(SearchPage("", "\"total\":0,\"total_pages\":0,\"offset\":0,\"limit\":\"20\""));
        Check(zeroSearch is null, "empty search page accepts provider effective limit as an integer string");
    }
    catch (InvalidOperationException)
    {
        zeroSearchRejected = true;
    }
    Check(!zeroSearchRejected, "empty search page is not rejected because the provider used a larger effective limit");

    var oneSearchRejected = false;
    try
    {
        var oneSearch = await SearchWithResponseAsync(SearchPage(searchOrder, "\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":\"20\""));
        Check(oneSearch?.Id == "order-1", "one exact search match accepts a larger effective page limit");
    }
    catch (InvalidOperationException)
    {
        oneSearchRejected = true;
    }
    Check(!oneSearchRejected, "unique exact search result survives the provider's larger effective limit");

    var overLimitData = string.Join(',', Enumerable.Repeat(searchOrder, 21));
    var invalidSearchPages = new (string Name, string Body)[]
    {
        ("response count exceeds effective limit", SearchPage(overLimitData, "\"total\":21,\"total_pages\":2,\"offset\":0,\"limit\":\"20\"")),
        ("inconsistent total pages", SearchPage(searchOrder, "\"total\":1,\"total_pages\":2,\"offset\":0,\"limit\":\"20\"")),
        ("nonzero search offset", SearchPage(searchOrder, "\"total\":1,\"total_pages\":1,\"offset\":1,\"limit\":\"20\"")),
        ("missing effective page limit", SearchPage(searchOrder, "\"total\":1,\"total_pages\":1,\"offset\":0"))
    };
    foreach (var invalidPage in invalidSearchPages)
        await CheckThrowsAsync<InvalidOperationException>(
            () => SearchWithResponseAsync(invalidPage.Body),
            $"search rejects {invalidPage.Name}");

    async Task<OrdenCheckoutMercadoPago?> SearchWithResponseAsync(string body)
    {
        var requestedLimit = false;
        var handler = new MockHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/orders/order-1", StringComparison.Ordinal))
                return JsonResponse(HttpStatusCode.OK, validOrder);
            requestedLimit = request.RequestUri.Query.Contains("limit=2", StringComparison.Ordinal);
            return JsonResponse(HttpStatusCode.OK, body);
        })
        {
            IdentityResponseFactory = (_, _) => JsonResponse(HttpStatusCode.OK, validIdentity)
        };
        var client = new MercadoPagoCheckoutClient(new HttpClient(handler), CreateParameters("TEST-contract-token"));
        var result = await client.BuscarOrdenAsync("attempt-reference",
            new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 17, 12, 1, 0, DateTimeKind.Utc), 125.50m);
        Check(requestedLimit, "search request asks for a page size without assuming the response limit is identical");
        return result;
    }

    var canceledHandler = new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, validOrder))
    {
        IdentityResponseFactory = (_, _) => throw new OperationCanceledException()
    };
    var canceledClient = new MercadoPagoCheckoutClient(new HttpClient(canceledHandler), CreateParameters("TEST-contract-token"));
    var cancellationPropagated = false;
    try
    {
        await canceledClient.CrearOrdenAsync(Guid.NewGuid(), "attempt-reference", 125.50m);
    }
    catch (OperationCanceledException)
    {
        cancellationPropagated = true;
    }
    Check(cancellationPropagated && canceledHandler.RequestCount == 0,
        "identity preflight cancellation propagates without a provider operation");
}

List<ValidationResult> Validate<TRequest>(TRequest request) where TRequest : class
{
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(
        request,
        new ValidationContext(request),
        results,
        validateAllProperties: true);
    return results;
}

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

var validRequest = new IniciarIntentoCompraRequestDto
{
    Tramos =
    [
        new() { Orden = 1, ServicioId = "opaque" },
        new() { Orden = 2, ServicioId = "arbitrary id" }
    ]
};
Check(Validate(validRequest).Count == 0, "valid selections pass without ID restrictions");
Check(Validate(new IniciarIntentoCompraRequestDto { Tramos = [] }).Any(), "empty list is rejected");
Check(Validate(new IniciarIntentoCompraRequestDto { Tramos = null! }).Any(), "null list is rejected");

var omittedList = JsonSerializer.Deserialize<IniciarIntentoCompraRequestDto>("{}", jsonOptions)!;
var omittedListResults = Validate(omittedList);
Check(omittedListResults.Any(result => result.MemberNames.Contains("Tramos")), "omitted list is rejected at Tramos");

var nullList = JsonSerializer.Deserialize<IniciarIntentoCompraRequestDto>("""{"tramos":null}""", jsonOptions)!;
Check(Validate(nullList).Any(result => result.MemberNames.Contains("Tramos")), "JSON null list is rejected at Tramos");

var nullElement = JsonSerializer.Deserialize<IniciarIntentoCompraRequestDto>("""{"tramos":[null]}""", jsonOptions)!;
Check(Validate(nullElement).Any(result => result.MemberNames.Contains("Tramos[0]")), "JSON null element reports Tramos[0]");

var mixedNull = new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = 1, ServicioId = "ok" }, null!]
};
Check(Validate(mixedNull).Any(result => result.MemberNames.Contains("Tramos[1]")), "mixed null element reports Tramos[1]");

var firstInvalid = new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = 0, ServicioId = "" }]
};
Check(Validate(firstInvalid).Any(result => result.MemberNames.Contains("Tramos[0].Orden")), "first element order error reports its indexed path");
Check(Validate(firstInvalid).Any(result => result.MemberNames.Contains("Tramos[0].ServicioId")), "first element service error reports its indexed path");

var secondInvalid = new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = 1, ServicioId = "ok" }, new() { Orden = 0, ServicioId = "" }]
};
var secondResults = Validate(secondInvalid);
Check(secondResults.Any(result => result.MemberNames.Contains("Tramos[1].Orden")), "second element order error reports its indexed path");
Check(secondResults.Any(result => result.MemberNames.Contains("Tramos[1].ServicioId")), "second element service error reports its indexed path");

var multipleInvalid = new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = 0, ServicioId = "" }, new() { Orden = -2, ServicioId = " " }]
};
var multipleResults = Validate(multipleInvalid);
var expectedMembers = new[]
{
    "Tramos[0].Orden",
    "Tramos[0].ServicioId",
    "Tramos[1].Orden",
    "Tramos[1].ServicioId"
};
Check(expectedMembers.All(member => multipleResults.Any(result => result.MemberNames.Contains(member))), "all invalid fields retain their indexed paths across children");

Check(Validate(new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = 0, ServicioId = "ok" }]
}).Any(), "zero order is rejected");
Check(Validate(new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = -1, ServicioId = "ok" }]
}).Any(), "negative order is rejected");
Check(Validate(new IniciarIntentoCompraRequestDto
{
    Tramos = [new() { Orden = int.MaxValue, ServicioId = "ok" }]
}).Count == 0, "maximum positive order is accepted");

foreach (var serviceId in new string?[] { null, "", "   " })
{
    var request = new IniciarIntentoCompraRequestDto
    {
        Tramos = [new() { Orden = 1, ServicioId = serviceId! }]
    };
    Check(Validate(request).Any(), $"missing or whitespace service ID is rejected ({serviceId ?? "null"})");
}

var originalFirst = new SeleccionTramoCompraDto { Orden = 1, ServicioId = "opaque-one" };
var originalSecond = new SeleccionTramoCompraDto { Orden = 0, ServicioId = "" };
var originalList = new List<SeleccionTramoCompraDto> { originalFirst, originalSecond };
var immutableRequest = new IniciarIntentoCompraRequestDto { Tramos = originalList };
Validate(immutableRequest);
Check(ReferenceEquals(immutableRequest.Tramos, originalList), "validation preserves the original list reference");
Check(ReferenceEquals(immutableRequest.Tramos[0], originalFirst) && ReferenceEquals(immutableRequest.Tramos[1], originalSecond), "validation preserves child object references");
Check(originalFirst.Orden == 1 && originalFirst.ServicioId == "opaque-one", "validation preserves valid child values");
Check(originalSecond.Orden == 0 && originalSecond.ServicioId == "", "validation preserves invalid child values");

var manySelections = new IniciarIntentoCompraRequestDto
{
    Tramos = Enumerable.Range(1, 3)
        .Select(index => new SeleccionTramoCompraDto { Orden = index, ServicioId = $"opaque-{index}" })
        .ToList()
};
Check(Validate(manySelections).Count == 0 && manySelections.Tramos.Count == 3, "more than two arbitrary selections remain valid");

var roundtrip = JsonSerializer.Deserialize<IniciarIntentoCompraRequestDto>(
    """{"tramos":[{"orden":1,"servicioId":"opaque-123"}]}""",
    jsonOptions)!;
Check(roundtrip.Tramos[0].ServicioId == "opaque-123", "opaque string IDs deserialize unchanged");

var response = new IniciarIntentoCompraResponseDto
{
    IntentoId = "opaque-intent",
    Itinerario = new ItinerarioDto { PrecioTotal = 17.5m },
    HoldVenceEn = DateTime.SpecifyKind(new DateTime(2026, 11, 16, 15, 30, 0), DateTimeKind.Utc)
};
Check(response.Itinerario is ItinerarioDto
    && response.IntentoId == "opaque-intent"
    && response.Itinerario.PrecioTotal == 17.5m, "response transports opaque ID and reused itinerary");

var responseJson = JsonSerializer.Serialize(response, jsonOptions);
var responseElement = JsonDocument.Parse(responseJson).RootElement;
Check(responseElement.GetProperty("holdVenceEn").GetString() == "2026-11-16T15:30:00Z", "response serializes an explicitly UTC sample unchanged");
Check(new IniciarIntentoCompraResponseDto().HoldVenceEn == default, "response does not invent a hold expiry default");

var route = typeof(IntentosCompraController).GetCustomAttributes(typeof(RouteAttribute), inherit: true)
    .Cast<RouteAttribute>().Single();
var authorize = typeof(IntentosCompraController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
    .Cast<AuthorizeAttribute>().Single();
Check(route.Template == "api/intentos-compra", "attempt controller uses the documented route");
Check(authorize.Roles == "Pasajero", "all attempt actions require the passenger role");
Check(typeof(IntentosCompraController).GetMethod(nameof(IntentosCompraController.Crear))!
    .GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Any(), "create action is HTTP POST");
Check(typeof(IntentosCompraController).GetMethod(nameof(IntentosCompraController.Consultar))!
    .GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template == "{intentoId}", "read action is HTTP GET by attempt ID");

var attemptResponse = new IniciarIntentoCompraResponseDto { IntentoId = "opaque-attempt" };
var fakeService = new FakeIntentosCompraService { CreateResponse = attemptResponse, ReadResponse = attemptResponse };
var controller = CreateController(fakeService, "passenger-public-id");
using var cancellationSource = new CancellationTokenSource();
var createResult = await controller.Crear(validRequest, cancellationSource.Token);
Check(createResult.Result is CreatedAtActionResult createdResult
    && createdResult.ActionName == nameof(IntentosCompraController.Consultar)
    && createdResult.RouteValues?.GetValueOrDefault("intentoId")?.ToString() == "opaque-attempt"
    && ReferenceEquals(createdResult.Value, attemptResponse), "create returns 201 with a location for the created opaque attempt");
Check(fakeService.CreatePassengerId == "passenger-public-id" && ReferenceEquals(fakeService.CreateRequest, validRequest), "create uses the authenticated public identity and typed request");
Check(fakeService.CreateCancellationToken == cancellationSource.Token, "create propagates the request cancellation token");

var readResult = await controller.Consultar("opaque-attempt", cancellationSource.Token);
Check(readResult.Result is OkObjectResult readOk && ReferenceEquals(readOk.Value, attemptResponse), "owner-scoped read returns the service response");
Check(fakeService.ReadPassengerId == "passenger-public-id" && fakeService.ReadAttemptId == "opaque-attempt", "read scopes lookup to authenticated passenger and attempt ID");
Check(fakeService.ReadCancellationToken == cancellationSource.Token, "read propagates the request cancellation token");

var noIdentityController = CreateController(fakeService, null);
var createCallsBeforeMissingIdentity = fakeService.CreateCalls;
Check((await noIdentityController.Crear(validRequest)).Result is UnauthorizedResult, "create rejects missing authenticated identity");
Check((await noIdentityController.Consultar("opaque-attempt")).Result is UnauthorizedResult, "read rejects missing authenticated identity");
Check(fakeService.CreateCalls == createCallsBeforeMissingIdentity, "missing identity never reaches the service");
Check((await controller.Crear(null!)).Result is BadRequestObjectResult, "direct null request is rejected as bad input");

fakeService.CreateException = new ArgumentException("La solicitud no es válida.");
var invalidCreate = await controller.Crear(validRequest);
Check(invalidCreate.Result is BadRequestObjectResult invalidBody && Equals(invalidBody.Value, "La solicitud no es válida."), "service validation errors map to 400");
fakeService.CreateException = new KeyNotFoundException("Missing service");
Check((await controller.Crear(validRequest)).Result is NotFoundResult, "missing selected service maps to 404");
fakeService.CreateException = new AsientoNoDisponibleException("internal seat state should stay private");
var unavailableCreate = await controller.Crear(validRequest);
Check(unavailableCreate.Result is ConflictObjectResult seatConflict
    && Equals(seatConflict.Value, "No se pudo reservar un asiento para el itinerario solicitado.")
    && !seatConflict.Value!.ToString()!.Contains("internal seat state", StringComparison.Ordinal), "seat unavailability maps to a sanitized 409");
fakeService.CreateException = new InvalidOperationException("provider credential secret");
var unexpectedCreate = await controller.Crear(validRequest);
Check(unexpectedCreate.Result is ObjectResult createFailure
    && createFailure.StatusCode == StatusCodes.Status500InternalServerError
    && Equals(createFailure.Value, "Error interno del servidor")
    && !createFailure.Value!.ToString()!.Contains("provider credential", StringComparison.Ordinal), "unexpected create failure is sanitized as 500");

fakeService.CreateException = null;
fakeService.ReadException = new KeyNotFoundException("Foreign or missing attempt");
Check((await controller.Consultar("opaque-attempt")).Result is NotFoundResult, "missing or foreign attempt maps to 404");
fakeService.ReadException = new ArgumentException("Attempt ID is required.");
Check((await controller.Consultar(" ")).Result is BadRequestObjectResult, "invalid attempt ID maps to 400");
fakeService.ReadException = new InvalidOperationException("provider credential secret");
var unexpectedRead = await controller.Consultar("opaque-attempt");
Check(unexpectedRead.Result is ObjectResult readFailure
    && readFailure.StatusCode == StatusCodes.Status500InternalServerError
    && Equals(readFailure.Value, "Error interno del servidor")
    && !readFailure.Value!.ToString()!.Contains("provider credential", StringComparison.Ordinal), "unexpected read failure is sanitized as 500");
fakeService.ReadException = null;

var originalQuote = CreateQuote(100m, 250m);
var quoteExpiry = DateTime.SpecifyKind(new DateTime(2026, 11, 16, 15, 0, 0), DateTimeKind.Utc);
var quoteAttempt = new IniciarIntentoCompraResponseDto
{
    IntentoId = "IC-123",
    Itinerario = originalQuote,
    HoldVenceEn = quoteExpiry
};
var quoteAttempts = new FakeIntentosCompraService { ReadResponse = quoteAttempt };
var quoteItineraries = new FakeItinerariosService { Offers = [CreateQuote(100m, 250m)] };
var quoteClock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 14, 0, 0, TimeSpan.Zero));
var quotes = new CotizacionesCompraService(quoteAttempts, quoteItineraries, quoteClock);
var unchangedQuote = await quotes.CotizarAsync("passenger", "IC-123");
Check(!unchangedQuote.CambioPrecio && unchangedQuote.CotizacionOriginal.PrecioTotal == 350m
    && unchangedQuote.CotizacionActual.PrecioTotal == 350m && unchangedQuote.HoldVenceEn == quoteExpiry,
    "same-price requote returns original and current quotes without moving the hold deadline");
Check(quoteAttempts.ReadPassengerId == "passenger" && quoteAttempts.ReadAttemptId == "IC-123"
    && quoteItineraries.SearchCount == 1 && quoteItineraries.LastDate == new DateOnly(2026, 11, 16),
    "requote uses owner-scoped attempt data and the local travel date");
using var requoteCancellation = new CancellationTokenSource();
await quotes.CotizarAsync("passenger", "IC-123", requoteCancellation.Token);
Check(quoteAttempts.ReadCancellationToken == requoteCancellation.Token,
    "requote forwards cancellation to the owner-scoped attempt read");
var repeatQuote = await quotes.CotizarAsync("passenger", "IC-123");
Check(unchangedQuote.HuellaCotizacion == repeatQuote.HuellaCotizacion
    && unchangedQuote.HuellaCotizacion.Length == 64, "same server quote produces a deterministic SHA-256 hex fingerprint");
var acceptedFingerprint = new IniciarPagoRequestDto { HuellaCotizacionAceptada = new string('a', 64) };
Check(Validate(acceptedFingerprint).Count == 0,
    "payment start accepts a 64-character hexadecimal server-quote fingerprint");
acceptedFingerprint.HuellaCotizacionAceptada = new string('A', 64);
Check(Validate(acceptedFingerprint).Count == 0,
    "payment start accepts uppercase hexadecimal quote fingerprints");
foreach (var invalidFingerprint in new[] { "", new string('a', 63), new string('a', 65), new string('g', 64) })
{
    acceptedFingerprint.HuellaCotizacionAceptada = invalidFingerprint;
    Check(Validate(acceptedFingerprint).Count > 0,
        $"payment start rejects invalid accepted-quote fingerprint '{invalidFingerprint}'");
}
Check(typeof(IniciarPagoRequestDto).GetProperties().Select(property => property.Name)
        .SequenceEqual(["HuellaCotizacionAceptada"]),
    "payment request accepts only quote acceptance and no client amount, card, or passenger identity");
foreach (var (current, currentPaid, incoming, incomingPaid, expected) in new[]
{
    (EstadoPago.Desconocido, 0m, EstadoPago.Pendiente, 0m, EstadoPago.Pendiente),
    (EstadoPago.Desconocido, 0m, EstadoPago.Rechazado, 0m, EstadoPago.Rechazado),
    (EstadoPago.Pendiente, 0m, EstadoPago.Rechazado, 0m, EstadoPago.Rechazado),
    (EstadoPago.Desconocido, 0m, EstadoPago.Rechazado, 1m, EstadoPago.Desconocido),
    (EstadoPago.Pendiente, 0m, EstadoPago.Rechazado, 1m, EstadoPago.Pendiente),
    (EstadoPago.Desconocido, 1m, EstadoPago.Rechazado, 0m, EstadoPago.Desconocido),
    (EstadoPago.Pendiente, 1m, EstadoPago.Rechazado, 0m, EstadoPago.Pendiente),
    (EstadoPago.Aprobado, 0m, EstadoPago.Rechazado, 0m, EstadoPago.Aprobado),
    (EstadoPago.Rechazado, 0m, EstadoPago.Pendiente, 0m, EstadoPago.Rechazado),
    (EstadoPago.Devuelto, 0m, EstadoPago.Pendiente, 0m, EstadoPago.Devuelto),
    (EstadoPago.DevolucionPendiente, 0m, EstadoPago.Rechazado, 0m, EstadoPago.DevolucionPendiente)
})
{
    var order = new OrdenCheckoutMercadoPago
    {
        Id = "order-state",
        ReferenciaExterna = "P-state",
        Importe = 100m,
        ImportePagado = incomingPaid,
        Moneda = "UYU",
        Estado = incoming
    };
    Check(ReglasInicioPago.EstadoTrasCrearOrden(current, currentPaid, order) == expected,
        $"checkout state transition {current} + {incoming} paid locally/provider {currentPaid}/{incomingPaid} => {expected}");
}

var paymentNow = new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 14, 0, 0, TimeSpan.Zero));
var paymentQuoteService = new FakeCotizacionesCompraService { Response = unchangedQuote };
var successfulPaymentRepository = new FakePagosRepository();
var successfulGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = new OrdenCheckoutMercadoPago
    {
        Id = "order-1",
        ReferenciaExterna = "P-1",
        Importe = 350m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1"
    }
};
successfulGateway.BeforeCreate = () => Check(successfulPaymentRepository.Current?.Pago.Estado == EstadoPago.Desconocido,
    "checkout gateway is called only after an unknown payment reservation is durable");
var successfulPaymentService = CreatePaymentService(
    successfulPaymentRepository, paymentQuoteService, successfulGateway, paymentNow);
var paymentRequest = new IniciarPagoRequestDto { HuellaCotizacionAceptada = unchangedQuote.HuellaCotizacion };
var startedPayment = await successfulPaymentService.IniciarAsync("passenger", "IC-123", paymentRequest);
Check(successfulPaymentRepository.ReserveCalls == 1 && successfulGateway.CreateCalls == 1
    && successfulPaymentRepository.Current?.Pago.Estado == EstadoPago.Pendiente,
    "fresh accepted quote reserves one payment and records the provider order");
Check(successfulGateway.LastAmount == unchangedQuote.CotizacionActual.PrecioTotal
    && successfulGateway.LastReference == successfulPaymentRepository.Current!.Pago.ReferenciaExterna
    && successfulGateway.LastIdempotencyKey == successfulPaymentRepository.Current.Pago.ClaveCreacion,
    "checkout uses the server quote amount, the durable creation GUID as external reference and the idempotency key");
Check(successfulGateway.LastReference == successfulPaymentRepository.Current!.Pago.ClaveCreacion.ToString("D")
    && successfulGateway.LastReference != successfulPaymentRepository.Current.Pago.Id
    && !successfulGateway.LastReference!.StartsWith("PG-", StringComparison.Ordinal)
    && Guid.TryParse(successfulGateway.LastReference, out _),
    "external_reference sent to the provider is the globally unique creation GUID, not the local PG-n counter");
Check(startedPayment.PagoId == successfulPaymentRepository.Current!.Pago.Id
    && startedPayment.IntentoId == "IC-123" && startedPayment.Importe == 350m
    && startedPayment.Moneda == "UYU" && startedPayment.Estado == EstadoPago.Pendiente
    && startedPayment.CheckoutUrl == successfulGateway.CreateResponse.CheckoutUrl
    && startedPayment.HoldVenceEn == unchangedQuote.HoldVenceEn
    && !typeof(PagoResponseDto).GetProperties().Any(property => property.Name.Contains("Compra", StringComparison.Ordinal)
        || property.Name.Contains("Ticket", StringComparison.Ordinal)),
    "payment response exposes payment state and original hold without purchase or ticket issuance fields");

var changedServerQuote = new CotizarCompraResponseDto
{
    IntentoId = unchangedQuote.IntentoId,
    CotizacionOriginal = unchangedQuote.CotizacionOriginal,
    CotizacionActual = unchangedQuote.CotizacionActual,
    CambioPrecio = true,
    HuellaCotizacion = new string('B', 64),
    HoldVenceEn = unchangedQuote.HoldVenceEn
};
var staleQuoteRepository = new FakePagosRepository();
var staleQuoteGateway = new FakeMercadoPagoCheckoutClient();
var staleQuoteService = CreatePaymentService(staleQuoteRepository,
    new FakeCotizacionesCompraService { Response = changedServerQuote }, staleQuoteGateway, paymentNow);
var staleQuoteException = await CaptureExceptionAsync<CotizacionModificadaException>(
    () => staleQuoteService.IniciarAsync("passenger", "IC-123", paymentRequest));
Check(ReferenceEquals(staleQuoteException.Cotizacion, changedServerQuote)
    && staleQuoteRepository.ReserveCalls == 0 && staleQuoteGateway.CreateCalls == 0,
    "stale accepted quote returns the authoritative current quote without reservation or checkout");

foreach (var blockedState in new[] { EstadoPago.Pendiente, EstadoPago.Desconocido })
{
    var blockedRepository = new FakePagosRepository(CreatePaymentRecord("P-existing", blockedState,
        paymentNow.GetUtcNow().UtcDateTime.AddMinutes(-1), "https://www.mercadopago.com.uy/checkout/existing"));
    var blockedQuoteService = new FakeCotizacionesCompraService { Response = unchangedQuote };
    var blockedGateway = new FakeMercadoPagoCheckoutClient();
    var blockedPayment = await CreatePaymentService(blockedRepository, blockedQuoteService, blockedGateway, paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest);
    Check(blockedPayment.PagoId == "P-existing" && blockedPayment.Estado == blockedState
        && blockedPayment.CheckoutUrl is null && blockedQuoteService.Calls == 0
        && blockedRepository.ReserveCalls == 0 && blockedGateway.CreateCalls == 0,
        $"existing {blockedState} payment blocks another checkout even after hold expiry");
}

var rejectedRepository = new FakePagosRepository(CreatePaymentRecord("P-rejected", EstadoPago.Rechazado,
    quoteExpiry, null));
var rejectedGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = new OrdenCheckoutMercadoPago
    {
        Id = "order-retry",
        ReferenciaExterna = "P-2",
        Importe = 350m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-retry"
    }
};
var rejectedRetry = await CreatePaymentService(rejectedRepository, paymentQuoteService, rejectedGateway, paymentNow)
    .IniciarAsync("passenger", "IC-123", paymentRequest);
Check(rejectedRepository.ReserveCalls == 1 && rejectedGateway.CreateCalls == 1
    && rejectedRetry.PagoId == rejectedRepository.Current!.Pago.Id && rejectedRetry.PagoId != "P-rejected"
    && rejectedRepository.Current!.HoldVenceEn == quoteExpiry,
    "definitely rejected payment permits a new checkout only inside the original live hold");

var rejectedOrderRepository = new FakePagosRepository();
var rejectedOrderGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = new OrdenCheckoutMercadoPago
    {
        Id = "order-rejected",
        ReferenciaExterna = "P-1",
        Importe = 350m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Rechazado,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/rejected"
    }
};
var rejectedOrderService = CreatePaymentService(rejectedOrderRepository, paymentQuoteService, rejectedOrderGateway, paymentNow);
var firstRejectedOrder = await rejectedOrderService.IniciarAsync("passenger", "IC-123", paymentRequest);
Check(firstRejectedOrder.Estado == EstadoPago.Rechazado && rejectedOrderRepository.Current!.Pago.Estado == EstadoPago.Rechazado,
    "valid zero-paid rejected provider order is persisted as rejected");
var firstRejectedPayment = rejectedOrderRepository.Current!.Pago;
var rejectedOrderRetryGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = new OrdenCheckoutMercadoPago
    {
        Id = "order-retry-after-rejection",
        ReferenciaExterna = "P-2",
        Importe = 350m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/retry"
    }
};
var secondPayment = await CreatePaymentService(rejectedOrderRepository, paymentQuoteService,
        rejectedOrderRetryGateway, paymentNow)
    .IniciarAsync("passenger", "IC-123", paymentRequest);
Check(rejectedOrderRepository.NewReservationCount == 2
    && secondPayment.Estado == EstadoPago.Pendiente
    && secondPayment.PagoId != firstRejectedPayment.Id
    && rejectedOrderRepository.History.Count == 2
    && rejectedOrderRepository.History[0].Estado == EstadoPago.Rechazado
    && rejectedOrderRepository.History[0].ClaveCreacion != rejectedOrderRepository.History[1].ClaveCreacion
    && rejectedOrderRepository.Current!.HoldVenceEn == quoteExpiry,
    "persisted rejection allows a new payment key while retaining original payment and hold expiry");

// MVP-004 E2E: real Mercado Pago TEST response shape (no status_detail, total_paid_amount or live_mode; string amounts).
var realShapeCheckoutUrl = "https://www.mercadopago.com.uy/checkout/v1/redirect?pref_id=123456789-aaaa-bbbb";
var realShapePostCalls = 0;
var realShapeRepository = new FakePagosRepository();
var realShapeHandler = CreateRealShapeHandler(() => realShapePostCalls++, realShapeCheckoutUrl,
    () => realShapeRepository.Current!.Pago.ReferenciaExterna);
var realShapeService = CreatePaymentService(realShapeRepository, paymentQuoteService,
    CreateCheckoutClient(realShapeHandler), paymentNow);
var realShapePayment = await realShapeService.IniciarAsync("passenger", "IC-123", paymentRequest);
Check(realShapePayment.Estado == EstadoPago.Pendiente && realShapePayment.CheckoutUrl == realShapeCheckoutUrl
    && realShapeRepository.Current!.Pago.Estado == EstadoPago.Pendiente && realShapePostCalls == 1,
    "real-shape created order (no status_detail) is pending and exposes the checkout URL");
Check((await CreatePaymentController(new FakePagosService { Response = realShapePayment }, "passenger-public-id")
        .Iniciar("IC-123", paymentRequest)).Result is OkObjectResult,
    "real-shape created order is answered with 200, not 202");
var realShapeReference = realShapeRepository.Current.Pago.ReferenciaExterna;
var realShapeGet = await CreateCheckoutClient(realShapeHandler).ConsultarOrdenAsync("ORD-REAL-1", realShapeReference, 350m);
Check(realShapeGet is { Estado: EstadoPago.Pendiente, ImportePagado: 0m, CheckoutUrl: not null },
    "real-shape get-by-id order is pending, unpaid and keeps its checkout URL");
var realShapeSearch = await CreateCheckoutClient(realShapeHandler).BuscarOrdenAsync(realShapeReference,
    paymentNow.GetUtcNow().UtcDateTime.AddMinutes(-5), paymentNow.GetUtcNow().UtcDateTime.AddMinutes(1), 350m);
Check(realShapeSearch is { Estado: EstadoPago.Pendiente, CheckoutUrl: not null },
    "real-shape search (no status_detail/checkout_url) resolves through get-by-id to a pending order with URL");
foreach (var (status, detail) in new (string, string?)[]
{
    ("processed", null), ("failed", null), ("canceled", null), ("processing", null),
    ("action_required", null), ("created", "unexpected_detail")
})
{
    var detailJson = detail is null ? "" : $",\"status_detail\":\"{detail}\"";
    EstadoPago? mapped = null;
    try
    {
        mapped = (await CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK,
                $"{{\"id\":\"order-1\",\"external_reference\":\"payment-123\",\"total_amount\":\"125.50\",\"currency\":\"UYU\",\"status\":\"{status}\"{detailJson}}}")))
            .ConsultarOrdenAsync("order-1", "payment-123", 125.50m)).Estado;
    }
    catch (InvalidOperationException)
    {
    }
    Check(mapped is null or EstadoPago.Desconocido, $"status {status}/{detail ?? "missing"} is not loosened to pending");
}

// Fallos definitivos previos al envío: el pago queda rechazado y se puede reintentar dentro del hold original.
const string validTestIdentity = "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}";
foreach (var (wrongName, wrongBody) in new (string, string)[]
{
    ("seller mismatch", "{\"id\":111,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}"),
    ("site mismatch", "{\"id\":987654321,\"site_id\":\"OTHER\",\"tags\":[\"test_user\"]}"),
    ("missing test_user tag", "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"normal\"]}")
})
{
    var preRepository = new FakePagosRepository();
    var prePosts = 0;
    var preHandler = CreateRealShapeHandler(() => prePosts++, realShapeCheckoutUrl,
        () => preRepository.Current!.Pago.ReferenciaExterna);
    preHandler.IdentityResponseFactory = (_, _) => JsonResponse(HttpStatusCode.OK, wrongBody);
    var preService = CreatePaymentService(preRepository, paymentQuoteService, CreateCheckoutClient(preHandler), paymentNow);
    var preError = await CaptureExceptionAsync<FalloPreEnvioProveedorException>(
        () => preService.IniciarAsync("passenger", "IC-123", paymentRequest));
    var rejectedPayment = preRepository.Current!.Pago;
    Check(prePosts == 0 && rejectedPayment.Estado == EstadoPago.Rechazado && rejectedPayment.ImportePagado == 0m
        && rejectedPayment.OrdenProveedorId is null && preRepository.RejectWithoutOrderCalls == 1
        && !preError.Message.Contains("987654321", StringComparison.Ordinal),
        $"identity {wrongName} before create leaves the payment rejected, unpaid, without order, with a sanitized error");
    preHandler.IdentityResponseFactory = (_, _) => JsonResponse(HttpStatusCode.OK, validTestIdentity);
    var preRetry = await preService.IniciarAsync("passenger", "IC-123", paymentRequest);
    Check(preRetry.Estado == EstadoPago.Pendiente && preRetry.PagoId != rejectedPayment.Id && prePosts == 1
        && preRepository.Current!.HoldVenceEn == quoteExpiry,
        $"retry after {wrongName} is allowed within the original hold");
}
foreach (var (preName, preFailure) in new (string, Exception)[]
{
    ("typed pre-send failure", new FalloPreEnvioProveedorException()),
    ("invalid idempotency/argument", new ArgumentException("clave inválida"))
})
{
    var preRepository = new FakePagosRepository();
    var preGateway = new FakeMercadoPagoCheckoutClient { CreateException = preFailure };
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreatePaymentService(preRepository, paymentQuoteService, preGateway, paymentNow)
            .IniciarAsync("passenger", "IC-123", paymentRequest),
        $"{preName} surfaces an error instead of 202");
    Check(preRepository.Current!.Pago.Estado == EstadoPago.Rechazado && preRepository.LastOrder is null,
        $"{preName} leaves the payment rejected");
}
var identityOutageRepository = new FakePagosRepository();
var identityOutageHandler = CreateRealShapeHandler(() => { }, realShapeCheckoutUrl,
    () => identityOutageRepository.Current!.Pago.ReferenciaExterna);
identityOutageHandler.IdentityResponseFactory = (_, _) => JsonResponse(HttpStatusCode.InternalServerError, "secret");
await CheckThrowsAsync<FalloPreEnvioProveedorException>(
    () => CreatePaymentService(identityOutageRepository, paymentQuoteService, CreateCheckoutClient(identityOutageHandler), paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest),
    "/users/me HTTP error before create is a definitive pre-send failure");
Check(identityOutageRepository.Current!.Pago.Estado == EstadoPago.Rechazado,
    "/users/me HTTP error leaves the payment rejected");

// Fallos posteriores al envío del POST siguen siendo ambiguos.
foreach (var (postName, postFailure) in new (string, Func<HttpResponseMessage>)[]
{
    ("POST 500", () => JsonResponse(HttpStatusCode.InternalServerError, "boom")),
    ("POST network error", () => throw new HttpRequestException("reset")),
    ("POST timeout", () => throw new TaskCanceledException("timeout", new TimeoutException())),
    ("POST unparseable body", () => JsonResponse(HttpStatusCode.Created, "not-json"))
})
{
    var ambiguousRepository = new FakePagosRepository();
    var ambiguousPosts = 0;
    var ambiguousHandler = new MockHttpMessageHandler((request, _) =>
    {
        ambiguousPosts++;
        return postFailure();
    });
    var ambiguousPayment = await CreatePaymentService(ambiguousRepository, paymentQuoteService,
            CreateCheckoutClient(ambiguousHandler), paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest);
    Check(ambiguousPosts == 1 && ambiguousPayment.Estado == EstadoPago.Desconocido
        && ambiguousRepository.Current!.Pago.Estado == EstadoPago.Desconocido
        && ambiguousRepository.RejectWithoutOrderCalls == 0,
        $"{postName} after the order POST was sent stays unknown and is never rejected");
}

var expiredAcceptedQuote = new CotizarCompraResponseDto
{
    IntentoId = unchangedQuote.IntentoId,
    CotizacionOriginal = unchangedQuote.CotizacionOriginal,
    CotizacionActual = unchangedQuote.CotizacionActual,
    CambioPrecio = unchangedQuote.CambioPrecio,
    HuellaCotizacion = unchangedQuote.HuellaCotizacion,
    HoldVenceEn = paymentNow.GetUtcNow().UtcDateTime
};
var expiredRepository = new FakePagosRepository();
var expiredGateway = new FakeMercadoPagoCheckoutClient();
var expiredPaymentException = await CaptureExceptionAsync<CotizacionNoDisponibleException>(
    () => CreatePaymentService(expiredRepository,
            new FakeCotizacionesCompraService { Response = expiredAcceptedQuote }, expiredGateway, paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest));
Check(expiredPaymentException is not null && expiredRepository.ReserveCalls == 0 && expiredGateway.CreateCalls == 0,
    "expired original hold rejects payment before creating a reservation or checkout");

foreach (var failure in new Exception[]
{
    new HttpRequestException("provider unavailable"),
    new TimeoutException("provider timeout")
})
{
    var uncertainRepository = new FakePagosRepository();
    var uncertainGateway = new FakeMercadoPagoCheckoutClient { CreateException = failure };
    var uncertainResponse = await CreatePaymentService(uncertainRepository, paymentQuoteService, uncertainGateway, paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest);
    Check(uncertainResponse.Estado == EstadoPago.Desconocido
        && uncertainResponse.PagoId == uncertainRepository.Current!.Pago.Id
        && uncertainRepository.Current.Pago.Estado == EstadoPago.Desconocido
        && uncertainRepository.LastOrder is null && uncertainRepository.ReserveCalls == 1,
        $"checkout {failure.GetType().Name} retains the same reserved payment as unknown, never rejected");
}
var invalidResponseRepository = new FakePagosRepository();
var invalidResponseGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = new OrdenCheckoutMercadoPago
    {
        Id = "order-invalid",
        ReferenciaExterna = "P-1",
        Importe = 351m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/invalid"
    }
};
var invalidProviderResponse = await CreatePaymentService(invalidResponseRepository, paymentQuoteService,
    invalidResponseGateway, paymentNow).IniciarAsync("passenger", "IC-123", paymentRequest);
Check(invalidProviderResponse.Estado == EstadoPago.Desconocido
    && invalidProviderResponse.PagoId == invalidResponseRepository.Current!.Pago.Id
    && invalidResponseRepository.LastOrder is null,
    "mismatched checkout response leaves the durable payment unknown");

var wrongReferenceRepository = new FakePagosRepository();
var wrongReferenceGateway = new FakeMercadoPagoCheckoutClient { ReferenceOverride = "PG-1" };
var wrongReferenceResponse = await CreatePaymentService(wrongReferenceRepository, paymentQuoteService,
    wrongReferenceGateway, paymentNow).IniciarAsync("passenger", "IC-123", paymentRequest);
Check(wrongReferenceResponse.Estado == EstadoPago.Desconocido && wrongReferenceRepository.LastOrder is null,
    "provider order echoing the local PG id instead of the creation GUID is not recorded as the payment order");

// S3g (b): after the original hold expired, a definitively rejected payment cannot be retried.
var rejectedExpiredRepository = new FakePagosRepository(CreatePaymentRecord("P-rejected", EstadoPago.Rechazado,
    paymentNow.GetUtcNow().UtcDateTime.AddMinutes(-1), null));
var rejectedExpiredGateway = new FakeMercadoPagoCheckoutClient();
await CheckThrowsAsync<CotizacionNoDisponibleException>(
    () => CreatePaymentService(rejectedExpiredRepository,
            new FakeCotizacionesCompraService { Response = expiredAcceptedQuote }, rejectedExpiredGateway, paymentNow)
        .IniciarAsync("passenger", "IC-123", paymentRequest),
    "retry after definitive rejection is refused once the original hold expired");
Check(rejectedExpiredRepository.ReserveCalls == 0 && rejectedExpiredGateway.CreateCalls == 0
    && rejectedExpiredRepository.Current!.Pago.Id == "P-rejected",
    "expired-hold retry creates no new payment reservation or provider order");

var canceledRepository = new FakePagosRepository();
using (var afterReserveCancellation = new CancellationTokenSource())
{
    var canceledGateway = new FakeMercadoPagoCheckoutClient
    {
        BeforeCreate = afterReserveCancellation.Cancel
    };
    await CheckThrowsAsync<OperationCanceledException>(
        () => CreatePaymentService(canceledRepository, paymentQuoteService, canceledGateway, paymentNow)
            .IniciarAsync("passenger", "IC-123", paymentRequest, afterReserveCancellation.Token),
        "caller cancellation propagates after a durable payment reservation");
    Check(canceledRepository.ReserveCalls == 1 && canceledRepository.Current?.Pago.Estado == EstadoPago.Desconocido,
        "caller cancellation leaves the reserved payment unknown for later reconciliation");
}

var noCurrentRepository = new FakePagosRepository();
Check(await CreatePaymentService(noCurrentRepository, paymentQuoteService, new FakeMercadoPagoCheckoutClient(), paymentNow)
        .ConsultarActualAsync("passenger", "IC-123") is null,
    "current-payment query returns null when no payment exists");
var foreignPaymentRepository = new FakePagosRepository { ConsultException = new KeyNotFoundException("Foreign or missing attempt") };
var foreignQuoteService = new FakeCotizacionesCompraService { Response = unchangedQuote };
var foreignGateway = new FakeMercadoPagoCheckoutClient();
await CheckThrowsAsync<KeyNotFoundException>(
    () => CreatePaymentService(foreignPaymentRepository, foreignQuoteService, foreignGateway, paymentNow)
        .ConsultarActualAsync("passenger", "IC-foreign"),
    "current-payment query keeps a foreign attempt not found");
Check(foreignQuoteService.Calls == 0 && foreignGateway.CreateCalls == 0,
    "foreign current-payment lookup never requotes or reaches checkout");
foreach (var visibleState in new[]
{
    EstadoPago.Pendiente,
    EstadoPago.Desconocido,
    EstadoPago.Aprobado,
    EstadoPago.DevolucionPendiente,
    EstadoPago.Devuelto,
    EstadoPago.Rechazado
})
{
    var currentRepository = new FakePagosRepository(CreatePaymentRecord("P-current", visibleState,
        quoteExpiry, "https://www.mercadopago.com.uy/checkout/current"));
    var currentPayment = await CreatePaymentService(currentRepository, paymentQuoteService,
        new FakeMercadoPagoCheckoutClient(), paymentNow).ConsultarActualAsync("passenger", "IC-123");
    Check(currentPayment?.PagoId == "P-current" && currentPayment.Estado == visibleState
        && (currentPayment.CheckoutUrl is not null) == (visibleState == EstadoPago.Pendiente),
        $"current {visibleState} payment is returned while checkout URL requires a pending live hold");
}
var expiredCurrentRepository = new FakePagosRepository(CreatePaymentRecord("P-expired", EstadoPago.Pendiente,
    paymentNow.GetUtcNow().UtcDateTime, "https://www.mercadopago.com.uy/checkout/expired"));
var expiredCurrent = await CreatePaymentService(expiredCurrentRepository, paymentQuoteService,
    new FakeMercadoPagoCheckoutClient(), paymentNow).ConsultarActualAsync("passenger", "IC-123");
Check(expiredCurrent?.Estado == EstadoPago.Pendiente && expiredCurrent.CheckoutUrl is null,
    "current pending payment hides its checkout URL when the original hold has expired");

var parallelRepository = new FakePagosRepository();
var parallelGateway = new FakeMercadoPagoCheckoutClient
{
    CreateResponse = successfulGateway.CreateResponse
};
var parallelService = CreatePaymentService(parallelRepository, paymentQuoteService, parallelGateway, paymentNow);
var parallelResponses = await Task.WhenAll(
    parallelService.IniciarAsync("passenger", "IC-123", paymentRequest),
    parallelService.IniciarAsync("passenger", "IC-123", paymentRequest));
Check(parallelRepository.ReserveCalls >= 1 && parallelRepository.NewReservationCount == 1
    && parallelGateway.CreateCalls == 1 && parallelResponses.Select(response => response.PagoId).Distinct().Count() == 1,
    "fake reservation seam elects one new payment and one checkout across parallel same-attempt calls (not MySQL proof)");

quoteItineraries.Offers = [CreateQuote(100.0m, 250.0m)];
var scaleOneQuote = await quotes.CotizarAsync("passenger", "IC-123");
quoteItineraries.Offers = [CreateQuote(100.00m, 250.00m)];
var scaleTwoQuote = await quotes.CotizarAsync("passenger", "IC-123");
Check(!scaleOneQuote.CambioPrecio && !scaleTwoQuote.CambioPrecio
    && scaleOneQuote.HuellaCotizacion == scaleTwoQuote.HuellaCotizacion,
    "numerically identical money with different decimal scales has the same quote fingerprint");
quoteItineraries.Offers = [CreateQuote(100.001m, 249.999m)];
await CheckThrowsAsync<InvalidOperationException>(
    () => quotes.CotizarAsync("passenger", "IC-123"),
    "offered prices beyond two decimal places are rejected before fingerprinting");
quoteItineraries.Offers = [CreateQuote(100m, 250m)];
quoteAttempts.ReadResponse = new IniciarIntentoCompraResponseDto
{
    IntentoId = "IC-123",
    Itinerario = originalQuote,
    HoldVenceEn = quoteExpiry.AddMinutes(1)
};
var laterExpiryQuote = await quotes.CotizarAsync("passenger", "IC-123");
Check(laterExpiryQuote.HuellaCotizacion != unchangedQuote.HuellaCotizacion,
    "quote fingerprint is bound to the original hold expiry");
quoteAttempts.ReadResponse = quoteAttempt;

quoteItineraries.Offers = [CreateQuote(110m, 240m)];
var compensatedChange = await quotes.CotizarAsync("passenger", "IC-123");
Check(compensatedChange.CambioPrecio && compensatedChange.CotizacionActual.PrecioTotal == 350m
    && compensatedChange.CotizacionOriginal.Tramos[0].Importe == 100m
    && compensatedChange.HuellaCotizacion != unchangedQuote.HuellaCotizacion,
    "a per-leg price change is detected when the total is unchanged and the original snapshot is preserved");
quoteAttempts.ReadResponse = new IniciarIntentoCompraResponseDto
{
    IntentoId = "IC-124",
    Itinerario = originalQuote,
    HoldVenceEn = quoteExpiry
};
var otherAttemptQuote = await quotes.CotizarAsync("passenger", "IC-124");
Check(otherAttemptQuote.HuellaCotizacion != compensatedChange.HuellaCotizacion,
    "quote fingerprint is bound to the attempt identity");
quoteAttempts.ReadResponse = quoteAttempt;

quoteItineraries.Offers = [CreateQuote(101m, 250m)];
var singleLegChange = await quotes.CotizarAsync("passenger", "IC-123");
Check(singleLegChange.CambioPrecio && singleLegChange.HuellaCotizacion != unchangedQuote.HuellaCotizacion,
    "a current price change changes the passenger-acceptance fingerprint");
quoteItineraries.Offers = [];
var searchCountBeforeUnavailable = quoteItineraries.SearchCount;
await CheckThrowsAsync<CotizacionNoDisponibleException>(
    () => quotes.CotizarAsync("passenger", "IC-123"), "missing exact service sequence fails as unavailable");
Check(quoteItineraries.SearchCount == searchCountBeforeUnavailable + 1,
    "unavailable quote searches current offers without substituting a different itinerary");

await AssertQuoteSelectionMutationRejected("origin ID", offer => offer.Tramos[0].Origen.Id = "P0");
await AssertQuoteSelectionMutationRejected("destination ID", offer => offer.Tramos[1].Destino.Id = "P4");
await AssertQuoteSelectionMutationRejected("segment ID", offer => offer.Tramos[0].TramoId = "T9");
await AssertQuoteSelectionMutationRejected("segment departure", offer => offer.Tramos[0].Salida = offer.Tramos[0].Salida.AddMinutes(1));
await AssertQuoteSelectionMutationRejected("segment arrival", offer => offer.Tramos[1].Llegada = offer.Tramos[1].Llegada.AddMinutes(1));
await AssertQuoteSelectionMutationRejected("overall departure", offer => offer.Salida = offer.Salida.AddMinutes(1));
await AssertQuoteSelectionMutationRejected("overall arrival", offer => offer.Llegada = offer.Llegada.AddMinutes(1));
await AssertQuoteSelectionMutationRejected("seat policy", offer => offer.Tramos[0].PoliticaAsiento = PoliticaAsiento.SinAsiento);
await AssertQuoteSelectionMutationRejected("shifted order labels", offer =>
{
    offer.Tramos[0].Orden = 10;
    offer.Tramos[1].Orden = 20;
});

async Task AssertQuoteSelectionMutationRejected(string field, Action<ItinerarioDto> mutate)
{
    var offer = CloneQuote(originalQuote);
    mutate(offer);
    quoteItineraries.Offers = [offer];
    await CheckThrowsAsync<CotizacionNoDisponibleException>(
        () => quotes.CotizarAsync("passenger", "IC-123"),
        $"same service sequence with a changed protected {field} is unavailable");
}

quoteClock.SetUtcNow(new DateTimeOffset(quoteExpiry));
var searchCountAtExpiry = quoteItineraries.SearchCount;
await CheckThrowsAsync<CotizacionNoDisponibleException>(
    () => quotes.CotizarAsync("passenger", "IC-123"), "hold expiry equality fails closed");
Check(quoteItineraries.SearchCount == searchCountAtExpiry, "already-expired hold avoids current itinerary lookup");
quoteClock.SetUtcNow(new DateTimeOffset(2026, 11, 16, 14, 0, 0, TimeSpan.Zero));
quoteItineraries.BeforeReturn = () => quoteClock.SetUtcNow(new DateTimeOffset(quoteExpiry));
await CheckThrowsAsync<CotizacionNoDisponibleException>(
    () => quotes.CotizarAsync("passenger", "IC-123"), "hold expiring during itinerary lookup fails closed");
quoteItineraries.BeforeReturn = null;

quoteAttempts.ReadException = new KeyNotFoundException("Foreign or missing attempt");
var searchesBeforeForeign = quoteItineraries.SearchCount;
await CheckThrowsAsync<KeyNotFoundException>(
    () => quotes.CotizarAsync("passenger", "IC-foreign"), "foreign attempt remains not found");
Check(quoteItineraries.SearchCount == searchesBeforeForeign, "foreign attempt never triggers itinerary search");
quoteAttempts.ReadException = null;

var quoteControllerService = new FakeCotizacionesCompraService { Response = unchangedQuote };
var quoteController = CreateQuoteController(quoteControllerService, "passenger-public-id");
var quoteControllerResult = await quoteController.Cotizar("IC-123", cancellationSource.Token);
Check(quoteControllerResult.Result is OkObjectResult quoteOk && ReferenceEquals(quoteOk.Value, unchangedQuote)
    && quoteControllerService.PassengerId == "passenger-public-id"
    && quoteControllerService.AttemptId == "IC-123"
    && quoteControllerService.CancellationToken == cancellationSource.Token,
    "requote endpoint uses authenticated passenger identity and propagates cancellation");
Check(typeof(CotizacionesCompraController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
    .Cast<AuthorizeAttribute>().Single().Roles == "Pasajero"
    && typeof(CotizacionesCompraController).GetMethod(nameof(CotizacionesCompraController.Cotizar))!
        .GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template == "{intentoId}/cotizacion",
    "requote is a passenger-authorized GET on the attempt quote route");
var missingQuoteController = CreateQuoteController(quoteControllerService, null);
var quoteCallsBeforeNoIdentity = quoteControllerService.Calls;
Check((await missingQuoteController.Cotizar("IC-123")).Result is UnauthorizedResult
    && quoteControllerService.Calls == quoteCallsBeforeNoIdentity,
    "missing authenticated identity is rejected before requote service access");
quoteControllerService.Exception = new CotizacionNoDisponibleException("internal unavailable reason");
var unavailableQuoteResult = await quoteController.Cotizar("IC-123");
Check(unavailableQuoteResult.Result is ConflictObjectResult unavailableQuoteBody
    && !unavailableQuoteBody.Value!.ToString()!.Contains("internal unavailable reason", StringComparison.Ordinal),
    "expected quote expiry or unavailability maps to a sanitized 409");
quoteControllerService.Exception = new InvalidOperationException("sensitive internal detail");
var unexpectedQuoteResult = await quoteController.Cotizar("IC-123");
Check(unexpectedQuoteResult.Result is ObjectResult quoteFailure
    && quoteFailure.StatusCode == StatusCodes.Status500InternalServerError
    && !quoteFailure.Value!.ToString()!.Contains("sensitive internal detail", StringComparison.Ordinal),
    "unexpected requote errors are sanitized as 500");

var paymentHttpService = new FakePagosService
{
    Response = new PagoResponseDto
    {
        PagoId = "PG-9",
        IntentoId = "IC-123",
        Estado = EstadoPago.Pendiente,
        Importe = 350m,
        Moneda = "UYU",
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/example",
        HoldVenceEn = quoteExpiry
    }
};
var paymentController = CreatePaymentController(paymentHttpService, "passenger-public-id");
using var paymentCancellation = new CancellationTokenSource();
var paymentHttpResult = await paymentController.Iniciar("IC-123", paymentRequest, paymentCancellation.Token);
Check(paymentHttpResult.Result is OkObjectResult paymentOk && ReferenceEquals(paymentOk.Value, paymentHttpService.Response)
    && paymentHttpService.PassengerId == "passenger-public-id"
    && paymentHttpService.AttemptId == "IC-123"
    && ReferenceEquals(paymentHttpService.Request, paymentRequest)
    && paymentHttpService.CancellationToken == paymentCancellation.Token,
    "payment POST uses authenticated identity, attempt ID, typed acceptance, and cancellation");
Check(typeof(PagosController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
    .Cast<AuthorizeAttribute>().Single().Roles == "Pasajero"
    && typeof(PagosController).GetCustomAttributes(typeof(RouteAttribute), true)
        .Cast<RouteAttribute>().Single().Template == "api/intentos-compra/{intentoId}/pagos"
    && typeof(PagosController).GetMethod(nameof(PagosController.Iniciar))!
        .GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template is null
    && typeof(PagosController).GetMethod(nameof(PagosController.ConsultarActual))!
        .GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template == "actual",
    "payment actions expose the passenger-authorized POST and current-payment GET routes");
paymentHttpService.Response = new PagoResponseDto { PagoId = "PG-10", IntentoId = "IC-123", Estado = EstadoPago.Desconocido };
Check((await paymentController.Iniciar("IC-123", paymentRequest)).Result is ObjectResult unknownPayment
    && unknownPayment.StatusCode == StatusCodes.Status202Accepted,
    "unknown checkout outcome returns 202 without claiming payment confirmation");
paymentHttpService.CurrentResponse = paymentHttpService.Response;
Check((await paymentController.ConsultarActual("IC-123")).Result is OkObjectResult currentHttpPayment
    && ReferenceEquals(currentHttpPayment.Value, paymentHttpService.CurrentResponse),
    "current-payment GET returns the service's typed representation");
paymentHttpService.CurrentResponse = null;
Check((await paymentController.ConsultarActual("IC-123")).Result is NotFoundResult,
    "current-payment GET returns 404 when no payment exists");
var paymentControllerCalls = paymentHttpService.Calls;
Check((await CreatePaymentController(paymentHttpService, null).Iniciar("IC-123", paymentRequest)).Result is UnauthorizedResult
    && (await CreatePaymentController(paymentHttpService, null).ConsultarActual("IC-123")).Result is UnauthorizedResult
    && paymentHttpService.Calls == paymentControllerCalls,
    "missing authenticated identity is rejected before payment service access");
Check((await paymentController.Iniciar("IC-123", null!)).Result is BadRequestObjectResult,
    "direct null payment request maps to 400");
paymentHttpService.Exception = new CotizacionModificadaException(unchangedQuote);
Check((await paymentController.Iniciar("IC-123", paymentRequest)).Result is ConflictObjectResult changedQuote
    && ReferenceEquals(changedQuote.Value, unchangedQuote),
    "changed quote returns 409 with current server quote for explicit re-acceptance");
paymentHttpService.Exception = new CotizacionNoDisponibleException("private unavailable detail");
var unavailablePayment = await paymentController.Iniciar("IC-123", paymentRequest);
Check(unavailablePayment.Result is ConflictObjectResult unavailablePaymentBody
    && !unavailablePaymentBody.Value!.ToString()!.Contains("private unavailable detail", StringComparison.Ordinal),
    "expired or unavailable quote maps to sanitized 409");
paymentHttpService.Exception = new KeyNotFoundException("missing or foreign attempt");
Check((await paymentController.ConsultarActual("IC-123")).Result is NotFoundResult,
    "missing or foreign payment attempt maps to 404");
paymentHttpService.Exception = new ArgumentException("invalid accepted quote");
Check((await paymentController.Iniciar("IC-123", paymentRequest)).Result is BadRequestObjectResult,
    "invalid payment request maps to 400");
paymentHttpService.Exception = new InvalidOperationException("sensitive payment provider detail");
var unexpectedPayment = await paymentController.Iniciar("IC-123", paymentRequest);
Check(unexpectedPayment.Result is ObjectResult paymentFailure
    && paymentFailure.StatusCode == StatusCodes.Status500InternalServerError
    && !paymentFailure.Value!.ToString()!.Contains("sensitive payment provider detail", StringComparison.Ordinal),
    "unexpected payment errors are sanitized as 500");
using (var canceledPaymentRequest = new CancellationTokenSource())
{
    canceledPaymentRequest.Cancel();
    paymentHttpService.Exception = new OperationCanceledException();
    await CheckThrowsAsync<OperationCanceledException>(
        () => paymentController.Iniciar("IC-123", paymentRequest, canceledPaymentRequest.Token),
        "caller cancellation propagates unchanged from payment endpoint");
}

var deadline = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
var beforeDeadline = deadline.AddTicks(-1);
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline, beforeDeadline, true) == DecisionConfirmacion.Confirmar, "approved payment confirms before the original deadline when available");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline, deadline, true) == DecisionConfirmacion.SeguimientoDevolucion, "deadline equality is expired and follows refund");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline, deadline.AddTicks(1), true) == DecisionConfirmacion.SeguimientoDevolucion, "earlier provider approval cannot bypass late API confirmation");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline, beforeDeadline, false) == DecisionConfirmacion.SeguimientoDevolucion, "unavailable capacity follows refund even before deadline");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Pendiente, deadline, beforeDeadline, true) == DecisionConfirmacion.Esperar, "pending payment waits without issuance");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Desconocido, deadline, beforeDeadline, true) == DecisionConfirmacion.Esperar, "unknown payment waits without issuance");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Rechazado, deadline, beforeDeadline, true) == DecisionConfirmacion.NoEmitir, "rejected payment does not issue");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.Devuelto, deadline, beforeDeadline, true) == DecisionConfirmacion.NoEmitir, "refunded payment does not issue");
Check(PoliticaConfirmacionCompra.Decidir(EstadoPago.DevolucionPendiente, deadline, beforeDeadline, true) == DecisionConfirmacion.SeguimientoDevolucion, "refund pending remains in refund follow-up");
Check(PoliticaConfirmacionCompra.Decidir((EstadoPago)999, deadline, beforeDeadline, true) == DecisionConfirmacion.NoEmitir, "unrecognized payment state fails closed");
CheckThrows<ArgumentException>(() => PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline.ToOffset(TimeSpan.FromHours(-3)), beforeDeadline, true), "non-UTC deadline is rejected");
CheckThrows<ArgumentException>(() => PoliticaConfirmacionCompra.Decidir(EstadoPago.Aprobado, deadline, beforeDeadline.ToOffset(TimeSpan.FromHours(-3)), true), "non-UTC API confirmation time is rejected");

var checkoutToken = "TEST-contract-token";
var checkoutAmount = 125.50m;
var checkoutReference = "payment-123";
var checkoutResponse = """
    {"id":"order-1","external_reference":"payment-123","total_amount":"125.50","total_paid_amount":"125.50","currency":"UYU","status":"processed","status_detail":"accredited","checkout_url":"https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1"}
    """;
var createHandler = new MockHttpMessageHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Post, "checkout creation uses POST");
    Check(request.RequestUri == new Uri("https://api.mercadopago.com/v1/orders"), "checkout creation uses the fixed Orders endpoint");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == checkoutToken, "TEST access token is sent as a bearer header");
    Check(request.Headers.TryGetValues("X-Idempotency-Key", out var keys)
        && keys.Single() == "11111111-1111-1111-1111-111111111111", "create uses the caller's stable idempotency UUID");
    var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
    Check(body.GetProperty("type").GetString() == "online"
        && body.GetProperty("processing_mode").GetString() == "manual"
        && body.GetProperty("total_amount").GetString() == "125.50"
        && body.GetProperty("external_reference").GetString() == checkoutReference, "create sends the exact TEST order contract");
    return JsonResponse(HttpStatusCode.Created, checkoutResponse);
});
var createClient = CreateCheckoutClient(createHandler);
var createdOrder = await createClient.CrearOrdenAsync(
    Guid.Parse("11111111-1111-1111-1111-111111111111"), checkoutReference, checkoutAmount);
Check(createdOrder.Id == "order-1" && createdOrder.Estado == EstadoPago.Aprobado
    && createdOrder.CheckoutUrl == "https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1", "create returns validated provider order and hosted URL");
await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)))
        .CrearOrdenAsync(Guid.Empty, checkoutReference, checkoutAmount), "empty create idempotency key is rejected");
await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)))
        .CrearOrdenAsync(Guid.NewGuid(), checkoutReference, 125.501m), "amount with more than two decimals is rejected without rounding");
await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)))
        .CrearOrdenAsync(Guid.NewGuid(), " ", checkoutAmount), "blank external reference is rejected");

await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)))
        .ConsultarOrdenAsync(" ", checkoutReference, checkoutAmount), "blank provider order ID is rejected");

var getHandler = new MockHttpMessageHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Get, "order reconciliation uses GET");
    Check(request.RequestUri == new Uri("https://api.mercadopago.com/v1/orders/provider%2Fid"), "provider ID is escaped in fixed GET endpoint");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == checkoutToken, "TEST access token is sent as a bearer header");
    return JsonResponse(HttpStatusCode.OK, checkoutResponse.Replace("order-1", "provider/id", StringComparison.Ordinal));
});
var readOrder = await CreateCheckoutClient(getHandler).ConsultarOrdenAsync("provider/id", checkoutReference, checkoutAmount);
Check(readOrder.Id == "provider/id" && readOrder.Estado == EstadoPago.Aprobado, "GET validates requested order identity");
Check(readOrder.CheckoutUrl == "https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=provider/id",
    "GET propagates the provider-hosted checkout URL after validating the order");
await CheckThrowsAsync<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK,
        checkoutResponse.Replace("https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1", "https://attacker.example/checkout", StringComparison.Ordinal))))
        .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount),
    "GET refuses a checkout URL outside the trusted Mercado Pago host");

var searchStartUtc = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc);
var searchEndUtc = searchStartUtc.AddMinutes(10);
var searchedOrderJson = """
    {"id":"order-1","external_reference":"payment-123","total_amount":"125.50","total_paid_amount":"125.50","currency":"UYU","status":"processed","status_detail":"accredited"}
    """;
var searchHandler = new MockHttpMessageHandler((request, _) =>
{
    if (!request.RequestUri!.Query.Contains("external_reference", StringComparison.Ordinal))
        return JsonResponse(HttpStatusCode.OK, searchedOrderJson);

    Check(request.Method == HttpMethod.Get && request.RequestUri!.GetLeftPart(UriPartial.Path) == "https://api.mercadopago.com/v1/orders",
        "lost-order recovery uses the fixed public Orders search endpoint");
    Check(request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == checkoutToken,
        "lost-order recovery uses the configured TEST bearer credential");
    var query = Uri.UnescapeDataString(request.RequestUri!.Query);
    Check(query.Contains("begin_date=2026-11-16T14:00:00", StringComparison.Ordinal)
        && query.Contains("end_date=2026-11-16T14:10:00", StringComparison.Ordinal)
        && query.Contains("external_reference=payment-123", StringComparison.Ordinal)
        && query.Contains("limit=2", StringComparison.Ordinal) && query.Contains("offset=0", StringComparison.Ordinal),
        "lost-order search bounds its UTC window and requests a two-row ambiguity check for one payment reference");
    return JsonResponse(HttpStatusCode.OK,
        $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}");
});
var searchedOrder = await CreateCheckoutClient(searchHandler).BuscarOrdenAsync(
    checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount);
Check(searchedOrder is { Id: "order-1", Estado: EstadoPago.Aprobado, CheckoutUrl: null },
    "one complete matching result recovers validated order facts without inventing a checkout URL");

var recoveredUrlHandler = new MockHttpMessageHandler((request, _) =>
{
    if (request.RequestUri!.Query.Contains("external_reference", StringComparison.Ordinal))
        return JsonResponse(HttpStatusCode.OK,
            $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}");

    Check(request.Method == HttpMethod.Get
        && request.RequestUri == new Uri("https://api.mercadopago.com/v1/orders/order-1"),
        "unique search result is followed by a detail GET using its validated provider order ID");
    return JsonResponse(HttpStatusCode.OK, checkoutResponse);
});
var recoveredOrder = await CreateCheckoutClient(recoveredUrlHandler).BuscarOrdenAsync(
    checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount);
Check(recoveredOrder is { Id: "order-1", Estado: EstadoPago.Aprobado,
    CheckoutUrl: "https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1" },
    "unique exact search result recovers only its validated detail checkout URL");

var stringPaginationOrder = await CreateCheckoutClient(new MockHttpMessageHandler((request, _) => JsonResponse(
    HttpStatusCode.OK,
    request.RequestUri!.Query.Contains("external_reference", StringComparison.Ordinal)
        ? $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"1\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"2\"}}}}"
        : searchedOrderJson)))
    .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount);
Check(stringPaginationOrder is { Id: "order-1", Estado: EstadoPago.Aprobado },
    "numeric-string pagination recovers the same validated order as numeric JSON fields");

var createdOrderWithoutPaidAmountJson = """
    {"id":"order-created","external_reference":"payment-123","total_amount":"125.50","currency":"UYU","status":"created","status_detail":"created"}
    """;
var createdOrderWithoutPaidAmount = await CreateCheckoutClient(new MockHttpMessageHandler((request, _) => JsonResponse(
    HttpStatusCode.OK,
    request.RequestUri!.Query.Contains("external_reference", StringComparison.Ordinal)
        ? $"{{\"data\":[{createdOrderWithoutPaidAmountJson}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}"
        : createdOrderWithoutPaidAmountJson)))
    .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount);
Check(createdOrderWithoutPaidAmount is { Id: "order-created", Estado: EstadoPago.Pendiente, ImportePagado: 0m, CheckoutUrl: null },
    "created order without paid amount safely reconciles as unpaid without inventing a checkout URL");
await CheckThrowsAsync<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK,
        checkoutResponse.Replace(",\"total_paid_amount\":\"125.50\"", "", StringComparison.Ordinal))))
        .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount),
    "approved order without paid amount fails closed");

var emptySearch = await CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK,
    "{\"data\":[],\"paging\":{\"total\":0,\"total_pages\":0,\"offset\":0,\"limit\":2}}")))
    .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount);
Check(emptySearch is null, "empty authoritative order search preserves the unknown-payment outcome");
foreach (var ambiguousSearch in new[]
{
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":2,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson},{searchedOrderJson.Replace("order-1", "order-2", StringComparison.Ordinal)}],\"paging\":{{\"total\":2,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson.Replace("payment-123", "other-payment", StringComparison.Ordinal)}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson.Replace("125.50", "125.51", StringComparison.Ordinal)}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson.Replace("UYU", "USD", StringComparison.Ordinal)}],\"paging\":{{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    "{\"data\":[],\"paging\":{\"total\":0,\"total_pages\":0,\"offset\":1,\"limit\":2}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"1.0\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"one\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"2147483648\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":2147483648,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"-1\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":-1,\"total_pages\":1,\"offset\":0,\"limit\":2}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"1\",\"total_pages\":\"0\",\"offset\":\"0\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"1\",\"total_pages\":\"1\",\"offset\":\"1\",\"limit\":\"2\"}}}}",
    $"{{\"data\":[{searchedOrderJson}],\"paging\":{{\"total\":\"1\",\"total_pages\":\"1\",\"offset\":\"0\",\"limit\":\"3\"}}}}"
})
{
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, ambiguousSearch)))
            .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount),
        "ambiguous, truncated, mismatched, or malformed pagination never binds a recovered order");
}
var invalidWindowHandler = new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, "{}"));
await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(invalidWindowHandler).BuscarOrdenAsync(
        checkoutReference, searchEndUtc, searchStartUtc, checkoutAmount),
    "reversed recovery time windows are rejected before HTTP");
Check(invalidWindowHandler.RequestCount == 0, "invalid recovery window issues no provider request");
await CheckThrowsAsync<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.InternalServerError, "offline")))
        .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount),
    "order search network failures remain an unknown reconciliation result");
using (var canceledSearch = new CancellationTokenSource())
{
    canceledSearch.Cancel();
    await CheckThrowsAsync<OperationCanceledException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => throw new OperationCanceledException()))
            .BuscarOrdenAsync(checkoutReference, searchStartUtc, searchEndUtc, checkoutAmount, canceledSearch.Token),
        "caller cancellation propagates through order recovery search");
}

foreach (var (status, statusDetail, paid, expected) in new (string Status, string? Detail, string Paid, EstadoPago Expected)[]
{
    ("processed", "accredited", "125.50", EstadoPago.Aprobado),
    ("processed", "accredited", "50.00", EstadoPago.Desconocido),
    ("processed", "refunded", "125.50", EstadoPago.Devuelto),
    ("processed", "partially_refunded", "125.50", EstadoPago.DevolucionPendiente),
    ("processed", "unknown_detail", "125.50", EstadoPago.Desconocido),
    ("processed", null, "125.50", EstadoPago.Desconocido),
    ("created", "created", "0.00", EstadoPago.Pendiente),
    ("processing", "in_process", "0.00", EstadoPago.Pendiente),
    ("processing", "pending_review_manual", "0.00", EstadoPago.Pendiente),
    ("action_required", "waiting_capture", "0.00", EstadoPago.Pendiente),
    ("failed", "processing_error", "0.00", EstadoPago.Rechazado),
    ("canceled", "canceled", "0.00", EstadoPago.Rechazado),
    ("refunded", "refunded", "125.50", EstadoPago.Devuelto),
    ("future_status", "accredited", "125.50", EstadoPago.Desconocido),
    ("processing", "accredited", "125.50", EstadoPago.Desconocido)
})
{
    var body = JsonSerializer.Serialize(new
    {
        id = "order-1",
        external_reference = checkoutReference,
        total_amount = "125.50",
        total_paid_amount = paid,
        currency = "UYU",
        status,
        status_detail = statusDetail
    });
    var snapshot = await CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, body)))
        .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount);
    Check(snapshot.Estado == expected, $"provider status/detail {status}/{statusDetail ?? "missing"} maps fail-closed to {expected}");
}
var missingDetailResponse = checkoutResponse.Replace(",\"status_detail\":\"accredited\"", "", StringComparison.Ordinal);
var missingDetailOrder = await CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, missingDetailResponse)))
    .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount);
Check(missingDetailOrder.Estado == EstadoPago.Desconocido, "processed order without status_detail remains unknown");

foreach (var invalidId in new[] { ".", ".." })
{
    var handler = new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse));
    await CheckThrowsAsync<ArgumentException>(
        () => CreateCheckoutClient(handler).ConsultarOrdenAsync(invalidId, checkoutReference, checkoutAmount),
        $"dot-segment provider ID {invalidId} is rejected");
    Check(handler.RequestCount == 0, $"dot-segment provider ID {invalidId} is rejected before HTTP");
}

foreach (var invalidSnapshot in new[]
{
    checkoutResponse.Replace("payment-123", "another-payment", StringComparison.Ordinal),
    checkoutResponse.Replace("125.50", "125.51", StringComparison.Ordinal),
    checkoutResponse.Replace("UYU", "USD", StringComparison.Ordinal),
    checkoutResponse.Replace("order-1", "different-order", StringComparison.Ordinal)
})
{
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, invalidSnapshot)))
            .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount), "mismatched provider facts reject reconciliation");
}

foreach (var (providerAmount, expectedAmount) in new[]
{
    ("1,25", 125m),
    ("1,000.00", 1000m),
    (" 125.50", 125.50m)
})
{
    var invalidAmount = checkoutResponse
        .Replace("125.50", providerAmount, StringComparison.Ordinal);
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, invalidAmount)))
            .ConsultarOrdenAsync("order-1", checkoutReference, expectedAmount),
        $"provider amount '{providerAmount}' is rejected instead of locale-normalized");
}

foreach (var unsafeUrl in new[]
{
    "http://www.mercadopago.com.uy/checkout",
    "https://mercadopago.com.uy/checkout",
    "https://attacker.example/checkout",
    "https://user@www.mercadopago.com.uy/checkout"
})
{
    var body = checkoutResponse.Replace("https://www.mercadopago.com.uy/checkout/v1/redirect?order_id=order-1", unsafeUrl, StringComparison.Ordinal);
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.Created, body)))
            .CrearOrdenAsync(Guid.NewGuid(), checkoutReference, checkoutAmount), "unsafe hosted checkout URL is rejected");
}

var refundHandler = new MockHttpMessageHandler((request, _) =>
{
    Check(request.Method == HttpMethod.Post
        && request.RequestUri == new Uri("https://api.mercadopago.com/v1/orders/order-1/refund"), "refund request uses official full-refund endpoint");
    Check(request.Headers.TryGetValues("X-Idempotency-Key", out var keys)
        && keys.Single() == "22222222-2222-2222-2222-222222222222", "refund uses the caller's stable idempotency UUID");
    Check(request.Content is null, "full refund request sends no amount body");
    return JsonResponse(HttpStatusCode.OK, "{}");
});
await CreateCheckoutClient(refundHandler).SolicitarDevolucionCompletaAsync(
    "order-1", Guid.Parse("22222222-2222-2222-2222-222222222222"));
Check(refundHandler.RequestCount == 1, "refund operation only submits a request and does not claim domain completion");

var opaqueTokenHandler = new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse));
await CreateCheckoutClient(opaqueTokenHandler, "opaque-token-fixture")
    .CrearOrdenAsync(Guid.NewGuid(), checkoutReference, checkoutAmount);
Check(opaqueTokenHandler.IdentityRequestCount == 1 && opaqueTokenHandler.RequestCount == 1,
    "opaque credential format proceeds only after the mocked test identity matches");
var missingTokenConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["MercadoPago:TestCredentialOriginConfirmed"] = "true",
    ["MercadoPago:ExpectedSellerId"] = "987654321",
    ["MercadoPago:ExpectedSiteId"] = "SITE_TEST_FIXTURE"
}).Build();
CheckThrows<InvalidOperationException>(
    () => ParametrosMercadoPago.Desde(missingTokenConfiguration),
    "missing private credential is rejected when client resolves");

await CheckThrowsAsync<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.InternalServerError, "secret response")))
        .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount), "provider HTTP errors fail safely");
await CheckThrowsAsync<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, "not-json")))
        .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount), "invalid provider JSON fails safely");
using (var canceled = new CancellationTokenSource())
{
    canceled.Cancel();
    await CheckThrowsAsync<OperationCanceledException>(
        () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => throw new OperationCanceledException()))
            .ConsultarOrdenAsync("order-1", checkoutReference, checkoutAmount, canceled.Token), "cancellation propagates unchanged");
}

var synchronizationResponseType = typeof(SincronizarPagoResponseDto);
Check(synchronizationResponseType.GetProperty("Pago")?.PropertyType == typeof(PagoResponseDto)
    && synchronizationResponseType.GetProperty("Compra")?.PropertyType == typeof(CompraConfirmadaResponseDto),
    "payment synchronization separates payment status from optional atomic purchase confirmation");
var confirmedPurchaseType = typeof(CompraConfirmadaResponseDto);
Check(new[] { "CompraId", "IntentoId", "PagoId", "ConfirmadoUtc", "ImporteTotal", "Tickets" }
    .All(name => confirmedPurchaseType.GetProperty(name) is not null),
    "purchase confirmation exposes stable purchase/payment identity, UTC confirmation, amount, and tickets");
var issuedTicketType = typeof(TicketEmitidoDto);
Check(new[] { "TicketId", "Orden", "ServicioId", "TramoId", "OrigenNombre", "DestinoNombre", "Salida", "Llegada", "Importe", "PoliticaAsiento", "NumeroAsiento", "QrOpaco" }
    .All(name => issuedTicketType.GetProperty(name) is not null),
    "ticket contract exposes frozen leg details, optional assigned seat, and opaque QR payload");
Check(typeof(TicketEmitidoDto).GetProperty("QrOpaco")?.PropertyType == typeof(string)
    && typeof(TicketEmitidoDto).GetProperty("NumeroAsiento")?.PropertyType == typeof(int?),
    "ticket QR is an opaque string and no-seat tickets can omit an assigned seat number");
var reconciliationType = typeof(ResultadoConciliacionPago);
Check(new[] { "Registro", "Compra", "RequiereDevolucion" }.All(name => reconciliationType.GetProperty(name) is not null),
    "repository reconciliation result carries durable payment, optional purchase, and refund follow-up state");
var synchronizationServiceMethod = typeof(IConciliacionesPagoService).GetMethod("SincronizarAsync");
Check(synchronizationServiceMethod?.ReturnType == typeof(Task<SincronizarPagoResponseDto>)
    && synchronizationServiceMethod.GetParameters().Select(parameter => parameter.ParameterType)
        .SequenceEqual([typeof(string), typeof(string), typeof(CancellationToken)]),
    "payment synchronization service is owner-scoped, attempt-scoped, asynchronous, and cancelable");
var confirmationRepositoryType = typeof(IConfirmacionesCompraRepository);
Check(confirmationRepositoryType.GetMethod("ConsultarAsync") is not null
    && confirmationRepositoryType.GetMethod("ConciliarAsync") is not null,
    "purchase confirmation repository separates owned snapshot reads from transactional reconciliation");

var confirmationCreatedUtc = new DateTime(2026, 11, 16, 14, 59, 0, DateTimeKind.Utc);
var confirmationHoldExpiryUtc = confirmationCreatedUtc.AddMinutes(1);
var frozenSelection = new[]
{
    ConfirmationSelection(1, "service-first", "segment-first", "stop-a", "Origin A", "stop-b", "Transfer", 100m, PoliticaAsiento.SinAsiento, null, confirmationCreatedUtc.AddHours(1), confirmationCreatedUtc.AddHours(2)),
    ConfirmationSelection(2, "service-second", "segment-second", "stop-b", "Transfer", "stop-c", "Destination C", 75m, PoliticaAsiento.AsignacionAutomatica, 12, confirmationCreatedUtc.AddHours(2.5), confirmationCreatedUtc.AddHours(3.5))
};
var frozenPaymentPrices = new[]
{
    new TramoPago { PagoId = "PG-confirm", IntentoId = "IC-confirm", Orden = 1, Importe = 210m },
    new TramoPago { PagoId = "PG-confirm", IntentoId = "IC-confirm", Orden = 2, Importe = 140m }
};
var approvedConfirmation = ConfirmationRecord(EstadoPago.Aprobado, 350m, confirmationHoldExpiryUtc);
var emitted = EmisionCompra.Preparar(
    approvedConfirmation,
    frozenPaymentPrices,
    frozenSelection,
    "CO-confirm",
    ["CS-first", "CS-second"],
    ["TK-first", "TK-second"],
    confirmationCreatedUtc);
var emittedQrValues = emitted.Tickets.Select(ticket => ticket.QrOpaco).ToArray();
Check(emitted.Compra.Id == "CO-confirm" && emitted.Compra.PagoId == "PG-confirm"
    && emitted.Compra.IntentoId == "IC-confirm" && emitted.Compra.ImporteTotal == 350m
    && emitted.Compra.ConfirmadoUtc == confirmationCreatedUtc,
    "approved full payment produces one purchase using the expected amount and supplied UTC confirmation time");
Check(emitted.Servicios.Select(service => (service.Id, service.Orden, service.Importe))
        .SequenceEqual([("CS-first", 1, 210m), ("CS-second", 2, 140m)])
    && emitted.Tickets.Select(ticket => ticket.CompraServicioId).SequenceEqual(["CS-first", "CS-second"]),
    "materialization emits one ordered purchase leg and ticket per frozen payment leg");
Check(emittedQrValues.Length == 2 && emittedQrValues.Distinct(StringComparer.Ordinal).Count() == 2
    && emittedQrValues.All(value => value.Length == 64 && value.All(Uri.IsHexDigit))
    && emittedQrValues.All(value => !value.Contains("IC-confirm", StringComparison.Ordinal)
        && !value.Contains("PG-confirm", StringComparison.Ordinal)
        && !value.Contains("service-first", StringComparison.Ordinal)),
    "ticket QR values are unique opaque 32-byte hex payloads without purchase or passenger identifiers");
Check(emitted.Servicios.Sum(service => service.Importe) == approvedConfirmation.Pago.ImportePagado
    && emitted.Servicios[0].Importe != frozenSelection[0].Importe
    && emitted.Servicios[1].Importe != frozenSelection[1].Importe,
    "issued prices come from the frozen payment snapshot rather than the original itinerary quote");

foreach (var (name, invalidRecord, invalidPrices, invalidSelection, compraId, csIds, ticketIds, confirmedUtc) in new[]
{
    ("partial payment", ConfirmationRecord(EstadoPago.Aprobado, 349m, confirmationHoldExpiryUtc), frozenPaymentPrices, frozenSelection, "CO-x", new[] { "CS-1", "CS-2" }, new[] { "TK-1", "TK-2" }, confirmationCreatedUtc),
    ("expired hold", ConfirmationRecord(EstadoPago.Aprobado, 350m, confirmationCreatedUtc), frozenPaymentPrices, frozenSelection, "CO-x", new[] { "CS-1", "CS-2" }, new[] { "TK-1", "TK-2" }, confirmationCreatedUtc),
    ("missing frozen price", approvedConfirmation, frozenPaymentPrices[..1], frozenSelection, "CO-x", new[] { "CS-1", "CS-2" }, new[] { "TK-1", "TK-2" }, confirmationCreatedUtc),
    ("wrong total", approvedConfirmation, [new TramoPago { PagoId = "PG-confirm", IntentoId = "IC-confirm", Orden = 1, Importe = 211m }, new TramoPago { PagoId = "PG-confirm", IntentoId = "IC-confirm", Orden = 2, Importe = 140m }], frozenSelection, "CO-x", new[] { "CS-1", "CS-2" }, new[] { "TK-1", "TK-2" }, confirmationCreatedUtc),
    ("wrong generated identity count", approvedConfirmation, frozenPaymentPrices, frozenSelection, "CO-x", new[] { "CS-1" }, new[] { "TK-1", "TK-2" }, confirmationCreatedUtc),
    ("non-UTC confirmation time", approvedConfirmation, frozenPaymentPrices, frozenSelection, "CO-x", new[] { "CS-1", "CS-2" }, new[] { "TK-1", "TK-2" }, DateTime.SpecifyKind(confirmationCreatedUtc, DateTimeKind.Unspecified))
})
{
    PlanEmisionCompra? partialPlan = null;
    CheckThrows<Exception>(() => partialPlan = EmisionCompra.Preparar(
        invalidRecord, invalidPrices, invalidSelection, compraId, csIds, ticketIds, confirmedUtc),
        $"materializer rejects {name} before returning a partial plan");
    Check(partialPlan is null, $"invalid {name} leaves no materialized plan available for persistence");
}

var validDecision = PoliticaConfirmacionCompra.Decidir(
    EstadoPago.Aprobado,
    new DateTimeOffset(confirmationHoldExpiryUtc, TimeSpan.Zero),
    new DateTimeOffset(confirmationCreatedUtc, TimeSpan.Zero),
    disponibilidadConfirmada: true);
var expiresAtDeadlineDecision = PoliticaConfirmacionCompra.Decidir(
    EstadoPago.Aprobado,
    new DateTimeOffset(confirmationHoldExpiryUtc, TimeSpan.Zero),
    new DateTimeOffset(confirmationHoldExpiryUtc, TimeSpan.Zero),
    disponibilidadConfirmada: true);
Check(validDecision == DecisionConfirmacion.Confirmar
    && expiresAtDeadlineDecision == DecisionConfirmacion.SeguimientoDevolucion,
    "purchase decision uses API confirmation time and exact original-hold deadline, not provider approval time");

var concurrentPurchaseStore = new FakeAtomicPurchaseStore();
var concurrentMaterializations = await Task.WhenAll(Enumerable.Range(0, 8)
    .Select(_ => concurrentPurchaseStore.ConfirmAsync(approvedConfirmation, frozenPaymentPrices, frozenSelection, confirmationCreatedUtc)));
Check(concurrentPurchaseStore.PublishedPurchaseCount == 1
    && concurrentPurchaseStore.PublishedTicketCount == 2
    && concurrentMaterializations.Select(result => result.CompraId).Distinct(StringComparer.Ordinal).Count() == 1
    && concurrentMaterializations.SelectMany(result => result.QrValues).Distinct(StringComparer.Ordinal).Count() == 2
    && concurrentMaterializations.SelectMany(result => result.QrValues).Distinct(StringComparer.Ordinal).Count() == concurrentMaterializations[0].QrValues.Count,
    "same-payment fake concurrency elects one materialization and all callers retain the same purchase and QR identities");
var failingPurchaseStore = new FakeAtomicPurchaseStore { FailBeforePublish = true };
await CheckThrowsAsync<InvalidOperationException>(
    () => failingPurchaseStore.ConfirmAsync(approvedConfirmation, frozenPaymentPrices, frozenSelection, confirmationCreatedUtc),
    "materialization failure before fake atomic publish is surfaced");
Check(failingPurchaseStore.PublishedPurchaseCount == 0 && failingPurchaseStore.PublishedTicketCount == 0,
    "failed fake atomic publish leaves no visible purchase or partial tickets; this does not prove database rollback");

const string confirmationReference = "11111111-1111-1111-1111-111111111111";
var noOrderSnapshot = ConfirmationResult(EstadoPago.Desconocido, 0m, "", 350m, confirmationHoldExpiryUtc, null, false);
var recoveryClock = new FakeTimeProvider(new DateTimeOffset(confirmationCreatedUtc.AddSeconds(20), TimeSpan.Zero));
var noOrderRepository = new FakeConfirmationRepository(noOrderSnapshot);
var noOrderGateway = new FakeMercadoPagoCheckoutClient();
var noOrderService = new ConciliacionesPagoService(noOrderRepository, noOrderGateway, recoveryClock);
var noOrderResponse = await noOrderService.SincronizarAsync("passenger", "IC-confirm");
Check(noOrderResponse.Pago.Estado == EstadoPago.Desconocido && noOrderResponse.Compra is null
    && noOrderGateway.QueryCalls == 0 && noOrderGateway.SearchCalls == 1 && noOrderGateway.RefundCalls == 0
    && noOrderGateway.CreateCalls == 0 && noOrderRepository.ConciliateCalls == 0,
    "missing stored provider order searches once, preserves unknown on no match, and never creates or issues tickets");
Check(noOrderGateway.LastSearchReference == confirmationReference
    && noOrderGateway.LastSearchReference != noOrderSnapshot.Registro.Pago.Id
    && noOrderGateway.LastSearchStartUtc == noOrderSnapshot.Registro.Pago.CreadoUtc - ConciliacionesPagoService.MargenDesfaseReloj
    && noOrderGateway.LastSearchEndUtc == recoveryClock.GetUtcNow().UtcDateTime,
    "recovery search uses the durable creation GUID (not the PG id) and a creation-minus-clock-skew-margin to current-API time window");

var searchFailureRepository = new FakeConfirmationRepository(noOrderSnapshot);
var searchFailureGateway = new FakeMercadoPagoCheckoutClient { SearchException = new HttpRequestException("offline") };
var searchFailureResponse = await new ConciliacionesPagoService(searchFailureRepository, searchFailureGateway, recoveryClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(searchFailureResponse.Pago.Estado == EstadoPago.Desconocido && searchFailureResponse.Compra is null
    && searchFailureGateway.SearchCalls == 1 && searchFailureGateway.CreateCalls == 0
    && searchFailureRepository.ConciliateCalls == 0,
    "recovery search outage preserves durable unknown without rejection, second create, or ticket fulfillment");

var pendingRecoveredOrder = ConfirmationOrder(EstadoPago.Pendiente, 0m, "order-pending") with { CheckoutUrl = null };
var pendingRecoveredResult = ConfirmationResult(EstadoPago.Pendiente, 0m, "order-pending", 350m,
    confirmationHoldExpiryUtc, null, false);
var pendingRecoveryRepository = new FakeConfirmationRepository(noOrderSnapshot)
{
    OnConciliate = (_, _) => pendingRecoveredResult
};
var pendingRecoveryGateway = new FakeMercadoPagoCheckoutClient { SearchResponse = pendingRecoveredOrder };
var pendingRecoveredResponse = await new ConciliacionesPagoService(pendingRecoveryRepository, pendingRecoveryGateway, recoveryClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(pendingRecoveredResponse.Pago.Estado == EstadoPago.Pendiente && pendingRecoveredResponse.Pago.CheckoutUrl is null
    && pendingRecoveredResponse.Compra is null && pendingRecoveryGateway.CreateCalls == 0,
    "recovered pending order remains synchronizable without an invented checkout URL or second order creation");

var realShapeRecoveryPayment = ConfirmationRecord(EstadoPago.Desconocido, 0m, confirmationHoldExpiryUtc, null).Pago;
var realShapeRecoveryHandler = CreateRealShapeHandler(() => { }, realShapeCheckoutUrl, () => confirmationReference);
var realShapeRecoveryRepository = new FakeConfirmationRepository(
    new ResultadoConciliacionPago(new RegistroPago(realShapeRecoveryPayment, confirmationHoldExpiryUtc, false), null, false))
{
    OnConciliate = (order, _) =>
    {
        var reconciled = ConfirmationResult(order.Estado, order.ImportePagado, order.Id, 350m, confirmationHoldExpiryUtc, null, false);
        reconciled.Registro.Pago.CheckoutUrl = order.CheckoutUrl;
        return reconciled;
    }
};
var realShapeRecovered = await new ConciliacionesPagoService(realShapeRecoveryRepository,
        CreateCheckoutClient(realShapeRecoveryHandler), recoveryClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(realShapeRecovered.Pago.Estado == EstadoPago.Pendiente && realShapeRecovered.Pago.CheckoutUrl == realShapeCheckoutUrl
    && realShapeRecovered.Compra is null,
    "unknown payment recovered from a real-shape order becomes pending with its checkout URL while the hold is live");

var queryFailureRepository = new FakeConfirmationRepository(noOrderSnapshot with
{
    Registro = ConfirmationRecord(EstadoPago.Desconocido, 0m, confirmationHoldExpiryUtc, "order-stored")
});
var queryFailureGateway = new FakeMercadoPagoCheckoutClient { QueryException = new HttpRequestException("offline") };
var queryFailureService = new ConciliacionesPagoService(queryFailureRepository, queryFailureGateway, paymentNow);
var queryFailureResponse = await queryFailureService.SincronizarAsync("passenger", "IC-confirm");
Check(queryFailureResponse.Pago.Estado == EstadoPago.Desconocido && queryFailureResponse.Compra is null
    && queryFailureRepository.ConciliateCalls == 0 && queryFailureGateway.CreateCalls == 0,
    "provider query failure preserves the last payment snapshot without fake rejection or fulfillment");
Check(queryFailureGateway.LastQueryOrderId == "order-stored"
    && queryFailureGateway.LastQueryReference == confirmationReference
    && queryFailureGateway.LastQueryAmount == 350m,
    "payment sync queries the stored provider order with the durable payment reference and expected amount");

var foreignSnapshotRepository = new FakeConfirmationRepository(null);
var foreignSnapshotGateway = new FakeMercadoPagoCheckoutClient();
await CheckThrowsAsync<KeyNotFoundException>(
    () => new ConciliacionesPagoService(foreignSnapshotRepository, foreignSnapshotGateway, paymentNow)
        .SincronizarAsync("foreign-passenger", "IC-confirm"),
    "foreign or missing owner snapshot fails as not found before provider access");
Check(foreignSnapshotGateway.QueryCalls == 0 && foreignSnapshotGateway.RefundCalls == 0,
    "unowned payment never triggers provider reconciliation or refund");

var confirmedPurchaseDto = new CompraConfirmadaResponseDto
{
    CompraId = "CO-confirm", IntentoId = "IC-confirm", PagoId = "PG-confirm",
    ConfirmadoUtc = confirmationCreatedUtc, ImporteTotal = 350m, Tickets = []
};
var approvedOrder = ConfirmationOrder(EstadoPago.Aprobado, 350m, "order-stored");
var approvedResult = ConfirmationResult(EstadoPago.Aprobado, 350m, "order-stored", 350m,
    confirmationHoldExpiryUtc, confirmedPurchaseDto, false);
var approvedRepo = new FakeConfirmationRepository(ConfirmationResult(EstadoPago.Pendiente, 0m,
    "order-stored", 350m, confirmationHoldExpiryUtc, null, false))
{
    OnConciliate = (_, _) => approvedResult
};
var approvedGateway = new FakeMercadoPagoCheckoutClient { QueryResponses = [approvedOrder] };
var approvedSync = await new ConciliacionesPagoService(approvedRepo, approvedGateway, paymentNow)
    .SincronizarAsync("passenger", "IC-confirm");
Check(approvedSync.Pago.Estado == EstadoPago.Aprobado && approvedSync.Compra?.CompraId == "CO-confirm"
    && approvedRepo.Events.SequenceEqual(["snapshot", "reconcile"])
    && approvedGateway.Events.SequenceEqual(["get"]),
    "verified provider approval is reconciled after the owned DB snapshot and returns only repository-confirmed purchase");
Check(approvedRepo.LastOwnerId == "passenger" && approvedRepo.LastAttemptId == "IC-confirm"
    && approvedGateway.LastQueryOrderId == "order-stored"
    && approvedGateway.LastQueryReference == confirmationReference
    && approvedGateway.LastQueryAmount == 350m,
    "synchronization is scoped to passenger and attempt while validating stored order, payment, and amount");
Check(approvedGateway.SearchCalls == 0,
    "a known persisted provider order uses direct GET and never invokes recovery search");

var recoveredApproved = ConfirmationResult(EstadoPago.Aprobado, 350m, "order-1", 350m,
    confirmationHoldExpiryUtc, confirmedPurchaseDto, false);
var recoveryRepository = new FakeConfirmationRepository(noOrderSnapshot)
{
    OnConciliate = (_, _) => recoveredApproved
};
var recoveryGateway = new FakeMercadoPagoCheckoutClient
{
    SearchResponse = ConfirmationOrder(EstadoPago.Aprobado, 350m, "order-1") with { CheckoutUrl = null }
};
var recoveredResponse = await new ConciliacionesPagoService(recoveryRepository, recoveryGateway, recoveryClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(recoveredResponse.Pago.Estado == EstadoPago.Aprobado && recoveredResponse.Compra?.PagoId == "PG-confirm"
    && recoveryRepository.ConciliateCalls == 1 && recoveryGateway.Events.SequenceEqual(["search"])
    && recoveryGateway.CreateCalls == 0,
    "one verified search match binds to the existing payment through ordinary locked reconciliation without recreating checkout");
Check(recoveryGateway.LastSearchReference == confirmationReference && recoveryGateway.LastSearchAmount == 350m
    && recoveryGateway.LastSearchStartUtc == noOrderSnapshot.Registro.Pago.CreadoUtc - ConciliacionesPagoService.MargenDesfaseReloj
    && recoveryGateway.LastSearchEndUtc == recoveryClock.GetUtcNow().UtcDateTime,
    "recovery passes the exact durable reference, expected amount, and uncapped persisted-to-now window");

var lateRefundPending = ConfirmationResult(EstadoPago.DevolucionPendiente, 350m, "order-stored", 350m,
    confirmationCreatedUtc, null, true);
var lateApprovedRepository = new FakeConfirmationRepository(ConfirmationResult(EstadoPago.Pendiente, 0m,
    "order-stored", 350m, confirmationCreatedUtc, null, false))
{
    OnConciliate = (order, call) => call == 1
        ? lateRefundPending
        : ConfirmationResult(order.Estado, order.ImportePagado, "order-stored", 350m,
            confirmationCreatedUtc, null, order.Estado == EstadoPago.DevolucionPendiente)
};
var lateGateway = new FakeMercadoPagoCheckoutClient
{
    QueryResponses = [approvedOrder, ConfirmationOrder(EstadoPago.Devuelto, 0m, "order-stored")]
};
var lateClock = new FakeTimeProvider(new DateTimeOffset(confirmationHoldExpiryUtc, TimeSpan.Zero));
var lateSync = await new ConciliacionesPagoService(lateApprovedRepository, lateGateway, lateClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(lateSync.Pago.Estado == EstadoPago.Devuelto && lateSync.Compra is null
    && lateApprovedRepository.Events.SequenceEqual(["snapshot", "reconcile", "reconcile"])
    && lateGateway.Events.SequenceEqual(["get", "refund", "get"])
    && lateGateway.LastRefundOrderId == "order-stored"
    && lateGateway.LastRefundKey == lateRefundPending.Registro.Pago.ClaveDevolucion,
    "late provider approval without live hold requests keyed full refund, then GET-verifies Devuelto before completion");

var refundStillPending = ConfirmationResult(EstadoPago.DevolucionPendiente, 350m, "order-stored", 350m,
    confirmationCreatedUtc, null, true);
var pendingRefundRepository = new FakeConfirmationRepository(ConfirmationResult(EstadoPago.Pendiente, 0m,
    "order-stored", 350m, confirmationCreatedUtc, null, false))
{
    OnConciliate = (_, _) => refundStillPending
};
var pendingRefundGateway = new FakeMercadoPagoCheckoutClient { QueryResponses = [approvedOrder, approvedOrder] };
var pendingRefundResponse = await new ConciliacionesPagoService(pendingRefundRepository, pendingRefundGateway, lateClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(pendingRefundResponse.Pago.Estado == EstadoPago.DevolucionPendiente
    && pendingRefundResponse.Compra is null && pendingRefundGateway.Events.SequenceEqual(["get", "refund", "get"]),
    "successful refund request does not mark payment refunded while authoritative GET still reports approval");

var refundFailureRepository = new FakeConfirmationRepository(ConfirmationResult(EstadoPago.Pendiente, 0m,
    "order-stored", 350m, confirmationCreatedUtc, null, false))
{
    OnConciliate = (_, _) => refundStillPending
};
var refundFailureGateway = new FakeMercadoPagoCheckoutClient
{
    QueryResponses = [approvedOrder], RefundException = new HttpRequestException("refund unavailable")
};
var refundFailureResponse = await new ConciliacionesPagoService(refundFailureRepository, refundFailureGateway, lateClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(refundFailureResponse.Pago.Estado == EstadoPago.DevolucionPendiente
    && refundFailureResponse.Compra is null && refundFailureGateway.Events.SequenceEqual(["get", "refund"])
    && refundFailureGateway.LastRefundKey == refundStillPending.Registro.Pago.ClaveDevolucion,
    "refund request failure keeps durable pending refund and the same idempotency key without ticket issuance");

using (var canceledSync = new CancellationTokenSource())
{
    canceledSync.Cancel();
    var canceledSyncRepository = new FakeConfirmationRepository(noOrderSnapshot);
    var canceledSyncGateway = new FakeMercadoPagoCheckoutClient();
    await CheckThrowsAsync<OperationCanceledException>(
        () => new ConciliacionesPagoService(canceledSyncRepository, canceledSyncGateway, paymentNow)
            .SincronizarAsync("passenger", "IC-confirm", canceledSync.Token),
        "caller cancellation propagates through synchronization");
    Check(canceledSyncRepository.Events.Count == 0 && canceledSyncGateway.Events.Count == 0,
        "canceled synchronization performs no provider side effect or destructive local transition");
}

// ---- Cancelación en el proveedor de la orden sin pagar cuando vence el hold ----
const string expOrderId = "ORD-REAL-1";
const string expCheckoutUrl = "https://www.mercadopago.com.uy/checkout/v1/redirect?pref_id=expired-case";
var expCreationKey = Guid.Parse(confirmationReference);
var expExpiredClock = new FakeTimeProvider(new DateTimeOffset(confirmationHoldExpiryUtc, TimeSpan.Zero));
var expLiveClock = paymentNow;

static string ExpOrderJson(string status, string? detail = null, string? paid = null, string id = expOrderId,
    string amount = "350.00", string? reference = null, bool checkout = true) =>
    "{\"id\":\"" + id + "\",\"type\":\"online\",\"processing_mode\":\"manual\",\"external_reference\":\""
    + (reference ?? confirmationReference) + "\",\"total_amount\":\"" + amount + "\""
    + (checkout ? ",\"checkout_url\":\"" + expCheckoutUrl + "\"" : "")
    + ",\"country_code\":\"URY\",\"user_id\":\"987654321\",\"status\":\"" + status + "\""
    + (detail is null ? "" : ",\"status_detail\":\"" + detail + "\"")
    + (paid is null ? "" : ",\"total_paid_amount\":\"" + paid + "\"")
    + ",\"capture_mode\":\"automatic_async\",\"currency\":\"UYU\",\"created_date\":\"2026-11-16T14:00:01.000Z\","
    + "\"last_updated_date\":\"2026-11-16T14:00:01.000Z\",\"integration_data\":{\"application_id\":\"1\"},\"items\":[]}";

static Guid? ExpHeaderKey(HttpRequestMessage request) =>
    request.Headers.TryGetValues("X-Idempotency-Key", out var values) ? Guid.Parse(values.Single()) : null;

static int ExpCancels(List<HttpRequestMessageSnapshot> requests) =>
    requests.Count(r => r.Method == "POST" && r.Path.EndsWith("/cancel", StringComparison.Ordinal));
static int ExpRefunds(List<HttpRequestMessageSnapshot> requests) =>
    requests.Count(r => r.Method == "POST" && r.Path.EndsWith("/refund", StringComparison.Ordinal));

static HttpResponseMessage ExpCanceledOk(HttpRequestMessage request, CancellationToken token) =>
    JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", "canceled_transaction", checkout: false));

(FakeConfirmationRepository Repository, List<OrdenCheckoutMercadoPago> Reconciled,
    List<HttpRequestMessageSnapshot> Requests, ConciliacionesPagoService Service) ExpBuild(
    EstadoPago snapshotState, FakeTimeProvider clock, Func<bool, string> getBody,
    Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> cancelResponse)
{
    var reconciled = new List<OrdenCheckoutMercadoPago>();
    var requests = new List<HttpRequestMessageSnapshot>();
    var canceledByProvider = false;
    var repository = new FakeConfirmationRepository(ConfirmationResult(snapshotState, 0m, expOrderId, 350m,
        confirmationHoldExpiryUtc, null, false))
    {
        OnConciliate = (order, _) =>
        {
            reconciled.Add(order);
            var refundRequired = order.Estado == EstadoPago.Aprobado;
            var result = ConfirmationResult(refundRequired ? EstadoPago.DevolucionPendiente : order.Estado,
                order.ImportePagado, order.Id, 350m, confirmationHoldExpiryUtc, null, refundRequired);
            result.Registro.Pago.CheckoutUrl = order.CheckoutUrl;
            return result;
        }
    };
    var handler = new MockHttpMessageHandler((request, token) =>
    {
        var path = request.RequestUri!.AbsolutePath;
        requests.Add(new HttpRequestMessageSnapshot(request.Method.Method, path, ExpHeaderKey(request), request.Content is not null));
        if (path.EndsWith("/cancel", StringComparison.Ordinal))
        {
            var response = cancelResponse(request, token);
            if (response.IsSuccessStatusCode)
                canceledByProvider = true;
            return response;
        }
        if (path.EndsWith("/refund", StringComparison.Ordinal))
            return JsonResponse(HttpStatusCode.OK, "{}");
        return JsonResponse(HttpStatusCode.OK, getBody(canceledByProvider));
    });
    var service = new ConciliacionesPagoService(repository, CreateCheckoutClient(handler), clock);
    return (repository, reconciled, requests, service);
}

// 1. Hold vencido + orden "created" (forma real, sin status_detail): se cancela una vez y el pago queda Rechazado.
var expCancelRun = ExpBuild(EstadoPago.Pendiente, expExpiredClock,
    canceled => canceled ? ExpOrderJson("canceled", "canceled_transaction") : ExpOrderJson("created"), ExpCanceledOk);
var expCancelResponse = await expCancelRun.Service.SincronizarAsync("passenger", "IC-confirm");
var expCancelRequest = expCancelRun.Requests.FirstOrDefault(r => r.Path.EndsWith("/cancel", StringComparison.Ordinal));
Check(ExpCancels(expCancelRun.Requests) == 1 && expCancelRequest is not null && expCancelRequest.Method == "POST"
    && expCancelRequest!.Path == "/v1/orders/ORD-REAL-1/cancel" && !expCancelRequest.HasBody
    && expCancelRequest.IdempotencyKey == ConciliacionesPagoService.ClaveCancelacion(expCreationKey),
    "expired hold with an unpaid created order cancels it once at the provider with the stable per-payment idempotency key and no body");
Check(expCancelResponse.Pago.Estado == EstadoPago.Rechazado && expCancelResponse.Pago.CheckoutUrl is null
    && expCancelResponse.Compra is null && expCancelRun.Repository.ConciliateCalls == 1
    && expCancelRun.Reconciled.Single().Estado == EstadoPago.Rechazado
    && expCancelRun.Reconciled.Single().ImportePagado == 0m && expCancelRun.Reconciled.Single().Id == expOrderId
    && expCancelRun.Repository.Events.SequenceEqual(["snapshot", "reconcile"]),
    "verified provider cancellation is persisted through locked reconciliation as Rechazado with zero paid, the provider order id and no checkout URL");

// 6. Una sincronización posterior sobre el pago ya Rechazado no vuelve a cancelar.
var expRepeatResponse = await expCancelRun.Service.SincronizarAsync("passenger", "IC-confirm");
Check(expRepeatResponse.Pago.Estado == EstadoPago.Rechazado && ExpCancels(expCancelRun.Requests) == 1
    && expCancelRun.Requests.Count(r => r.Method == "GET") >= 2,
    "repeated synchronization after Rechazado re-reads the order but never cancels again");

var expDirectRejected = ExpBuild(EstadoPago.Rechazado, expExpiredClock,
    _ => ExpOrderJson("canceled", "canceled_transaction", checkout: false), ExpCanceledOk);
var expDirectRejectedResponse = await expDirectRejected.Service.SincronizarAsync("passenger", "IC-confirm");
Check(expDirectRejectedResponse.Pago.Estado == EstadoPago.Rechazado && ExpCancels(expDirectRejected.Requests) == 0,
    "an already rejected payment whose provider order is canceled is never canceled again");

// Cancelación aceptada aunque la respuesta omita status_detail.
var expNoDetail = ExpBuild(EstadoPago.Pendiente, expExpiredClock, _ => ExpOrderJson("created"),
    (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", checkout: false)));
Check((await expNoDetail.Service.SincronizarAsync("passenger", "IC-confirm")).Pago.Estado == EstadoPago.Rechazado
    && ExpCancels(expNoDetail.Requests) == 1,
    "a verified order whose cancel response is canceled without status_detail is still persisted as Rechazado");

// 2. Hold vigente: nunca se cancela.
var expLive = ExpBuild(EstadoPago.Pendiente, expLiveClock, _ => ExpOrderJson("created"), ExpCanceledOk);
var expLiveResponse = await expLive.Service.SincronizarAsync("passenger", "IC-confirm");
Check(ExpCancels(expLive.Requests) == 0 && expLiveResponse.Pago.Estado == EstadoPago.Pendiente
    && expLiveResponse.Pago.CheckoutUrl == expCheckoutUrl && expLive.Reconciled.Single().Estado == EstadoPago.Pendiente,
    "live hold with a created order never cancels, stays Pendiente and keeps the checkout URL");

// 3. Fallos al cancelar: se conserva el comportamiento actual (sin degradar el estado).
var expFailures = new (string Name, Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> Cancel)[]
{
    ("HTTP 500", (_, _) => JsonResponse(HttpStatusCode.InternalServerError, "{\"message\":\"boom\"}")),
    ("HTTP 400 not cancelable", (_, _) => JsonResponse(HttpStatusCode.BadRequest, "{\"errors\":[]}")),
    ("network error", (_, _) => throw new HttpRequestException("offline")),
    ("timeout", (_, _) => throw new TaskCanceledException("timeout", new TimeoutException())),
    ("malformed body", (_, _) => JsonResponse(HttpStatusCode.OK, "not-json")),
    ("still created", (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("created"))),
    ("processed instead of canceled", (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("processed", "accredited", "350.00"))),
    ("different order id", (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", "canceled_transaction", id: "ORD-OTHER"))),
    ("different reference", (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", "canceled_transaction", reference: "other-reference"))),
    ("different amount", (_, _) => JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", "canceled_transaction", amount: "10.00")))
};
foreach (var failure in expFailures)
{
    var failedRun = ExpBuild(EstadoPago.Pendiente, expExpiredClock, _ => ExpOrderJson("created"), failure.Cancel);
    var failedResponse = await failedRun.Service.SincronizarAsync("passenger", "IC-confirm");
    Check(ExpCancels(failedRun.Requests) == 1 && failedResponse.Pago.Estado == EstadoPago.Pendiente
        && failedResponse.Pago.CheckoutUrl is null && failedRun.Reconciled.Count == 1
        && failedRun.Reconciled.Single().Estado == EstadoPago.Pendiente && failedRun.Reconciled.Single().ImportePagado == 0m,
        $"failed cancel ({failure.Name}) keeps today's behavior: still Pendiente, never Rechazado");
}

// El estado Desconocido tampoco se degrada cuando falla la cancelación.
var expUnknownFailure = ExpBuild(EstadoPago.Desconocido, expExpiredClock, _ => ExpOrderJson("created"),
    (_, _) => JsonResponse(HttpStatusCode.InternalServerError, "{}"));
Check((await expUnknownFailure.Service.SincronizarAsync("passenger", "IC-confirm")).Pago.Estado == EstadoPago.Pendiente
    && expUnknownFailure.Reconciled.Single().Estado == EstadoPago.Pendiente,
    "failed cancel reconciles the order as read (Pendiente) without inventing a rejection");

// Cancelación del llamador durante la cancelación: se propaga y no cambia el estado.
using (var expCaller = new CancellationTokenSource())
{
    var callerRun = ExpBuild(EstadoPago.Pendiente, expExpiredClock, _ => ExpOrderJson("created"),
        (_, _) => { expCaller.Cancel(); throw new OperationCanceledException(expCaller.Token); });
    await CheckThrowsAsync<OperationCanceledException>(
        () => callerRun.Service.SincronizarAsync("passenger", "IC-confirm", expCaller.Token),
        "caller cancellation while canceling propagates");
    Check(callerRun.Repository.ConciliateCalls == 0 && callerRun.Reconciled.Count == 0,
        "caller cancellation while canceling does not change the persisted payment state");
}

// 4. Orden con pago o en curso: no se cancela y la ruta de aprobación tardía queda igual.
var expApproved = ExpBuild(EstadoPago.Pendiente, expExpiredClock,
    _ => ExpOrderJson("processed", "accredited", "350.00"), ExpCanceledOk);
var expApprovedResponse = await expApproved.Service.SincronizarAsync("passenger", "IC-confirm");
Check(ExpCancels(expApproved.Requests) == 0 && ExpRefunds(expApproved.Requests) == 1
    && expApprovedResponse.Pago.Estado == EstadoPago.DevolucionPendiente
    && expApproved.Reconciled.All(o => o.Estado == EstadoPago.Aprobado),
    "expired hold with an accredited order never cancels and keeps the late-approval refund follow-up");
foreach (var inFlight in new[]
{
    ("processing", "in_process", "0.00"),
    ("processing", "pending_review_manual", "0.00"),
    ("action_required", "waiting_capture", "0.00")
})
{
    var inFlightRun = ExpBuild(EstadoPago.Pendiente, expExpiredClock,
        _ => ExpOrderJson(inFlight.Item1, inFlight.Item2, inFlight.Item3), ExpCanceledOk);
    var inFlightResponse = await inFlightRun.Service.SincronizarAsync("passenger", "IC-confirm");
    Check(ExpCancels(inFlightRun.Requests) == 0 && inFlightResponse.Pago.Estado == EstadoPago.Pendiente
        && inFlightRun.Reconciled.Single().Estado == EstadoPago.Pendiente,
        $"expired hold with a provider order in {inFlight.Item1}/{inFlight.Item2} is never canceled");
}

// Defensa del servicio ante una orden devuelta que no verifica (cliente simulado).
FakeMercadoPagoCheckoutClient ExpFakeGateway(OrdenCheckoutMercadoPago? cancelResponse) => new()
{
    QueryResponses = [ConfirmationOrder(EstadoPago.Pendiente, 0m, "order-stored")],
    CancelResponse = cancelResponse
};
FakeConfirmationRepository ExpFakeRepository() => new(ConfirmationResult(EstadoPago.Pendiente, 0m, "order-stored", 350m,
    confirmationHoldExpiryUtc, null, false))
{
    OnConciliate = (order, _) => ConfirmationResult(order.Estado, order.ImportePagado, order.Id, 350m,
        confirmationHoldExpiryUtc, null, false)
};
var expFakeOk = ExpFakeGateway(ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-stored"));
var expFakeOkResponse = await new ConciliacionesPagoService(ExpFakeRepository(), expFakeOk, expExpiredClock)
    .SincronizarAsync("passenger", "IC-confirm");
Check(expFakeOkResponse.Pago.Estado == EstadoPago.Rechazado && expFakeOk.Events.SequenceEqual(["get", "cancel"])
    && expFakeOk.LastCancelKey == ConciliacionesPagoService.ClaveCancelacion(expCreationKey),
    "expired hold: provider read then cancel with the payment-derived key, in that order");
foreach (var unverified in new[]
{
    ("still pending", ConfirmationOrder(EstadoPago.Pendiente, 0m, "order-stored")),
    ("approved", ConfirmationOrder(EstadoPago.Aprobado, 350m, "order-stored")),
    ("different order", ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-other")),
    ("different reference", ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-stored") with { ReferenciaExterna = "other" }),
    ("different amount", ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-stored") with { Importe = 10m }),
    ("different currency", ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-stored") with { Moneda = "USD" }),
    ("paid amount", ConfirmationOrder(EstadoPago.Rechazado, 100m, "order-stored"))
})
{
    var unverifiedGateway = ExpFakeGateway(unverified.Item2);
    var unverifiedRepository = ExpFakeRepository();
    var unverifiedResponse = await new ConciliacionesPagoService(unverifiedRepository, unverifiedGateway, expExpiredClock)
        .SincronizarAsync("passenger", "IC-confirm");
    Check(unverifiedGateway.CancelCalls == 1 && unverifiedResponse.Pago.Estado == EstadoPago.Pendiente
        && unverifiedRepository.ConciliateCalls == 1,
        $"a cancel result that does not verify ({unverified.Item1}) is ignored and the order is reconciled as read");
}
var expLiveFake = ExpFakeGateway(ConfirmationOrder(EstadoPago.Rechazado, 0m, "order-stored"));
await new ConciliacionesPagoService(ExpFakeRepository(), expLiveFake, expLiveClock).SincronizarAsync("passenger", "IC-confirm");
Check(expLiveFake.CancelCalls == 0, "live hold never reaches the cancel call");

// Clave de idempotencia estable derivada de la ClaveCreacion persistida.
Check(ConciliacionesPagoService.ClaveCancelacion(expCreationKey) == Guid.Parse("baa6b3be-e75f-55fa-a3f9-c112607f08c2")
    && ConciliacionesPagoService.ClaveCancelacion(expCreationKey) == ConciliacionesPagoService.ClaveCancelacion(expCreationKey)
    && ConciliacionesPagoService.ClaveCancelacion(expCreationKey) != expCreationKey
    && ConciliacionesPagoService.ClaveCancelacion(expCreationKey) != Guid.Parse("33333333-3333-3333-3333-333333333333")
    && ConciliacionesPagoService.ClaveCancelacion(Guid.Parse("22222222-2222-2222-2222-222222222222"))
        != ConciliacionesPagoService.ClaveCancelacion(expCreationKey)
    && ConciliacionesPagoService.ClaveCancelacion(expCreationKey).ToString("D")[14] == '5',
    "cancel idempotency key is a deterministic UUID v5 of the persisted creation key, distinct per payment and from creation/refund keys");

// Contrato del cliente: endpoint, cabeceras, sin cuerpo y una sola verificación de identidad.
var expClientRequests = new List<HttpRequestMessageSnapshot>();
var expClientHandler = new MockHttpMessageHandler((request, _) =>
{
    expClientRequests.Add(new HttpRequestMessageSnapshot(request.Method.Method, request.RequestUri!.AbsolutePath,
        ExpHeaderKey(request), request.Content is not null));
    return request.Method == HttpMethod.Post
        ? JsonResponse(HttpStatusCode.OK, ExpOrderJson("canceled", "canceled_transaction", checkout: false))
        : JsonResponse(HttpStatusCode.OK, ExpOrderJson("created"));
});
var expClientKey = Guid.Parse("44444444-4444-4444-4444-444444444444");
var expCanceledOrder = await CreateCheckoutClient(expClientHandler)
    .CancelarOrdenAsync(expOrderId, expClientKey, confirmationReference, 350m);
var expClientPost = expClientRequests.FirstOrDefault(r => r.Method == "POST") ?? new HttpRequestMessageSnapshot("none", "", null, false);
Check(expClientPost.Path == "/v1/orders/ORD-REAL-1/cancel" && !expClientPost.HasBody
    && expClientPost.IdempotencyKey == expClientKey && expClientHandler.IdentityRequestCount == 1
    && expCanceledOrder.Estado == EstadoPago.Rechazado && expCanceledOrder.Id == expOrderId && expCanceledOrder.ImportePagado == 0m,
    "client cancel verifies the TEST identity once, POSTs the official cancel endpoint with the caller's key and no body");
await CheckThrowsAsync<ArgumentException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, "{}")))
        .CancelarOrdenAsync(expOrderId, Guid.Empty, confirmationReference, 350m),
    "client cancel rejects an empty idempotency key");
foreach (var notCancelable in new (string Status, string? Detail, string Paid)[]
{
    ("processed", "accredited", "350.00"),
    ("processing", "in_process", "0.00"),
    ("action_required", "waiting_capture", "0.00"),
    ("created", null, "50.00")
})
{
    var posts = 0;
    var notCancelableHandler = new MockHttpMessageHandler((request, _) =>
    {
        if (request.Method == HttpMethod.Post) posts++;
        return JsonResponse(HttpStatusCode.OK, ExpOrderJson(notCancelable.Status, notCancelable.Detail, notCancelable.Paid));
    });
    await CheckThrowsAsync<InvalidOperationException>(
        () => CreateCheckoutClient(notCancelableHandler).CancelarOrdenAsync(expOrderId, expClientKey, confirmationReference, 350m),
        $"client cancel refuses an order in {notCancelable.Status}/{notCancelable.Detail} with paid {notCancelable.Paid}");
    Check(posts == 0, $"client never POSTs a cancel for an order in {notCancelable.Status}/{notCancelable.Detail}");
}

// 5. Mapeo de estados del proveedor.
foreach (var mapping in new (string Status, string? Detail, string? Paid, EstadoPago Expected)[]
{
    ("canceled", "canceled_transaction", null, EstadoPago.Rechazado),
    ("canceled", "canceled_transaction", "0.00", EstadoPago.Rechazado),
    ("canceled", "canceled", "0.00", EstadoPago.Rechazado),
    ("canceled", "unexpected_detail", "0.00", EstadoPago.Desconocido),
    ("created", null, null, EstadoPago.Pendiente),
    ("created", "created", "0.00", EstadoPago.Pendiente),
    ("processing", "in_process", "0.00", EstadoPago.Pendiente),
    ("processed", "accredited", "350.00", EstadoPago.Aprobado),
    ("failed", "processing_error", "0.00", EstadoPago.Rechazado),
    ("processed", "refunded", "350.00", EstadoPago.Devuelto)
})
{
    var mapped = await CreateCheckoutClient(new MockHttpMessageHandler((_, _) =>
            JsonResponse(HttpStatusCode.OK, ExpOrderJson(mapping.Status, mapping.Detail, mapping.Paid))))
        .ConsultarOrdenAsync(expOrderId, confirmationReference, 350m);
    Check(mapped.Estado == mapping.Expected,
        $"provider status {mapping.Status}/{mapping.Detail ?? "(none)"} maps to {mapping.Expected}");
}

var syncEndpoint = typeof(ConciliacionesPagoController).GetMethod(nameof(ConciliacionesPagoController.Sincronizar));
var syncRoute = typeof(ConciliacionesPagoController).GetCustomAttributes(typeof(RouteAttribute), true)
    .Cast<RouteAttribute>().SingleOrDefault()?.Template;
Check(syncRoute == "api/intentos-compra/{intentoId}/pagos"
    && syncEndpoint?.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>()
        .SingleOrDefault()?.Template == "sincronizar"
    && syncEndpoint.GetParameters().Length == 2,
    "sync endpoint is a bodyless authenticated POST, so clients cannot assert payment approval");
var unknownControllerService = new FakeConciliacionesPagoService
{
    Response = new SincronizarPagoResponseDto { Pago = new PagoResponseDto { Estado = EstadoPago.Desconocido } }
};
var unknownController = CreateSyncController(unknownControllerService, "passenger-public-id");
var unknownHttpResult = await unknownController.Sincronizar("IC-opaque");
Check(unknownHttpResult.Result is ObjectResult { StatusCode: 202 }
    && unknownControllerService.Owner == "passenger-public-id"
    && unknownControllerService.Attempt == "IC-opaque",
    "unknown local payment maps to 202 and the controller supplies only the authenticated passenger identity");
var knownControllerService = new FakeConciliacionesPagoService
{
    Response = new SincronizarPagoResponseDto { Pago = new PagoResponseDto { Estado = EstadoPago.Aprobado } }
};
var knownHttpResult = await CreateSyncController(knownControllerService, "passenger-public-id").Sincronizar("IC-opaque");
Check(knownHttpResult.Result is OkObjectResult,
    "known synchronized payment maps to 200 without requiring client-provided payment state");
var noIdentitySyncResult = await CreateSyncController(new FakeConciliacionesPagoService(), null).Sincronizar("IC-opaque");
Check(noIdentitySyncResult.Result is UnauthorizedResult,
    "missing authenticated identity is rejected before payment synchronization");
var foreignControllerService = new FakeConciliacionesPagoService { Exception = new KeyNotFoundException() };
var foreignHttpResult = await CreateSyncController(foreignControllerService, "passenger-public-id").Sincronizar("IC-foreign");
Check(foreignHttpResult.Result is NotFoundResult,
    "missing or foreign attempt maps to 404 without exposing ownership details");
var internalSyncService = new FakeConciliacionesPagoService
{
    Exception = new InvalidOperationException("sensitive provider detail PG-1 credential")
};
var internalSyncResult = await CreateSyncController(internalSyncService, "passenger-public-id").Sincronizar("IC-opaque");
Check(internalSyncResult.Result is ObjectResult { StatusCode: StatusCodes.Status500InternalServerError } internalSyncFailure
    && Equals(internalSyncFailure.Value, "Error interno del servidor")
    && !internalSyncFailure.Value!.ToString()!.Contains("sensitive provider detail", StringComparison.Ordinal),
    "internal InvalidOperationException during synchronization maps to a sanitized 500, not a 400 with its message");
var canceledControllerService = new FakeConciliacionesPagoService { Exception = new OperationCanceledException() };
using var canceledControllerToken = new CancellationTokenSource();
canceledControllerToken.Cancel();
await CheckThrowsAsync<OperationCanceledException>(
    () => CreateSyncController(canceledControllerService, "passenger-public-id").Sincronizar("IC-opaque", canceledControllerToken.Token),
    "synchronization controller propagates caller cancellation");

// T2 / S3f: authenticated passenger endpoint for the most recent attempt that still needs attention.
var pendingAttempts = new FakeIntentosCompraService
{
    ReadResponse = new IniciarIntentoCompraResponseDto { IntentoId = "IC-pending", HoldVenceEn = quoteExpiry }
};
var pendingPayments = new FakePagosService
{
    CurrentResponse = new PagoResponseDto { PagoId = "PG-9", IntentoId = "IC-pending", Estado = EstadoPago.Desconocido }
};
var pendingRepository = new FakeIntentosPendientesRepository(new Dictionary<string, string> { ["owner-public-id"] = "IC-pending" });
var pendingService = new IntentosPendientesService(pendingRepository, pendingAttempts, pendingPayments, paymentNow);
var ownerPending = await pendingService.ConsultarAsync("owner-public-id");
Check(ownerPending is not null && ownerPending.Intento.IntentoId == "IC-pending"
    && ownerPending.Pago?.Estado == EstadoPago.Desconocido
    && pendingAttempts.ReadPassengerId == "owner-public-id" && pendingAttempts.ReadAttemptId == "IC-pending"
    && pendingPayments.PassengerId == "owner-public-id" && pendingPayments.AttemptId == "IC-pending"
    && pendingRepository.LastNowUtc == paymentNow.GetUtcNow().UtcDateTime,
    "pending-attempt service returns the owner's attempt and current payment using the server clock");
var otherPendingAttempts = new FakeIntentosCompraService();
var otherPending = await new IntentosPendientesService(pendingRepository, otherPendingAttempts, pendingPayments, paymentNow)
    .ConsultarAsync("other-public-id");
Check(otherPending is null && otherPendingAttempts.ReadPassengerId is null,
    "another passenger gets no pending attempt and never reads the owner's attempt");
await CheckThrowsAsync<ArgumentException>(() => pendingService.ConsultarAsync(" "),
    "pending-attempt service rejects a blank passenger identity");

var pendingController = CreatePendingController(new FakeIntentosPendientesService(ownerPending), "owner-public-id");
Check((await pendingController.Consultar()).Result is OkObjectResult { Value: IntentoPendienteResponseDto },
    "pending-attempt endpoint returns 200 with the attempt and payment for the owner");
Check((await CreatePendingController(new FakeIntentosPendientesService(null), "other-public-id").Consultar()).Result is NoContentResult,
    "pending-attempt endpoint returns 204 when nothing needs attention");
Check((await CreatePendingController(new FakeIntentosPendientesService(null), null).Consultar()).Result is UnauthorizedResult,
    "pending-attempt endpoint rejects a missing authenticated identity");
var failingPending = await CreatePendingController(
    new FakeIntentosPendientesService(null) { Exception = new InvalidOperationException("sensitive detail") },
    "owner-public-id").Consultar();
Check(failingPending.Result is ObjectResult { StatusCode: StatusCodes.Status500InternalServerError } pendingFailure
    && Equals(pendingFailure.Value, "Error interno del servidor"),
    "pending-attempt endpoint sanitizes unexpected failures as 500");
Check(typeof(IntentosPendientesController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>()
        .Single().Template == "api/intentos-compra/pendiente"
    && typeof(IntentosPendientesController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()
        .Single().Roles == "Pasajero"
    && typeof(IntentosPendientesController).GetMethod(nameof(IntentosPendientesController.Consultar))!
        .GetCustomAttributes(typeof(HttpGetAttribute), true).Length == 1,
    "pending-attempt endpoint is an authenticated passenger-only GET at the documented route");

Console.WriteLine($"Checks: {checks}; failures: {failures}");
return failures == 0 ? 0 : 1;

static IntentosCompraController CreateController(IIntentosCompraService service, string? passengerId)
{
    var claims = passengerId is null
        ? Array.Empty<Claim>()
        : [new Claim(ClaimTypes.NameIdentifier, passengerId)];
    return new IntentosCompraController(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        }
    };
}

static CotizacionesCompraController CreateQuoteController(ICotizacionesCompraService service, string? passengerId)
{
    var claims = passengerId is null
        ? Array.Empty<Claim>()
        : [new Claim(ClaimTypes.NameIdentifier, passengerId)];
    return new CotizacionesCompraController(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        }
    };
}

static PagosController CreatePaymentController(IPagosService service, string? passengerId)
{
    var claims = passengerId is null
        ? Array.Empty<Claim>()
        : [new Claim(ClaimTypes.NameIdentifier, passengerId)];
    return new PagosController(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        }
    };
}

static IntentosPendientesController CreatePendingController(IIntentosPendientesService service, string? passengerId)
{
    var claims = passengerId is null
        ? Array.Empty<Claim>()
        : [new Claim(ClaimTypes.NameIdentifier, passengerId)];
    return new IntentosPendientesController(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        }
    };
}

static ConciliacionesPagoController CreateSyncController(IConciliacionesPagoService service, string? passengerId)
{
    var claims = passengerId is null
        ? Array.Empty<Claim>()
        : [new Claim(ClaimTypes.NameIdentifier, passengerId)];
    return new ConciliacionesPagoController(service)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        }
    };
}

static ItinerarioDto CreateQuote(decimal firstPrice, decimal secondPrice)
{
    var start = new DateTime(2026, 11, 16, 8, 0, 0);
    return new ItinerarioDto
    {
        Salida = start,
        Llegada = start.AddHours(2),
        PrecioTotal = firstPrice + secondPrice,
        Transbordo = new TransbordoDto { Parada = new ParadaDto { Id = "P2", Nombre = "Middle" }, Minutos = 15 },
        Tramos =
        [
            new() { Orden = 1, ServicioId = "S1", TramoId = "T1", Origen = new() { Id = "P1", Nombre = "Origin" }, Destino = new() { Id = "P2", Nombre = "Middle" }, Salida = start, Llegada = start.AddMinutes(45), Importe = firstPrice, PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica },
            new() { Orden = 2, ServicioId = "S2", TramoId = "T2", Origen = new() { Id = "P2", Nombre = "Middle" }, Destino = new() { Id = "P3", Nombre = "Destination" }, Salida = start.AddHours(1), Llegada = start.AddHours(2), Importe = secondPrice, PoliticaAsiento = PoliticaAsiento.SinAsiento }
        ]
    };
}

static ItinerarioDto CloneQuote(ItinerarioDto source) => new()
{
    Salida = source.Salida,
    Llegada = source.Llegada,
    PrecioTotal = source.PrecioTotal,
    Transbordo = source.Transbordo is { } transbordo ? new TransbordoDto
    {
        Parada = new ParadaDto
        {
            Id = transbordo.Parada.Id,
            Nombre = transbordo.Parada.Nombre
        },
        Minutos = transbordo.Minutos
    } : null,
    Tramos = source.Tramos.Select(tramo => new TramoItinerarioDto
    {
        Orden = tramo.Orden,
        ServicioId = tramo.ServicioId,
        TramoId = tramo.TramoId,
        Origen = new ParadaDto { Id = tramo.Origen.Id, Nombre = tramo.Origen.Nombre },
        Destino = new ParadaDto { Id = tramo.Destino.Id, Nombre = tramo.Destino.Nombre },
        Salida = tramo.Salida,
        Llegada = tramo.Llegada,
        Importe = tramo.Importe,
        PoliticaAsiento = tramo.PoliticaAsiento
    }).ToList()
};

static PagosService CreatePaymentService(
    FakePagosRepository repository,
    FakeCotizacionesCompraService quotes,
    IMercadoPagoCheckoutClient gateway,
    TimeProvider timeProvider)
{
    repository.NowUtc = timeProvider.GetUtcNow().UtcDateTime;
    return new PagosService(repository, quotes, gateway, timeProvider);
}

static RegistroPago CreatePaymentRecord(string id, EstadoPago state, DateTime holdExpiry, string? checkoutUrl)
{
    var payment = new Pago
    {
        Id = id,
        IntentoId = "IC-123",
        ImporteEsperado = 350m,
        Moneda = "UYU",
        Estado = state,
        ClaveCreacion = Guid.NewGuid(),
        CheckoutUrl = checkoutUrl
    };
    return new RegistroPago(payment, holdExpiry, false);
}

static MockHttpMessageHandler CreateRealShapeHandler(Action onPost, string checkoutUrl, Func<string> currentReference)
{
    static string RealOrder(string reference, string? checkoutUrl) =>
        "{\"id\":\"ORD-REAL-1\",\"type\":\"online\",\"processing_mode\":\"manual\",\"external_reference\":\"" + reference
        + "\",\"total_amount\":\"350.00\"" + (checkoutUrl is null ? "" : ",\"checkout_url\":\"" + checkoutUrl + "\"")
        + ",\"country_code\":\"URY\",\"user_id\":\"987654321\",\"status\":\"created\",\"capture_mode\":\"automatic_async\","
        + "\"currency\":\"UYU\",\"created_date\":\"2026-11-16T14:00:01.000Z\",\"last_updated_date\":\"2026-11-16T14:00:01.000Z\","
        + "\"integration_data\":{\"application_id\":\"1\"},\"items\":[]}";
    return new MockHttpMessageHandler((request, _) =>
    {
        if (request.Method == HttpMethod.Post)
        {
            onPost();
            var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement;
            return JsonResponse(HttpStatusCode.Created, RealOrder(body.GetProperty("external_reference").GetString()!, checkoutUrl));
        }
        if (request.RequestUri!.Query.Contains("external_reference=", StringComparison.Ordinal))
            return JsonResponse(HttpStatusCode.OK, "{\"data\":[" + RealOrder(currentReference(), null)
                + "],\"paging\":{\"total\":1,\"total_pages\":1,\"offset\":0,\"limit\":2}}");
        return JsonResponse(HttpStatusCode.OK, RealOrder(currentReference(), checkoutUrl));
    });
}

async Task<TException> CaptureExceptionAsync<TException>(Func<Task> action) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name} was not thrown.");
}

void CheckThrows<TException>(Action action, string name) where TException : Exception
{
    try
    {
        action();
        Check(false, name);
    }
    catch (TException)
    {
        Check(true, name);
    }
}

static MercadoPagoCheckoutClient CreateCheckoutClient(MockHttpMessageHandler handler, string token = "TEST-contract-token")
{
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["MercadoPago:AccessToken"] = token,
        ["MercadoPago:TestCredentialOriginConfirmed"] = "true",
        ["MercadoPago:ExpectedSellerId"] = "987654321",
        ["MercadoPago:ExpectedSiteId"] = "SITE_TEST_FIXTURE"
    }).Build();
    handler.IdentityResponseFactory ??= (_, _) => JsonResponse(HttpStatusCode.OK,
        "{\"id\":987654321,\"site_id\":\"SITE_TEST_FIXTURE\",\"tags\":[\"test_user\"]}");
    return new MercadoPagoCheckoutClient(new HttpClient(handler), ParametrosMercadoPago.Desde(configuration));
}

static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) => new(statusCode)
{
    Content = new StringContent(body, Encoding.UTF8, "application/json")
};

static TramoIntentoCompra ConfirmationSelection(int order, string serviceId, string segmentId,
    string originId, string originName, string destinationId, string destinationName, decimal originalAmount,
    PoliticaAsiento seatPolicy, int? seatNumber, DateTime departure, DateTime arrival) => new()
{
    IntentoId = "IC-confirm",
    Orden = order,
    ServicioId = serviceId,
    TramoId = segmentId,
    OrigenId = originId,
    OrigenNombre = originName,
    DestinoId = destinationId,
    DestinoNombre = destinationName,
    Salida = departure,
    Llegada = arrival,
    Importe = originalAmount,
    PoliticaAsiento = seatPolicy,
    NumeroAsiento = seatNumber,
    SalidaId = "SA-confirm",
    OrdenEmbarque = order,
    OrdenDesembarque = order + 1
};

static RegistroPago ConfirmationRecord(EstadoPago state, decimal paid, DateTime holdExpiry, string? providerOrderId = "order-stored") =>
    new(new Pago
    {
        Id = "PG-confirm",
        IntentoId = "IC-confirm",
        ImporteEsperado = 350m,
        ImportePagado = paid,
        Moneda = "UYU",
        Estado = state,
        ClaveCreacion = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ClaveDevolucion = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        OrdenProveedorId = providerOrderId,
        CreadoUtc = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc),
        ActualizadoUtc = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc)
    }, holdExpiry, false);

static ResultadoConciliacionPago ConfirmationResult(EstadoPago state, decimal paid, string? providerOrderId,
    decimal expected, DateTime holdExpiry, CompraConfirmadaResponseDto? purchase, bool refundRequired) =>
    new(ConfirmationRecord(state, paid, holdExpiry, providerOrderId) with
    {
        Pago = new Pago
        {
            Id = "PG-confirm",
            IntentoId = "IC-confirm",
            ImporteEsperado = expected,
            ImportePagado = paid,
            Moneda = "UYU",
            Estado = state,
            ClaveCreacion = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ClaveDevolucion = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            OrdenProveedorId = providerOrderId,
            CreadoUtc = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc),
            ActualizadoUtc = new DateTime(2026, 11, 16, 14, 0, 0, DateTimeKind.Utc)
        }
    }, purchase, refundRequired);

static OrdenCheckoutMercadoPago ConfirmationOrder(EstadoPago state, decimal paid, string orderId) => new()
{
    Id = orderId,
    ReferenciaExterna = confirmationReference,
    Importe = 350m,
    ImportePagado = paid,
    Moneda = "UYU",
    Estado = state,
    CheckoutUrl = "https://www.mercadopago.com.uy/checkout/confirmed"
};

async Task CheckThrowsAsync<TException>(Func<Task> action, string name) where TException : Exception
{
    try
    {
        await action();
        Check(false, name);
    }
    catch (TException)
    {
        Check(true, name);
    }
}

}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.ToString());
    return 1;
}

sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
    public int RequestCount { get; private set; }
    public int IdentityRequestCount { get; private set; }
    public Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>? IdentityResponseFactory { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.Host == "api.mercadolibre.com"
            && request.RequestUri.AbsolutePath == "/users/me"
            && IdentityResponseFactory is not null)
        {
            IdentityRequestCount++;
            return Task.FromResult(IdentityResponseFactory(request, cancellationToken));
        }
        RequestCount++;
        return Task.FromResult(responseFactory(request, cancellationToken));
    }
}

sealed class FakePagosService : IPagosService
{
    public PagoResponseDto Response { get; set; } = new();
    public PagoResponseDto? CurrentResponse { get; set; }
    public Exception? Exception { get; set; }
    public string? PassengerId { get; private set; }
    public string? AttemptId { get; private set; }
    public IniciarPagoRequestDto? Request { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public int Calls { get; private set; }

    public Task<PagoResponseDto> IniciarAsync(
        string identificadorPublico,
        string intentoId,
        IniciarPagoRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        PassengerId = identificadorPublico;
        AttemptId = intentoId;
        Request = solicitud;
        CancellationToken = cancellationToken;
        if (Exception is not null) return Task.FromException<PagoResponseDto>(Exception);
        return Task.FromResult(Response);
    }

    public Task<PagoResponseDto?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        PassengerId = identificadorPublico;
        AttemptId = intentoId;
        CancellationToken = cancellationToken;
        if (Exception is not null) return Task.FromException<PagoResponseDto?>(Exception);
        return Task.FromResult(CurrentResponse);
    }
}

sealed class FakePagosRepository : IPagosRepository
{
    private readonly object _sync = new();
    private int _nextId = 1;

    public FakePagosRepository(RegistroPago? current = null) => Current = current;

    public RegistroPago? Current { get; private set; }
    public OrdenCheckoutMercadoPago? LastOrder { get; private set; }
    public Exception? ConsultException { get; set; }
    public DateTime NowUtc { get; set; } = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    public int ConsultCalls { get; private set; }
    public int ReserveCalls { get; private set; }
    public int NewReservationCount { get; private set; }
    public List<Pago> History { get; } = [];

    public Task<RegistroPago?> ConsultarActualAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsultCalls++;
        if (ConsultException is not null)
            return Task.FromException<RegistroPago?>(ConsultException);
        return Task.FromResult(Current);
    }

    public Task<RegistroPago> ReservarAsync(
        string identificadorPublico,
        CotizarCompraResponseDto cotizacion,
        string huellaAceptada,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (Current is not null && Current.Pago.Estado != EstadoPago.Rechazado)
                return Task.FromResult(Current with { EsNuevo = false });

            var intento = new IniciarIntentoCompraResponseDto
            {
                IntentoId = cotizacion.IntentoId,
                Itinerario = cotizacion.CotizacionOriginal,
                HoldVenceEn = cotizacion.HoldVenceEn
            };
            ReglasInicioPago.ValidarReserva(intento, cotizacion, huellaAceptada, NowUtc);

            ReserveCalls++;
            var payment = new Pago
            {
                Id = $"P-{_nextId++}",
                IntentoId = cotizacion.IntentoId,
                ImporteEsperado = cotizacion.CotizacionActual.PrecioTotal,
                Moneda = "UYU",
                Estado = EstadoPago.Desconocido,
                ClaveCreacion = Guid.NewGuid()
            };
            History.Add(payment);
            Current = new RegistroPago(payment, cotizacion.HoldVenceEn, true);
            NewReservationCount++;
            return Task.FromResult(Current);
        }
    }

    public Task<RegistroPago> RegistrarOrdenAsync(
        string identificadorPublico,
        string pagoId,
        OrdenCheckoutMercadoPago orden,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (Current is null || Current.Pago.Id != pagoId)
                throw new InvalidOperationException("The fake payment reservation is unavailable.");
            LastOrder = orden;
            Current.Pago.OrdenProveedorId = orden.Id;
            Current.Pago.CheckoutUrl = orden.CheckoutUrl;
            Current.Pago.Estado = ReglasInicioPago.EstadoTrasCrearOrden(Current.Pago.Estado, Current.Pago.ImportePagado, orden);
            Current.Pago.ActualizadoUtc = DateTime.UtcNow;
            return Task.FromResult(Current with { EsNuevo = false });
        }
    }

    public int RejectWithoutOrderCalls { get; private set; }

    public Task<RegistroPago> RechazarSinOrdenAsync(
        string identificadorPublico,
        string pagoId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (Current is null || Current.Pago.Id != pagoId)
                throw new InvalidOperationException("The fake payment reservation is unavailable.");
            RejectWithoutOrderCalls++;
            if (Current.Pago.Estado == EstadoPago.Desconocido && Current.Pago.OrdenProveedorId is null
                && Current.Pago.ImportePagado == 0m)
                Current.Pago.Estado = EstadoPago.Rechazado;
            return Task.FromResult(Current with { EsNuevo = false });
        }
    }
}

sealed class FakeMercadoPagoCheckoutClient : IMercadoPagoCheckoutClient
{
    private int _createCalls;
    private int _queryCalls;
    private int _refundCalls;

    public OrdenCheckoutMercadoPago CreateResponse { get; set; } = new()
    {
        Id = "order-default",
        ReferenciaExterna = "P-1",
        Importe = 350m,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://www.mercadopago.com.uy/checkout/default"
    };
    public Exception? CreateException { get; set; }
    public string? ReferenceOverride { get; set; }
    public Action? BeforeCreate { get; set; }
    public int CreateCalls => Volatile.Read(ref _createCalls);
    public int QueryCalls => Volatile.Read(ref _queryCalls);
    public int RefundCalls => Volatile.Read(ref _refundCalls);
    public decimal? LastAmount { get; private set; }
    public string? LastReference { get; private set; }
    public Guid? LastIdempotencyKey { get; private set; }
    public IReadOnlyList<OrdenCheckoutMercadoPago> QueryResponses { get; set; } = [];
    public Exception? QueryException { get; set; }
    public OrdenCheckoutMercadoPago? SearchResponse { get; set; }
    public Exception? SearchException { get; set; }
    public Exception? RefundException { get; set; }
    public List<string> Events { get; } = [];
    public int SearchCalls { get; private set; }
    public string? LastSearchReference { get; private set; }
    public DateTime? LastSearchStartUtc { get; private set; }
    public DateTime? LastSearchEndUtc { get; private set; }
    public decimal? LastSearchAmount { get; private set; }
    public string? LastQueryOrderId { get; private set; }
    public string? LastQueryReference { get; private set; }
    public decimal? LastQueryAmount { get; private set; }
    public string? LastRefundOrderId { get; private set; }
    public Guid? LastRefundKey { get; private set; }
    public int CancelCalls { get; private set; }
    public Guid? LastCancelKey { get; private set; }
    public OrdenCheckoutMercadoPago? CancelResponse { get; set; }
    public Exception? CancelException { get; set; }

    public Task<OrdenCheckoutMercadoPago> CancelarOrdenAsync(
        string ordenId,
        Guid claveIdempotencia,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        CancelCalls++;
        Events.Add("cancel");
        LastCancelKey = claveIdempotencia;
        cancellationToken.ThrowIfCancellationRequested();
        if (CancelException is not null)
            return Task.FromException<OrdenCheckoutMercadoPago>(CancelException);
        return Task.FromResult(CancelResponse
            ?? throw new NotSupportedException("No cancel response was configured."));
    }

    public Task<OrdenCheckoutMercadoPago> CrearOrdenAsync(
        Guid claveIdempotencia,
        string referenciaExterna,
        decimal importe,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _createCalls);
        BeforeCreate?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();
        LastIdempotencyKey = claveIdempotencia;
        LastReference = referenciaExterna;
        LastAmount = importe;
        return CreateException is null
            ? Task.FromResult(CreateResponse with { ReferenciaExterna = ReferenceOverride ?? referenciaExterna })
            : Task.FromException<OrdenCheckoutMercadoPago>(CreateException);
    }

    public Task<OrdenCheckoutMercadoPago> ConsultarOrdenAsync(
        string ordenId,
        string referenciaExternaEsperada,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        var call = Interlocked.Increment(ref _queryCalls);
        Events.Add("get");
        LastQueryOrderId = ordenId;
        LastQueryReference = referenciaExternaEsperada;
        LastQueryAmount = importeEsperado;
        cancellationToken.ThrowIfCancellationRequested();
        if (QueryException is not null)
            return Task.FromException<OrdenCheckoutMercadoPago>(QueryException);
        if (QueryResponses.Count == 0)
            return Task.FromException<OrdenCheckoutMercadoPago>(new NotSupportedException("No provider response was configured."));
        return Task.FromResult(QueryResponses[Math.Min(call - 1, QueryResponses.Count - 1)]);
    }

    public Task<OrdenCheckoutMercadoPago?> BuscarOrdenAsync(
        string referenciaExternaEsperada,
        DateTime desdeUtc,
        DateTime hastaUtc,
        decimal importeEsperado,
        CancellationToken cancellationToken = default)
    {
        SearchCalls++;
        Events.Add("search");
        LastSearchReference = referenciaExternaEsperada;
        LastSearchStartUtc = desdeUtc;
        LastSearchEndUtc = hastaUtc;
        LastSearchAmount = importeEsperado;
        cancellationToken.ThrowIfCancellationRequested();
        return SearchException is not null
            ? Task.FromException<OrdenCheckoutMercadoPago?>(SearchException)
            : Task.FromResult(SearchResponse);
    }

    public Task SolicitarDevolucionCompletaAsync(
        string ordenId,
        Guid claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _refundCalls);
        Events.Add("refund");
        LastRefundOrderId = ordenId;
        LastRefundKey = claveIdempotencia;
        cancellationToken.ThrowIfCancellationRequested();
        return RefundException is null ? Task.CompletedTask : Task.FromException(RefundException);
    }
}

sealed class FakeConfirmationRepository(ResultadoConciliacionPago? snapshot) : IConfirmacionesCompraRepository
{
    public ResultadoConciliacionPago? Snapshot { get; set; } = snapshot;
    public Func<OrdenCheckoutMercadoPago, int, ResultadoConciliacionPago>? OnConciliate { get; set; }
    public List<string> Events { get; } = [];
    public int ConciliateCalls { get; private set; }
    public string? LastOwnerId { get; private set; }
    public string? LastAttemptId { get; private set; }
    public string? LastPaymentId { get; private set; }

    public Task<ResultadoConciliacionPago?> ConsultarAsync(string identificadorPublico, string intentoId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Add("snapshot");
        LastOwnerId = identificadorPublico;
        LastAttemptId = intentoId;
        return Task.FromResult(Snapshot);
    }

    public Task<ResultadoConciliacionPago> ConciliarAsync(string identificadorPublico, string pagoId,
        OrdenCheckoutMercadoPago orden, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Events.Add("reconcile");
        LastOwnerId = identificadorPublico;
        LastPaymentId = pagoId;
        ConciliateCalls++;
        Snapshot = OnConciliate?.Invoke(orden, ConciliateCalls) ?? Snapshot
            ?? throw new InvalidOperationException("A reconciliation result must be configured.");
        return Task.FromResult(Snapshot);
    }
}

sealed class FakeIntentosPendientesRepository(IReadOnlyDictionary<string, string> attemptsByPassenger) : IIntentosPendientesRepository
{
    public DateTime? LastNowUtc { get; private set; }

    public Task<string?> BuscarIdPendienteAsync(string identificadorPublico, DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        LastNowUtc = ahoraUtc;
        return Task.FromResult(attemptsByPassenger.TryGetValue(identificadorPublico, out var id) ? id : null);
    }
}

sealed class FakeIntentosPendientesService(IntentoPendienteResponseDto? response) : IIntentosPendientesService
{
    public Exception? Exception { get; set; }

    public Task<IntentoPendienteResponseDto?> ConsultarAsync(string identificadorPublico,
        CancellationToken cancellationToken = default) =>
        Exception is null ? Task.FromResult(response) : Task.FromException<IntentoPendienteResponseDto?>(Exception);
}

sealed class FakeConciliacionesPagoService : IConciliacionesPagoService
{
    public SincronizarPagoResponseDto Response { get; set; } = new();
    public Exception? Exception { get; set; }
    public string? Owner { get; private set; }
    public string? Attempt { get; private set; }

    public Task<SincronizarPagoResponseDto> SincronizarAsync(string identificadorPublico, string intentoId,
        CancellationToken cancellationToken = default)
    {
        Owner = identificadorPublico;
        Attempt = intentoId;
        cancellationToken.ThrowIfCancellationRequested();
        return Exception is null ? Task.FromResult(Response) : Task.FromException<SincronizarPagoResponseDto>(Exception);
    }
}

sealed class FakeAtomicPurchaseStore
{
    private readonly SemaphoreSlim _attemptLock = new(1, 1);
    private PlanEmisionCompra? _published;

    public bool FailBeforePublish { get; init; }
    public int PublishedPurchaseCount => _published is null ? 0 : 1;
    public int PublishedTicketCount => _published?.Tickets.Count ?? 0;

    public async Task<(string CompraId, IReadOnlyList<string> QrValues)> ConfirmAsync(
        RegistroPago payment, IReadOnlyList<TramoPago> prices, IReadOnlyList<TramoIntentoCompra> selections,
        DateTime confirmedUtc)
    {
        await _attemptLock.WaitAsync();
        try
        {
            if (_published is null)
            {
                var decision = PoliticaConfirmacionCompra.Decidir(payment.Pago.Estado,
                    new DateTimeOffset(payment.HoldVenceEn, TimeSpan.Zero),
                    new DateTimeOffset(confirmedUtc, TimeSpan.Zero), disponibilidadConfirmada: true);
                if (decision != DecisionConfirmacion.Confirmar)
                    throw new InvalidOperationException("The payment is not eligible for purchase confirmation.");
                var plan = EmisionCompra.Preparar(payment, prices, selections, "CO-concurrent",
                    ["CS-concurrent-1", "CS-concurrent-2"], ["TK-concurrent-1", "TK-concurrent-2"], confirmedUtc);
                if (FailBeforePublish)
                    throw new InvalidOperationException("Injected pre-publish failure.");
                _published = plan;
            }

            return (_published.Compra.Id, _published.Tickets.Select(ticket => ticket.QrOpaco).ToArray());
        }
        finally
        {
            _attemptLock.Release();
        }
    }
}

sealed class FakeIntentosCompraService : IIntentosCompraService
{
    public IniciarIntentoCompraResponseDto CreateResponse { get; set; } = new();
    public IniciarIntentoCompraResponseDto ReadResponse { get; set; } = new();
    public Exception? CreateException { get; set; }
    public Exception? ReadException { get; set; }
    public string? CreatePassengerId { get; private set; }
    public string? ReadPassengerId { get; private set; }
    public string? ReadAttemptId { get; private set; }
    public IniciarIntentoCompraRequestDto? CreateRequest { get; private set; }
    public CancellationToken CreateCancellationToken { get; private set; }
    public CancellationToken ReadCancellationToken { get; private set; }
    public int CreateCalls { get; private set; }

    public Task<IniciarIntentoCompraResponseDto> CrearAsync(string identificadorPublico, IniciarIntentoCompraRequestDto solicitud, CancellationToken cancellationToken = default)
    {
        CreateCalls++;
        CreatePassengerId = identificadorPublico;
        CreateRequest = solicitud;
        CreateCancellationToken = cancellationToken;
        return CreateException is null ? Task.FromResult(CreateResponse) : Task.FromException<IniciarIntentoCompraResponseDto>(CreateException);
    }

    public Task<IniciarIntentoCompraResponseDto> ConsultarAsync(string identificadorPublico, string intentoId, CancellationToken cancellationToken = default)
    {
        ReadPassengerId = identificadorPublico;
        ReadAttemptId = intentoId;
        ReadCancellationToken = cancellationToken;
        return ReadException is null ? Task.FromResult(ReadResponse) : Task.FromException<IniciarIntentoCompraResponseDto>(ReadException);
    }
}

sealed class FakeItinerariosService : IItinerariosService
{
    public IReadOnlyList<ItinerarioDto> Offers { get; set; } = [];
    public Action? BeforeReturn { get; set; }
    public int SearchCount { get; private set; }
    public DateOnly LastDate { get; private set; }

    public Task<IReadOnlyList<ItinerarioDto>> BuscarAsync(string? origenId, string? destinoId, DateOnly fecha)
    {
        SearchCount++;
        LastDate = fecha;
        BeforeReturn?.Invoke();
        return Task.FromResult(Offers);
    }
}

sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => _utcNow;
    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
}

sealed class FakeCotizacionesCompraService : ICotizacionesCompraService
{
    public CotizarCompraResponseDto Response { get; set; } = new();
    public Exception? Exception { get; set; }
    public string? PassengerId { get; private set; }
    public string? AttemptId { get; private set; }
    public CancellationToken CancellationToken { get; private set; }
    public int Calls { get; private set; }

    public Task<CotizarCompraResponseDto> CotizarAsync(string identificadorPublico, string intentoId, CancellationToken cancellationToken = default)
    {
        Calls++;
        PassengerId = identificadorPublico;
        AttemptId = intentoId;
        CancellationToken = cancellationToken;
        return Exception is null ? Task.FromResult(Response) : Task.FromException<CotizarCompraResponseDto>(Exception);
    }
}

sealed record HttpRequestMessageSnapshot(string Method, string Path, Guid? IdempotencyKey, bool HasBody);
