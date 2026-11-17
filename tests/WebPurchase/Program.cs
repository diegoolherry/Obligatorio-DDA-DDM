using System.Net;
using Web_App.Models.Authentication;
using Web_App.Models.Operational;
using Web_App.Models.Purchase;
using Web_App.Services.Authentication;
using Web_App.Services.Purchase;

try
{
    if (args.Length == 1 && args[0] == "--verify-failure-exit")
        throw new InvalidOperationException("CONTROLLED-TEST-HARNESS-FAILURE");

    var checks = new Checks();
    await checks.RunAsync();
    Console.WriteLine($"{checks.Count}/{checks.Count} Web purchase checks passed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.ToString());
    return 1;
}

sealed class Checks
{
    public int Count { get; private set; }
    private void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}.");
        Count++;
    }
    private void True(bool value, string name) { if (!value) throw new Exception(name); Count++; }

    public async Task RunAsync()
    {
        var attempt = """{"intentoId":"IC-01","itinerario":{"salida":"2026-11-18T09:00:00","llegada":"2026-11-18T10:00:00","precioTotal":125,"transbordo":null,"tramos":[{"orden":1,"servicioId":"SV-01","tramoId":"TR-01"}]},"holdVenceEn":"2026-11-18T09:15:00Z"}""";
        var quote = """{"intentoId":"IC-01","cotizacionOriginal":{"salida":"2026-11-18T09:00:00","llegada":"2026-11-18T10:00:00","precioTotal":125,"transbordo":null,"tramos":[]},"cotizacionActual":{"salida":"2026-11-18T09:00:00","llegada":"2026-11-18T10:00:00","precioTotal":125,"transbordo":null,"tramos":[]},"cambioPrecio":false,"huellaCotizacion":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","holdVenceEn":"2026-11-18T09:15:00Z"}""";
        var fake = new FakeSession(
            new(true, 201, attempt, "created"),
            new(true, 200, quote, "ok"),
            new(false, 409, null, "conflict"),
            new(true, 200, quote.Replace("\"cambioPrecio\":false", "\"cambioPrecio\":true").Replace("\"precioTotal\":125", "\"precioTotal\":130"), "ok"));
        var client = new CompraCliente(fake);
        var created = await client.CrearIntentoAsync([new() { Orden = 1, ServicioId = "SV-01" }, new() { Orden = 2, ServicioId = "SV-02" }]);
        Equal(TipoResultadoCompra.Exito, created.Tipo, "attempt create succeeds");
        Equal(HttpMethod.Post, fake.Calls[0].Metodo, "attempt method");
        Equal("/api/intentos-compra", fake.Calls[0].Ruta, "attempt path");
        True(fake.Calls[0].Json!.Contains("\"servicioId\":\"SV-01\""), "service identifiers are sent");
        Equal(TipoResultadoCompra.Exito, (await client.ObtenerCotizacionAsync("IC-01")).Tipo, "quote fetch");
        var changed = await client.IniciarPagoAsync("IC-01", new string('a', 64));
        Equal(TipoResultadoCompra.CotizacionModificada, changed.Tipo, "stale quote requires renewed consent");
        Equal(130m, changed.Cotizacion!.CotizacionActual.PrecioTotal, "fresh current quote returned");
        Equal("/api/intentos-compra/IC-01/pagos", fake.Calls[2].Ruta, "payment path");
        True(fake.Calls[2].Json!.Contains("huellaCotizacionAceptada"), "fingerprint sent");
        True(!fake.Calls[2].Json!.Contains("125"), "no payment amount is client supplied");
        Equal("/api/intentos-compra/IC-01/cotizacion", fake.Calls[3].Ruta, "conflict refreshes quote only");
        Equal(4, fake.Calls.Count, "no duplicate payment request after quote conflict");

        const string itinerary = """{"salida":"2026-11-18T09:00:00","llegada":"2026-11-18T10:00:00","precioTotal":125,"transbordo":null,"tramos":[{"orden":1,"servicioId":"SV-01","tramoId":"TR-01"}]}""";
        var malformedAttempts = new[]
        {
            "{}",
            """{"itinerario":{"salida":"2026-11-18T09:00:00","llegada":"2026-11-18T10:00:00","tramos":[{"orden":1,"servicioId":"SV-01"}]},"holdVenceEn":"2026-11-18T09:15:00Z"}""",
            $"{{\"intentoId\":\"IC/02\",\"itinerario\":{itinerary},\"holdVenceEn\":\"2026-11-18T00:00:00Z\"}}",
            $"{{\"intentoId\":\"IC-03\",\"itinerario\":{itinerary}}}",
            $"{{\"intentoId\":\"IC-03\",\"itinerario\":{itinerary},\"holdVenceEn\":\"0001-01-01T00:00:00Z\"}}",
            $"{{\"intentoId\":\"IC-03\",\"itinerario\":{itinerary},\"holdVenceEn\":\"2026-11-18T09:15:00\"}}",
            """{"intentoId":"IC-03","itinerario":null,"holdVenceEn":"2026-11-18T09:15:00Z"}""",
            """{"intentoId":"IC-04","itinerario":{"tramos":[]},"holdVenceEn":"2026-11-18T09:15:00Z"}""",
            """{"intentoId":"IC-05","itinerario":{"tramos":[{"orden":1}]},"holdVenceEn":"2026-11-18T09:15:00Z"}""",
            """{"intentoId":"IC-05","itinerario":{"tramos":[{"orden":0,"servicioId":"SV-01"}]},"holdVenceEn":"2026-11-18T09:15:00Z"}""",
        };
        var malformedIndex = 0;
        foreach (var body in malformedAttempts)
        {
            malformedIndex++;
            var malformedSession = new FakeSession(new RespuestaApiAdministracion(true, 201, body, "created"));
            var malformed = await new CompraCliente(malformedSession).CrearIntentoAsync([new() { Orden = 1, ServicioId = "SV-01" }]);
            Equal(TipoResultadoCompra.Ambiguo, malformed.Tipo, $"malformed successful attempt {malformedIndex} stays ambiguous");
            True(malformed.Datos is null, "ambiguous create exposes no unusable attempt DTO");
            Equal(1, malformedSession.Calls.Count, "ambiguous create is never repeated");
        }

        var expiredButKnown = $"{{\"intentoId\":\"IC-06\",\"itinerario\":{itinerary},\"holdVenceEn\":\"2025-11-18T09:15:00Z\"}}";
        var expiredSession = new FakeSession(new RespuestaApiAdministracion(true, 201, expiredButKnown, "created"));
        Equal(TipoResultadoCompra.Exito,
            (await new CompraCliente(expiredSession).CrearIntentoAsync([new() { Orden = 1, ServicioId = "SV-01" }])).Tipo,
            "known expired hold remains a known create result");

        var synchronized = """{"pago":{"pagoId":"PG-01","intentoId":"IC-01","estado":"aprobado","importe":125,"moneda":"UYU","checkoutUrl":null,"holdVenceEn":"2026-11-18T09:15:00Z"},"compra":{"compraId":"CO-01","intentoId":"IC-01","pagoId":"PG-01","confirmadoUtc":"2026-11-18T09:02:00Z","importeTotal":125,"tickets":[{"ticketId":"TI-01","orden":1,"servicioId":"SV-01","tramoId":"TR-01","origenNombre":"A","destinoNombre":"B","salida":"2026-11-18T09:00:00","llegada":"2026-11-18T09:30:00","importe":50,"politicaAsiento":"sinAsiento","numeroAsiento":null,"qrOpaco":"opaque-1"},{"ticketId":"TI-02","orden":2,"servicioId":"SV-02","tramoId":"TR-02","origenNombre":"B","destinoNombre":"C","salida":"2026-11-18T09:40:00","llegada":"2026-11-18T10:00:00","importe":75,"politicaAsiento":"asignacionAutomatica","numeroAsiento":7,"qrOpaco":"opaque-2"}]}}""";
        var syncSession = new FakeSession(new RespuestaApiAdministracion(true, 200, synchronized, "ok"));
        var syncResult = await new CompraCliente(syncSession).SincronizarPagoAsync("IC-01");
        Equal(TipoResultadoCompra.Exito, syncResult.Tipo, "sync returns authoritative purchase result");
        Equal(2, syncResult.Datos!.Compra!.Tickets.Count, "two issued ticket summaries returned");
        Equal(HttpMethod.Post, syncSession.Calls[0].Metodo, "sync uses POST");
        Equal("/api/intentos-compra/IC-01/pagos/sincronizar", syncSession.Calls[0].Ruta, "sync reuses attempt ID");
        Equal(null, syncSession.Calls[0].Json, "sync sends no payment body");
    }
}

