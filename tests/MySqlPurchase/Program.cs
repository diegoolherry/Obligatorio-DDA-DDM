using Microsoft.AspNetCore.Identity;
using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Authentication;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Identifiers;
using API_Berruti.Services.Operational;
using API_Berruti.Security.Authentication;
using Microsoft.EntityFrameworkCore;

const string uso = "Use no arguments or --plan for a safe plan. Prepare an externally created empty fixture with --prepare --allow-test-db-writes; purchase checks require --run --allow-test-db-writes.";

if (args.Length == 0 || args.SequenceEqual(["--plan"]))
{
    Console.WriteLine("PLANNED ONLY: no environment secrets read, DbContext created, SQL executed, or tests run.");
    Console.WriteLine(uso);
    return 0;
}

var preparar = args.SequenceEqual(["--prepare", "--allow-test-db-writes"]);
var ejecutar = args.SequenceEqual(["--run", "--allow-test-db-writes"]);
if (!preparar && !ejecutar)
{
    Console.Error.WriteLine("REFUSED: choose exactly --prepare or --run together with --allow-test-db-writes before configuration is read.");
    Console.Error.WriteLine(uso);
    return 2;
}

try
{
    if (preparar)
    {
        await PrepararFixtureAsync();
        return 0;
    }
    var fixture = PurchaseFixture.FromEnvironment();
    await RunAsync(fixture);
    Console.WriteLine("MySQL purchase integration checks passed. Test rows and occupied seats were intentionally retained.");
    return 0;
}
catch (HarnessFailure ex)
{
    Console.Error.WriteLine($"FAILED: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAILED: unexpected {ex.GetType().Name}; details omitted to protect fixture data and credentials.");
    return 1;
}

