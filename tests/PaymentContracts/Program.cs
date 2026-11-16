using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using API_Berruti.DTOs.Operational;

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

Console.WriteLine($"Checks: {checks}; failures: {failures}");
return failures == 0 ? 0 : 1;