sealed class FakeSession(params RespuestaApiAdministracion[] responses) : ISesionCliente
{
    private readonly Queue<RespuestaApiAdministracion> _responses = new(responses);
    public List<(HttpMethod Metodo, string Ruta, string? Json)> Calls { get; } = [];
    public EstadoSesion Estado => new(true, false);
    public Task<ResultadoAutenticacion> IngresarAsync(SolicitudIngresoDto solicitud) => throw new NotImplementedException();
    public Task<ResultadoAutenticacion> SalirAsync() => throw new NotImplementedException();
    public Task<ResultadoAutenticacion> ConsultarIdentidadAsync() => throw new NotImplementedException();
    public Task<ResultadoAutenticacion> ProbarPasajeroAsync() => throw new NotImplementedException();
    public Task<ResultadoAutenticacion> ProbarAdministradorAsync() => throw new NotImplementedException();
    public Task<RespuestaApiAdministracion> SolicitarAdministradorAsync(HttpMethod metodo, string ruta, string? json = null) => throw new NotImplementedException();
    public Task<RespuestaApiAdministracion> SolicitarAutenticadaAsync(HttpMethod metodo, string ruta, string? json = null)
    {
        Calls.Add((metodo, ruta, json));
        return Task.FromResult(_responses.Dequeue());
    }
    public void Descartar() { }
}
