using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Operational;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

List<ValidationResult> Validate(IniciarIntentoCompraRequestDto request)
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

CheckThrows<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)), "PROD-secret"),
    "production credential is rejected when client resolves");
CheckThrows<InvalidOperationException>(
    () => CreateCheckoutClient(new MockHttpMessageHandler((_, _) => JsonResponse(HttpStatusCode.OK, checkoutResponse)), ""),
    "missing TEST credential is rejected when client resolves");

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
        ["MercadoPago:AccessToken"] = token
    }).Build();
    return new MercadoPagoCheckoutClient(new HttpClient(handler), ParametrosMercadoPago.Desde(configuration));
}

static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string body) => new(statusCode)
{
    Content = new StringContent(body, Encoding.UTF8, "application/json")
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

sealed class MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        return Task.FromResult(responseFactory(request, cancellationToken));
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