static async Task RunAsync(PurchaseFixture fixture)
{
    var reloj = new RelojPrueba(DateTimeOffset.UtcNow);
    await using (var contexto = CrearContexto(fixture.ConnectionString))
    {
        await contexto.Database.OpenConnectionAsync();
        await VerificarDestinoAsync(contexto);
        await VerificarFixtureAsync(contexto, fixture, reloj);
    }

    var aprobado = await PrepararPagoAsync(fixture, reloj);
    var confirmado = aprobado.OrdenPendiente with
    {
        Estado = EstadoPago.Aprobado,
        ImportePagado = aprobado.ImporteEsperado
    };
    var primera = await ConciliarAsync(fixture, reloj, aprobado, confirmado);
    Verificar(primera.Compra is not null, "approved order atomically returns a confirmed purchase");
    Verificar(primera.Compra!.Tickets.Count == 2, "two selected legs produce two tickets");
    var sinAsiento = primera.Compra.Tickets.SingleOrDefault(t => !t.NumeroAsiento.HasValue);
    var asignado = primera.Compra.Tickets.SingleOrDefault(t => t.NumeroAsiento.HasValue);
    Verificar(sinAsiento is not null && asignado is not null, "tickets preserve no-seat and assigned-seat policies");
    Verificar(primera.Compra.Tickets.Select(t => t.TicketId).Distinct(StringComparer.Ordinal).Count() == 2,
        "ticket identifiers are distinct");
    Verificar(primera.Compra.Tickets.All(t => EsQrOpaco(t.QrOpaco)), "ticket QR payloads are opaque 64-character hex values");
    Verificar(primera.Compra.Tickets.Select(t => t.QrOpaco).Distinct(StringComparer.Ordinal).Count() == 2,
        "ticket QR payloads are unique");
    await VerificarPersistenciaAsync(fixture, aprobado.IntentoId, primera.Compra.CompraId, 2);

    var contadoresConfirmados = await LeerContadoresAsync(fixture.ConnectionString);
    var repetidas = await Task.WhenAll(
        ConciliarAsync(fixture, reloj, aprobado, confirmado),
        ConciliarAsync(fixture, reloj, aprobado, confirmado));
    Verificar(repetidas.All(r => r.Compra?.CompraId == primera.Compra.CompraId),
        "parallel reconciliation of the same provider order returns one purchase");
    Verificar(repetidas.All(r => TicketsIguales(r.Compra!, primera.Compra)),
        "parallel idempotent responses preserve ticket identifiers and QR values");
    Verificar(DictionariesIguales(contadoresConfirmados, await LeerContadoresAsync(fixture.ConnectionString)),
        "same-order retries do not advance counters");

    reloj.Definir(aprobado.HoldVenceEn.AddSeconds(1));
    var competencia = await Task.WhenAll(
        PrepararPagoAsync(fixture, reloj),
        PrepararPagoAsync(fixture, reloj));
    var asientos = competencia.Select(p => p.AsientoAsignado).ToArray();
    Verificar(asientos.All(n => n.HasValue) && asientos.Distinct().Count() == 2,
        "concurrent live holds allocate different assigned seats");
    Verificar(asientos.All(n => n != asignado!.NumeroAsiento),
        "expired original hold does not make a confirmed seat reusable");
    var conciliadas = await Task.WhenAll(competencia.Select(p => ConciliarAsync(
        fixture, reloj, p, p.OrdenPendiente with { Estado = EstadoPago.Aprobado, ImportePagado = p.ImporteEsperado })));
    Verificar(conciliadas.All(r => r.Compra?.Tickets.Count == 2), "both competing reservations confirm without ticket overlap");
    Verificar(conciliadas.Select(r => r.Compra!.Tickets.Single(t => t.NumeroAsiento.HasValue).NumeroAsiento)
            .Distinct().Count() == 2,
        "concurrent confirmed purchases retain different assigned seats");

    var rechazo = await PrepararPagoAsync(fixture, reloj);
    var desconocida = await ConciliarAsync(fixture, reloj, rechazo,
        rechazo.OrdenPendiente with { Estado = EstadoPago.Desconocido });
    Verificar(desconocida.Compra is null && desconocida.Registro.Pago.Estado == EstadoPago.Desconocido,
        "unknown provider state does not issue tickets");
    var bloqueante = await ReservarDeNuevoAsync(fixture, reloj, rechazo);
    Verificar(bloqueante.Pago.Id == rechazo.PagoId, "unknown payment blocks creating a second payment");
    Verificar(await BuscarPendienteAsync(fixture, reloj) == rechazo.IntentoId,
        "pending-attempt lookup returns the most recent attempt with an unknown payment");
    var relojVivo = reloj.GetUtcNow().UtcDateTime;
    reloj.Definir(rechazo.HoldVenceEn.AddSeconds(1));
    var bloqueanteVencido = await ReservarDeNuevoAsync(fixture, reloj, rechazo);
    Verificar(bloqueanteVencido.Pago.Id == rechazo.PagoId && !bloqueanteVencido.EsNuevo,
        "unknown payment still blocks creating a second payment after the hold expired");
    Verificar(await BuscarPendienteAsync(fixture, reloj) == rechazo.IntentoId,
        "pending-attempt lookup keeps an expired-hold attempt whose payment is still unknown");
    reloj.Definir(relojVivo);
    var rechazado = await ConciliarAsync(fixture, reloj, rechazo,
        rechazo.OrdenPendiente with { Estado = EstadoPago.Rechazado });
    Verificar(rechazado.Compra is null && rechazado.Registro.Pago.Estado == EstadoPago.Rechazado,
        "definite rejection does not issue tickets");
    reloj.Definir(rechazo.HoldVenceEn.AddSeconds(1));
    var reintentoVencido = false;
    try
    {
        await ReservarDeNuevoAsync(fixture, reloj, rechazo);
    }
    catch (CotizacionNoDisponibleException)
    {
        reintentoVencido = true;
    }
    Verificar(reintentoVencido, "retry after definite rejection is refused once the original hold expired");
    Verificar(await BuscarPendienteAsync(fixture, reloj) != rechazo.IntentoId,
        "pending-attempt lookup drops a rejected attempt whose hold expired");
    reloj.Definir(relojVivo);
    var reintento = await ReservarDeNuevoAsync(fixture, reloj, rechazo);
    Verificar(reintento.Pago.Id != rechazo.PagoId && reintento.HoldVenceEn == rechazo.HoldVenceEn,
        "definite rejection allows a new payment without extending the original hold");

    var tardia = await PrepararPagoAsync(fixture, reloj);
    var countersBeforeLate = await LeerContadoresAsync(fixture.ConnectionString);
    var lateCounter = new DecoradorContadores(
        afterTicketAllocation: () => reloj.Definir(tardia.HoldVenceEn),
        callbackAfterTicketAllocation: 2);
    var late = await ConciliarAsync(fixture, reloj, tardia,
        tardia.OrdenPendiente with { Estado = EstadoPago.Aprobado, ImportePagado = tardia.ImporteEsperado },
        lateCounter);
    Verificar(late.Compra is null && late.RequiereDevolucion &&
        late.Registro.Pago.Estado == EstadoPago.DevolucionPendiente,
        "approval observed at the original deadline schedules refund and issues no tickets");
    var refundKey = late.Registro.Pago.ClaveDevolucion;
    Verificar(refundKey.HasValue, "late approval records a stable refund key");
    Verificar(DictionariesIguales(countersBeforeLate, await LeerContadoresAsync(fixture.ConnectionString)),
        "savepoint rollback removes counter SQL updates from late approval");
    var repeatedLate = await ConciliarAsync(fixture, reloj, tardia,
        tardia.OrdenPendiente with { Estado = EstadoPago.Aprobado, ImportePagado = tardia.ImporteEsperado });
    Verificar(repeatedLate.Compra is null && repeatedLate.Registro.Pago.ClaveDevolucion == refundKey,
        "repeated late approval preserves refund key and never creates a purchase");
    await VerificarSinCompraAsync(fixture, tardia.IntentoId);
    Verificar(await BuscarPendienteAsync(fixture, reloj) == tardia.IntentoId,
        "pending-attempt lookup returns an approved payment pending refund even after the hold expired");

    var fallo = await PrepararPagoAsync(fixture, reloj);
    var countersBeforeFailure = await LeerContadoresAsync(fixture.ConnectionString);
    var fallador = new DecoradorContadores(failAfterTicketAllocation: 1);
    var falloOcurrio = false;
    try
    {
        await ConciliarAsync(fixture, reloj, fallo,
            fallo.OrdenPendiente with { Estado = EstadoPago.Aprobado, ImportePagado = fallo.ImporteEsperado }, fallador);
    }
    catch (InvalidOperationException) when (fallador.FalloInyectado)
    {
        falloOcurrio = true;
    }
    Verificar(falloOcurrio, "test fault occurs only after a real ticket-counter SQL update");
    Verificar(DictionariesIguales(countersBeforeFailure, await LeerContadoresAsync(fixture.ConnectionString)),
        "database rollback restores every allocated counter");
    await VerificarSinCompraAsync(fixture, fallo.IntentoId);

    Console.WriteLine("PASS: approved issuance, same-order idempotency, concurrent seat allocation, rejected retry, late refund savepoint, and counter rollback.");
}

