using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Operational;
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