static BerrutiDbContext CrearContexto(string connectionString) =>
    new(new DbContextOptionsBuilder<BerrutiDbContext>().UseMySQL(connectionString).Options);

static async Task PrepararFixtureAsync()
{
    var connectionString = ConnectionSettings.Build();
    await using var contexto = CrearContexto(connectionString);
    await contexto.Database.OpenConnectionAsync();
    await VerificarDestinoAsync(contexto);

    await using (var command = contexto.Database.GetDbConnection().CreateCommand())
    {
        command.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()";
        var tableCount = Convert.ToInt64(await command.ExecuteScalarAsync());
        if (tableCount != 0)
            throw new HarnessFailure("fixture preparation refused: database must contain no tables; no cleanup was attempted");
    }

    var password = Environment.GetEnvironmentVariable("DemoSeed__Contrasena");
    if (string.IsNullOrWhiteSpace(password) || password.Length > 255)
        throw new HarnessFailure("fixture preparation requires the existing private DemoSeed__Contrasena setting");

    await contexto.Database.MigrateAsync();
    var prefixes = new[] { "US-", "RO-", "SA-", "IC-", "PG-", "CO-", "CS-", "TI-" };
    var presentPrefixes = await contexto.Contadores.Select(c => c.Prefijo).ToHashSetAsync();
    contexto.Contadores.AddRange(prefixes.Where(prefix => !presentPrefixes.Contains(prefix))
        .Select(prefix => new ContadorEntidad { Prefijo = prefix, Ultimo = prefix == "SA-" ? 1 : 0 }));
    await contexto.SaveChangesAsync();
    contexto.ChangeTracker.Clear();

    var contadores = new ContadoresRepository(contexto, new Correlativo());
    var cuentas = new CuentasRepository(contexto, contadores, new PasswordHasher<Usuario>());
    var rol = await cuentas.CrearRolAsync(RolIngreso.Pasajero);
    var pasajero = await cuentas.CrearUsuarioAsync("pasajero.purchase@example.test", password, rol.Identificador);
    var escenario = EscenarioOperativoDemo.Construir(DateTime.Today.AddDays(1));
    contexto.Paradas.AddRange(escenario.Paradas);
    contexto.Tramos.AddRange(escenario.Tramos);
    contexto.Salidas.AddRange(escenario.Salidas);
    contexto.ParadasSalida.AddRange(escenario.ParadasSalida);
    contexto.AsientosSalida.AddRange(escenario.AsientosSalida);
    contexto.Servicios.AddRange(escenario.Servicios);
    contexto.Tarifas.AddRange(escenario.Tarifas);
    await contexto.SaveChangesAsync();

    var sinAsiento = escenario.Servicios.Single(s => s.PoliticaAsiento == PoliticaAsiento.SinAsiento);
    var asignado = escenario.Servicios.Single(s => s.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica);
    if (!pasajero.Activo) throw new HarnessFailure("prepared passenger is not active");
    var fixture = new PurchaseFixture(connectionString, pasajero.IdentificadorPublico, sinAsiento.Id, asignado.Id);
    await VerificarFixtureAsync(contexto, fixture, new RelojPrueba(DateTimeOffset.UtcNow));
    Console.WriteLine("Purchase fixture prepared. Save these non-secret values for the later opted-in harness run:");
    Console.WriteLine($"BERUTI_MYSQL_PURCHASE_TEST_PASSENGER_SUBJECT={pasajero.IdentificadorPublico}");
    Console.WriteLine($"BERUTI_MYSQL_PURCHASE_TEST_SERVICE_NO_SEAT={sinAsiento.Id}");
    Console.WriteLine($"BERUTI_MYSQL_PURCHASE_TEST_SERVICE_ASSIGNED={asignado.Id}");
    Console.WriteLine("No password, token, connection string, or QR value was printed. Rows were retained; no cleanup is available.");
}

static async Task VerificarDestinoAsync(BerrutiDbContext contexto)
{
    await using var command = contexto.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT DATABASE(), CURRENT_USER()";
    await using var reader = await command.ExecuteReaderAsync();
    Verificar(await reader.ReadAsync(), "database identity query returns a row");
    var database = reader.GetString(0);
    var databaseUser = reader.GetString(1).Split('@')[0];
    Verificar(database == PurchaseFixture.DatabaseName &&
        databaseUser.Equals(PurchaseFixture.DatabaseUser, StringComparison.OrdinalIgnoreCase),
        "connected server identity remains the dedicated non-root test database");
}

static async Task VerificarFixtureAsync(BerrutiDbContext contexto, PurchaseFixture fixture, RelojPrueba reloj)
{
    Verificar(!await contexto.Set<IntentoCompra>().AnyAsync() && !await contexto.Set<Pago>().AnyAsync() &&
        !await contexto.Set<Compra>().AnyAsync() && !await contexto.Set<CompraServicio>().AnyAsync() &&
        !await contexto.Set<Ticket>().AnyAsync() && !await contexto.Set<HoldAsiento>().AnyAsync() &&
        !await contexto.Set<TramoPago>().AnyAsync(),
        "fresh fixture has no prior purchase, payment, hold, or ticket rows");

    var contadores = await contexto.Contadores.AsNoTracking().ToDictionaryAsync(c => c.Prefijo, c => c.Ultimo);
    foreach (var (prefix, headroom) in new[] { ("IC-", 6), ("PG-", 7), ("CO-", 4), ("CS-", 7), ("TI-", 7) })
        Verificar(contadores.TryGetValue(prefix, out var current) && current + headroom < 50000,
            "fixture has required identifier counters and headroom");

    var counters = new ContadoresRepository(contexto, new Correlativo());
    var intentos = new IntentosCompraRepository(contexto, counters, new ParametrosCompra { HoldMinutos = 15 }, reloj);
    var servicios = await intentos.BuscarServiciosAsync([fixture.ServicioSinAsiento, fixture.ServicioAsignado]);
    Verificar(servicios.Count == 2, "fixture service identifiers exist");
    var primero = servicios.Single(s => s.Servicio.Id == fixture.ServicioSinAsiento);
    var segundo = servicios.Single(s => s.Servicio.Id == fixture.ServicioAsignado);
    Verificar(primero.Servicio.PoliticaAsiento == PoliticaAsiento.SinAsiento &&
        segundo.Servicio.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica &&
        segundo.Servicio.SalidaId is not null && primero.DestinoId == segundo.OrigenId,
        "fixture services form the required connected no-seat and assigned-seat legs");
    var itinerarios = new ItinerariosService(new BusquedaItinerariosRepository(contexto));
    var ofrecidos = await itinerarios.BuscarAsync(primero.OrigenId, segundo.DestinoId,
        DateOnly.FromDateTime(primero.Servicio.Salida));
    Verificar(ofrecidos.Any(i => i.Tramos.OrderBy(t => t.Orden).Select(t => t.ServicioId)
        .SequenceEqual([fixture.ServicioSinAsiento, fixture.ServicioAsignado], StringComparer.Ordinal)),
        "fixture services are offered as the selected itinerary");
    Verificar(await contexto.AsientosSalida.CountAsync(a => a.SalidaId == segundo.Servicio.SalidaId) >= 4,
        "assigned departure has sufficient externally prepared inventory");
}

static async Task<CompraPrueba> PrepararPagoAsync(PurchaseFixture fixture, RelojPrueba reloj)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    var contadores = new ContadoresRepository(contexto, new Correlativo());
    var intentos = new IntentosCompraRepository(contexto, contadores, new ParametrosCompra { HoldMinutos = 15 }, reloj);
    var itinerarios = new ItinerariosService(new BusquedaItinerariosRepository(contexto));
    var servicioIntentos = new IntentosCompraService(intentos, itinerarios);
    var intento = await servicioIntentos.CrearAsync(fixture.Pasajero, new IniciarIntentoCompraRequestDto
    {
        Tramos =
        [
            new SeleccionTramoCompraDto { Orden = 1, ServicioId = fixture.ServicioSinAsiento },
            new SeleccionTramoCompraDto { Orden = 2, ServicioId = fixture.ServicioAsignado }
        ]
    });
    var cotizaciones = new CotizacionesCompraService(servicioIntentos, itinerarios, reloj);
    var cotizacion = await cotizaciones.CotizarAsync(fixture.Pasajero, intento.IntentoId);
    var asientoAsignado = await contexto.TramosIntentoCompra.AsNoTracking()
        .Where(t => t.IntentoId == intento.IntentoId && t.ServicioId == fixture.ServicioAsignado)
        .Select(t => t.NumeroAsiento).SingleAsync();
    var pagos = new PagosRepository(contexto, intentos, contadores, new Correlativo(), reloj);
    var registro = await pagos.ReservarAsync(fixture.Pasajero, cotizacion, cotizacion.HuellaCotizacion);
    var pendiente = new OrdenCheckoutMercadoPago
    {
        Id = $"TEST-{Guid.NewGuid():N}",
        ReferenciaExterna = registro.Pago.ReferenciaExterna,
        Importe = registro.Pago.ImporteEsperado,
        ImportePagado = 0m,
        Moneda = "UYU",
        Estado = EstadoPago.Pendiente,
        CheckoutUrl = "https://example.invalid/test-checkout"
    };
    registro = await pagos.RegistrarOrdenAsync(fixture.Pasajero, registro.Pago.Id, pendiente);
    return new CompraPrueba(intento.IntentoId, registro.Pago.Id, registro.HoldVenceEn,
        registro.Pago.ImporteEsperado, cotizacion, pendiente, asientoAsignado);
}

static async Task<RegistroPago> ReservarDeNuevoAsync(PurchaseFixture fixture, RelojPrueba reloj, CompraPrueba prueba)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    var contadores = new ContadoresRepository(contexto, new Correlativo());
    var intentos = new IntentosCompraRepository(contexto, contadores, new ParametrosCompra { HoldMinutos = 15 }, reloj);
    return await new PagosRepository(contexto, intentos, contadores, new Correlativo(), reloj)
        .ReservarAsync(fixture.Pasajero, prueba.Cotizacion, prueba.Cotizacion.HuellaCotizacion);
}

static async Task<string?> BuscarPendienteAsync(PurchaseFixture fixture, RelojPrueba reloj)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    return await new IntentosPendientesRepository(contexto)
        .BuscarIdPendienteAsync(fixture.Pasajero, reloj.GetUtcNow().UtcDateTime);
}

static async Task<ResultadoConciliacionPago> ConciliarAsync(
    PurchaseFixture fixture,
    RelojPrueba reloj,
    CompraPrueba prueba,
    OrdenCheckoutMercadoPago orden,
    DecoradorContadores? decorador = null)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    var rawCounters = new ContadoresRepository(contexto, new Correlativo());
    var intentos = new IntentosCompraRepository(contexto, rawCounters, new ParametrosCompra { HoldMinutos = 15 }, reloj);
    IContadoresRepository confirmationCounters = decorador is null
        ? rawCounters
        : decorador.ConDelegado(rawCounters);
    return await new ConfirmacionesCompraRepository(contexto, intentos, confirmationCounters, reloj)
        .ConciliarAsync(fixture.Pasajero, prueba.PagoId, orden);
}

static async Task<Dictionary<string, int>> LeerContadoresAsync(string connectionString)
{
    await using var contexto = CrearContexto(connectionString);
    return await contexto.Contadores.AsNoTracking().ToDictionaryAsync(c => c.Prefijo, c => c.Ultimo);
}

static async Task VerificarPersistenciaAsync(PurchaseFixture fixture, string intentoId, string compraId, int expectedTickets)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    Verificar(await contexto.Set<Compra>().CountAsync(c => c.IntentoId == intentoId && c.Id == compraId) == 1,
        "one purchase row is persisted for the attempt");
    var tickets = await (from ticket in contexto.Set<Ticket>()
                         join servicio in contexto.Set<CompraServicio>() on ticket.CompraServicioId equals servicio.Id
                         where servicio.IntentoId == intentoId
                         select ticket.Id).CountAsync();
    Verificar(tickets == expectedTickets, "all expected ticket rows are persisted");
}

static async Task VerificarSinCompraAsync(PurchaseFixture fixture, string intentoId)
{
    await using var contexto = CrearContexto(fixture.ConnectionString);
    var compraExiste = await contexto.Set<Compra>().AnyAsync(c => c.IntentoId == intentoId);
    var tickets = await (from ticket in contexto.Set<Ticket>()
                         join servicio in contexto.Set<CompraServicio>() on ticket.CompraServicioId equals servicio.Id
                         where servicio.IntentoId == intentoId
                         select ticket.Id).CountAsync();
    Verificar(!compraExiste && tickets == 0, "no purchase or tickets are persisted for this attempt");
}

static bool TicketsIguales(CompraConfirmadaResponseDto actual, CompraConfirmadaResponseDto original) =>
    actual.Tickets.OrderBy(t => t.Orden).Select(t => (t.TicketId, t.QrOpaco))
        .SequenceEqual(original.Tickets.OrderBy(t => t.Orden).Select(t => (t.TicketId, t.QrOpaco)));

static bool DictionariesIguales(Dictionary<string, int> left, Dictionary<string, int> right) =>
    left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);

static bool EsQrOpaco(string qr) => qr.Length == 64 && qr.All(c => char.IsAsciiHexDigit(c));

static void Verificar(bool condicion, string assertion)
{
    if (!condicion) throw new HarnessFailure(assertion);
    Console.WriteLine($"PASS: {assertion}");
}

static class ConnectionSettings
{
    public static string Build()
    {
        var raw = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw)) throw new HarnessFailure("required test connection environment variable is missing");
        try { return ConfiguracionAutenticacion.ValidarConexion(raw, "PurchaseTests").ConnectionString; }
        catch { throw new HarnessFailure("connection must match the dedicated loopback purchase-test environment tuple"); }
    }
}

sealed record PurchaseFixture(string ConnectionString, string Pasajero, string ServicioSinAsiento, string ServicioAsignado)
{
    public const string DatabaseName = "berruticomprastests";
    public const string DatabaseUser = "berruti_purchase_test";

    public static PurchaseFixture FromEnvironment()
    {
        var connection = ConnectionSettings.Build();
        var passenger = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_PASSENGER_SUBJECT");
        var noSeat = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_SERVICE_NO_SEAT");
        var assigned = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_SERVICE_ASSIGNED");
        if (string.IsNullOrWhiteSpace(passenger) || string.IsNullOrWhiteSpace(noSeat) ||
            string.IsNullOrWhiteSpace(assigned) || noSeat == assigned)
            throw new HarnessFailure("active passenger subject and two distinct service IDs are required");
        return new PurchaseFixture(connection, passenger, noSeat, assigned);
    }
}

sealed record CompraPrueba(
    string IntentoId,
    string PagoId,
    DateTime HoldVenceEn,
    decimal ImporteEsperado,
    CotizarCompraResponseDto Cotizacion,
    OrdenCheckoutMercadoPago OrdenPendiente,
    int? AsientoAsignado);

sealed class RelojPrueba(DateTimeOffset inicial) : TimeProvider
{
    private long _ticks = inicial.UtcTicks;

    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);

    public void Definir(DateTime valorUtc)
    {
        if (valorUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC is required.", nameof(valorUtc));
        Interlocked.Exchange(ref _ticks, valorUtc.Ticks);
    }
}

sealed class DecoradorContadores(
    Action? afterTicketAllocation = null,
    int callbackAfterTicketAllocation = int.MaxValue,
    int failAfterTicketAllocation = int.MaxValue) : IContadoresRepository
{
    private int _tickets;
    public bool FalloInyectado { get; private set; }

    public async Task<string> IncrementarAsync(string prefijo)
    {
        var id = await _inner!.IncrementarAsync(prefijo);
        if (prefijo == "TI-")
        {
            var count = Interlocked.Increment(ref _tickets);
            if (count == callbackAfterTicketAllocation) afterTicketAllocation?.Invoke();
            if (count == failAfterTicketAllocation)
            {
                FalloInyectado = true;
                throw new InvalidOperationException("Injected failure after the real ticket counter SQL update.");
            }
        }
        return id;
    }

    private IContadoresRepository? _inner;

    public DecoradorContadores ConDelegado(IContadoresRepository inner)
    {
        _inner = inner;
        return this;
    }
}

sealed class HarnessFailure(string message) : Exception(message);
