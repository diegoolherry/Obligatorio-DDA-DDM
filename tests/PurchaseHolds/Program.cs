using System.Reflection;
using API_Berruti.Data;
using API_Berruti.Data.Migrations;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Authentication;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions;

var checks = new List<(string Name, bool Passed)>();
void Check(string name, bool passed) => checks.Add((name, passed));
async Task Expect<TException>(Func<Task> action, string name) where TException : Exception
{
    try { await action(); Check(name, false); }
    catch (TException) { Check(name, true); }
    catch { Check(name, false); }
}

var api = Assembly.Load("API-Berruti");
Check("purchase attempt entity is present", api.GetType("API_Berruti.Models.Operational.IntentoCompra") is not null);
Check("ordered purchase selection entity is present", api.GetType("API_Berruti.Models.Operational.TramoIntentoCompra") is not null);
Check("seat hold entity is present", api.GetType("API_Berruti.Models.Operational.HoldAsiento") is not null);
Check("purchase repository and service are registered production types", api.GetType("API_Berruti.Repositories.Operational.IntentosCompraRepository") is not null && api.GetType("API_Berruti.Services.Operational.IntentosCompraService") is not null);

var departure = new DateTime(2026, 11, 17, 8, 0, 0, DateTimeKind.Unspecified);
var quote = PurchaseFixtures.Itinerary(("svc-1", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 125m));
var repo = new FakePurchaseRepository();
repo.Services.Add(PurchaseFixtures.Service("svc-1", "stop-a", "stop-b", departure, departure.AddHours(1)));
var search = new FakeItinerariosService { Offered = [quote] };
var service = new IntentosCompraService(repo, search);

var response = await service.CrearAsync("public-user", PurchaseFixtures.Request((7, "svc-1")));
Check("create returns the server quote unchanged and writes one new attempt", ReferenceEquals(response.Itinerario, quote) && response.Itinerario.PrecioTotal == 125m && repo.CreateCount == 1);
Check("search uses mapped route endpoints and service date", search.Origin == "stop-a" && search.Destination == "stop-b" && search.Date == DateOnly.FromDateTime(departure));
Check("request DTO exposes no client quote or price field", typeof(IniciarIntentoCompraRequestDto).GetProperties().All(property => property.Name is not "Precio" and not "Importe"));

var duplicateOrders = PurchaseFixtures.Request((1, "svc-1"), (1, "svc-1"));
await Expect<ArgumentException>(() => service.CrearAsync("public-user", duplicateOrders), "duplicate order or service is rejected before repository write");
Check("duplicate request did not consume a fake attempt", repo.CreateCount == 1);
await Expect<ArgumentException>(() => service.CrearAsync("public-user", PurchaseFixtures.Request((0, "svc-1"))), "non-positive request order is rejected by DataAnnotations");
await Expect<ArgumentException>(() => service.CrearAsync(string.Empty, PurchaseFixtures.Request((1, "svc-1"))), "missing public identity is rejected");
await Expect<KeyNotFoundException>(() => service.CrearAsync("public-user", PurchaseFixtures.Request((1, "svc-missing"))), "unknown service cannot be quoted");
Check("unknown service is rejected without a repository write", repo.CreateCount == 1);

var alternateQuote = PurchaseFixtures.Itinerary(("svc-other", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 900m));
search.Offered = [alternateQuote];
await Expect<ArgumentException>(() => service.CrearAsync("public-user", PurchaseFixtures.Request((1, "svc-1"))), "unoffered service sequence is rejected");
Check("unoffered service cannot use client-selected price or create an attempt", repo.CreateCount == 1);

var twoLegQuote = PurchaseFixtures.Itinerary(
    ("svc-1", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 125m),
    ("svc-2", "stop-b", "B", "stop-c", "C", departure.AddHours(1).AddMinutes(10), departure.AddHours(2), 75m));
repo.Services.Add(PurchaseFixtures.Service("svc-2", "stop-b", "stop-c", departure.AddHours(1).AddMinutes(10), departure.AddHours(2)));
search.Offered = [twoLegQuote];
var ordered = await service.CrearAsync("public-user", PurchaseFixtures.Request((10, "svc-2"), (4, "svc-1")));
Check("noncontiguous client orders are sorted before matching the offered itinerary", ordered.Itinerario.Tramos.Select(leg => leg.ServicioId).SequenceEqual(["svc-1", "svc-2"]));
Check("a valid two-leg server offer preserves its total", ordered.Itinerario.PrecioTotal == 200m);

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Compra:HoldMinutos"] = "15" }).Build();
Check("purchase hold configuration accepts the approved fifteen minute value", ParametrosCompra.Desde(config).HoldMinutos == 15);
foreach (var invalid in new string?[] { null, "", "0", "-3", "not-a-number" })
{
    var invalidConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Compra:HoldMinutos"] = invalid }).Build();
    try { _ = ParametrosCompra.Desde(invalidConfig); Check($"invalid hold configuration rejected ({invalid ?? "missing"})", false); }
    catch (InvalidOperationException) { Check($"invalid hold configuration rejected ({invalid ?? "missing"})", true); }
}

var overlap = typeof(IntentosCompraRepository).GetMethod("IntervalsOverlap", BindingFlags.Static | BindingFlags.NonPublic)!;
bool Overlap(int a, int b, int c, int d) => (bool)overlap.Invoke(null, [a, b, c, d])!;
Check("same-seat overlapping intervals conflict", Overlap(1, 4, 3, 5));
Check("nested interval conflicts with an existing hold", Overlap(1, 8, 3, 5));
Check("equal interval conflicts", Overlap(2, 5, 2, 5));
Check("adjacent intervals can reuse a seat", !Overlap(1, 3, 3, 5));
Check("separated intervals can reuse a seat", !Overlap(1, 2, 4, 6));

var bookedSelection = SeatSelection("IC-booked", "service-booked", "SA-confirmed", 7,
    departure, departure.AddHours(2), 1, 4);
bool HasSeatConflict(TramoIntentoCompra candidate, params TramoIntentoCompra[] existing) =>
    existing.AsQueryable().Any(OcupacionAsientosCompra.Superpuestos(candidate));
Check("confirmed-occupancy predicate conflicts for same departure, same seat, and overlapping interval",
    HasSeatConflict(SeatSelection("IC-new", "service-new", "SA-confirmed", 7,
        departure.AddMinutes(30), departure.AddHours(1), 3, 5), bookedSelection));
Check("confirmed-occupancy predicate permits adjacent intervals on the same seat",
    !HasSeatConflict(SeatSelection("IC-adjacent", "service-adjacent", "SA-confirmed", 7,
        departure.AddHours(2), departure.AddHours(3), 4, 5), bookedSelection));
Check("confirmed-occupancy overlap follows stop order even when departure times do not overlap",
    HasSeatConflict(SeatSelection("IC-order-overlap", "service-order-overlap", "SA-confirmed", 7,
        departure.AddHours(3), departure.AddHours(4), 3, 5), bookedSelection));
Check("confirmed-occupancy adjacency follows stop order even when timestamps are identical",
    !HasSeatConflict(SeatSelection("IC-order-adjacent", "service-order-adjacent", "SA-confirmed", 7,
        departure, departure.AddHours(2), 4, 5), bookedSelection));
Check("confirmed-occupancy predicate permits an overlapping interval on a different departure",
    !HasSeatConflict(SeatSelection("IC-other-departure", "service-other", "SA-other", 7,
        departure.AddMinutes(30), departure.AddHours(1), 3, 5), bookedSelection));
Check("confirmed-occupancy predicate permits an overlapping interval on a different seat",
    !HasSeatConflict(SeatSelection("IC-other-seat", "service-other-seat", "SA-confirmed", 8,
        departure.AddMinutes(30), departure.AddHours(1), 3, 5), bookedSelection));

var confirmedAttemptId = "IC-sold";
var originalHoldExpiry = departure.AddMinutes(-1);
var originalAttempt = new IntentoCompra
{
    Id = confirmedAttemptId,
    UsuarioId = "owner",
    CreadoUtc = departure.AddMinutes(-16),
    HoldVenceUtc = originalHoldExpiry
};
var originalExpiredHold = new HoldAsiento
{
    IntentoId = confirmedAttemptId,
    Orden = 1,
    SalidaId = "SA-confirmed",
    NumeroAsiento = 7,
    OrdenEmbarque = 1,
    OrdenDesembarque = 4
};
var confirmedSelection = SeatSelection(confirmedAttemptId, "service-sold", "SA-confirmed", 7,
    departure, departure.AddHours(2), 1, 4);
var ticketedSelectionQuery = OcupacionAsientosCompra.TicketsConfirmados(
    new[] { new Ticket { Id = "TI-sold", CompraServicioId = "CS-sold", QrOpaco = "opaque" } }.AsQueryable(),
    new[] { new CompraServicio { Id = "CS-sold", CompraId = "CO-sold", IntentoId = confirmedAttemptId, PagoId = "PG-sold", Orden = 1, Importe = 100m } }.AsQueryable(),
    new[] { confirmedSelection }.AsQueryable());
var overlappingPurchase = SeatSelection("IC-current", "service-current", "SA-confirmed", 7,
    departure.AddMinutes(30), departure.AddHours(1), 3, 5);
Check("ticket-to-purchase-leg-to-attempt query returns a sold interval after its original hold expired",
    originalAttempt.HoldVenceUtc < departure && originalExpiredHold.IntentoId == confirmedAttemptId
    && ticketedSelectionQuery.Any(OcupacionAsientosCompra.Superpuestos(overlappingPurchase)));
Check("ticket-confirmed seat is reusable in an adjacent stop-order interval",
    !ticketedSelectionQuery.Any(OcupacionAsientosCompra.Superpuestos(SeatSelection("IC-next", "service-next",
        "SA-confirmed", 7, departure.AddHours(2), departure.AddHours(3), 4, 5))));
Check("ticket-confirmed seat on another departure does not block this departure",
    !ticketedSelectionQuery.Any(OcupacionAsientosCompra.Superpuestos(SeatSelection("IC-other", "service-other",
        "SA-another", 7, departure.AddMinutes(30), departure.AddHours(1), 3, 5))));

var fixedNow = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc).AddTicks(7);
var truncate = typeof(IntentosCompraRepository).GetMethod("TruncarMicrosegundos", BindingFlags.Static | BindingFlags.NonPublic)!;
var storedNow = (DateTime)truncate.Invoke(null, [fixedNow])!;
Check("persisted timestamps truncate to MySQL microsecond precision and stay UTC", storedNow.Kind == DateTimeKind.Utc && storedNow.Ticks % 10 == 0 && storedNow < fixedNow);

var providerOptions = new DbContextOptionsBuilder<BerrutiDbContext>()
    .UseMySQL("server=127.0.0.1;database=berrutilocal;user=offline_metadata;password=placeholder")
    .Options;
using var context = new BerrutiDbContext(providerOptions);
var model = context.Model;
var attemptEntity = model.FindEntityType(typeof(IntentoCompra));
var selectionEntity = model.FindEntityType(typeof(TramoIntentoCompra));
var holdEntity = model.FindEntityType(typeof(HoldAsiento));
Check("attempt has stable IC identity, owner and immutable UTC deadlines", attemptEntity?.FindPrimaryKey()?.Properties.Single().Name == nameof(IntentoCompra.Id) && attemptEntity.FindProperty(nameof(IntentoCompra.CreadoUtc))?.GetColumnType() == "datetime(6)" && attemptEntity.FindProperty(nameof(IntentoCompra.HoldVenceUtc))?.GetColumnType() == "datetime(6)");
Check("selection uses natural attempt-and-order key and stores quote fields", selectionEntity?.FindPrimaryKey()?.Properties.Select(property => property.Name).SequenceEqual([nameof(TramoIntentoCompra.IntentoId), nameof(TramoIntentoCompra.Orden)]) == true && selectionEntity.FindProperty(nameof(TramoIntentoCompra.Importe))?.GetColumnType() == "decimal(18,2)");
Check("seat hold uses selection identity rather than a generated hold identifier", holdEntity?.FindPrimaryKey()?.Properties.Select(property => property.Name).SequenceEqual([nameof(HoldAsiento.IntentoId), nameof(HoldAsiento.Orden)]) == true);
Check("hold references existing inventory and ordered stop occurrences", holdEntity?.GetForeignKeys().Count(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(AsientoSalida)) == 1 && holdEntity.GetForeignKeys().Count(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(ParadaSalida)) == 2);
var counterConstraint = File.ReadAllLines("src/Api/API-Berruti/API-Berruti/Data/BerrutiDbContext.cs")
    .SingleOrDefault(line => line.Contains("ck_contador_prefijo", StringComparison.Ordinal));
Check("counter constraint accepts the approved IC prefix",
    counterConstraint is not null && counterConstraint.Contains("'IC-'", StringComparison.Ordinal));

var repositorySource = File.ReadAllText("src/Api/API-Berruti/API-Berruti/Repositories/Operational/IntentosCompraRepository.cs");
Check("production repository uses explicit read-committed transaction and locks the departure date with a parameterized row query", repositorySource.Contains("IsolationLevel.ReadCommitted") && repositorySource.Contains("SELECT fecha FROM salida WHERE salida_id = @salidaId FOR UPDATE") && repositorySource.Contains("OrderBy(id => id, StringComparer.Ordinal)"));
var departureGuardInvocation = repositorySource.IndexOf("ValidarSalidasYFechasBloqueadas(salidasInicialesPorServicio", StringComparison.Ordinal);
var counterIncrement = repositorySource.IndexOf("contadores.IncrementarAsync(PrefijoIntento)", StringComparison.Ordinal);
Check("production departure mapping/date guard runs before counter allocation", departureGuardInvocation >= 0 && counterIncrement > departureGuardInvocation);
Check("production hold availability reads the parent deadline strictly after server UTC", repositorySource.Contains("intento.HoldVenceUtc > ahoraUtc"));
Check("production ownership lookup requires active user and passenger role", repositorySource.Contains("cuenta.Activo && rol.Nombre == \"Pasajero\""));
Check("production seat conflict uses same-seat half-open interval overlap", repositorySource.Contains("hold.NumeroAsiento == asiento") && repositorySource.Contains("firstBoarding < secondAlighting && secondBoarding < firstAlighting"));
Check("production repository snapshots quote data and does not store pending payment state", repositorySource.Contains("Importe = cotizado.Importe") && !repositorySource.Contains("EstadoPago"));

var salidaGuard = typeof(IntentosCompraRepository).GetMethod("ValidarSalidasYFechasBloqueadas", BindingFlags.Static | BindingFlags.NonPublic);
Check("production departure mapping/date guard exists as an executable pure guard", salidaGuard is not null);
var guardService = new Servicio
{
    Id = "guard-service", TramoId = "guard-tramo", Salida = departure,
    Llegada = departure.AddHours(1), PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica,
    SalidaId = "SA-A", OrdenEmbarque = 1, OrdenDesembarque = 2
};
Dictionary<string, (string? SalidaId, PoliticaAsiento Politica)> Initial(params (string Id, string? SalidaId, PoliticaAsiento Policy)[] rows) =>
    rows.ToDictionary(row => row.Id, row => (row.SalidaId, row.Policy), StringComparer.Ordinal);
Dictionary<string, ServicioCompra> Current(params ServicioCompra[] rows) => rows.ToDictionary(row => row.Servicio.Id, StringComparer.Ordinal);
var validInitial = Initial(("guard-service", "SA-A", PoliticaAsiento.AsignacionAutomatica));
var validCurrent = Current(new ServicioCompra(guardService, "stop-a", "stop-b"));
var validLockedDates = new Dictionary<string, DateOnly>(StringComparer.Ordinal) { ["SA-A"] = DateOnly.FromDateTime(departure) };
Check("guard accepts an assigned service with unchanged locked departure and local date", InvokeGuardAccepts(salidaGuard, validInitial, validCurrent, validLockedDates));

var remappedToB = new ServicioCompra(new Servicio
{
    Id = "guard-service", TramoId = "guard-tramo", Salida = departure,
    Llegada = departure.AddHours(1), PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica,
    SalidaId = "SA-B", OrdenEmbarque = 1, OrdenDesembarque = 2
}, "stop-a", "stop-b");
Check("guard rejects A-to-B remapping when only A was locked", InvokeGuardRejects(salidaGuard, validInitial, Current(remappedToB), validLockedDates));
Check("guard rejects A-to-B remapping even when both A and B are locked for selected legs", InvokeGuardRejects(salidaGuard, validInitial, Current(remappedToB), new Dictionary<string, DateOnly>(StringComparer.Ordinal) { ["SA-A"] = DateOnly.FromDateTime(departure), ["SA-B"] = DateOnly.FromDateTime(departure) }));
Check("guard rejects an assigned departure missing from the locked-row date map", InvokeGuardRejects(salidaGuard, validInitial, validCurrent, new Dictionary<string, DateOnly>(StringComparer.Ordinal)));
Check("guard rejects a service departure date that differs from its locked Salida date", InvokeGuardRejects(salidaGuard, validInitial, Current(new ServicioCompra(new Servicio
{
    Id = "guard-service", TramoId = "guard-tramo", Salida = departure.AddDays(1), Llegada = departure.AddDays(1).AddHours(1),
    PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica, SalidaId = "SA-A", OrdenEmbarque = 1, OrdenDesembarque = 2
}, "stop-a", "stop-b")), validLockedDates));
Check("guard rejects missing assigned-service departure mapping", InvokeGuardRejects(salidaGuard, validInitial, Current(new ServicioCompra(new Servicio
{
    Id = "guard-service", TramoId = "guard-tramo", Salida = departure, Llegada = departure.AddHours(1),
    PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica, OrdenEmbarque = 1, OrdenDesembarque = 2
}, "stop-a", "stop-b")), validLockedDates));
Check("guard rejects an assigned service absent from the initial mapping snapshot", InvokeGuardRejects(salidaGuard, Initial(), validCurrent, validLockedDates));
Check("guard leaves a no-seat selection without Salida inventory unchanged", InvokeGuardAccepts(salidaGuard,
    Initial(("no-seat", null, PoliticaAsiento.SinAsiento)),
    Current(new ServicioCompra(new Servicio { Id = "no-seat", Salida = departure, Llegada = departure.AddHours(1), PoliticaAsiento = PoliticaAsiento.SinAsiento }, "stop-a", "stop-b")),
    new Dictionary<string, DateOnly>(StringComparer.Ordinal)));

var migrationType = api.GetType("API_Berruti.Data.Migrations.IntentosCompraHold")!;
var migration = (Migration)Activator.CreateInstance(migrationType)!;
var migrationInsert = migration.UpOperations.OfType<InsertDataOperation>().SingleOrDefault(operation => operation.Table == "contador_entidad");
Check("migration inserts only the new IC counter at zero", migrationInsert is not null && migrationInsert.Values.GetLength(0) == 1 && Equals(migrationInsert.Values[0, 0], "IC-") && Equals(migrationInsert.Values[0, 1], 0));
Check("migration does not delete or rewrite previously issued counter rows", !migration.UpOperations.Any(operation => operation is DeleteDataOperation or UpdateDataOperation));
var resetSource = File.ReadAllText("src/Api/API-Berruti/API-Berruti/Data/ReinicioLocal.cs");
Check("reset source does not reseed the IC counter already inserted by the migration (static source check only)", !resetSource.Contains("new ContadorEntidad { Prefijo = \"IC-\" }", StringComparison.Ordinal));
var downBuilder = new MigrationBuilder("MySql.EntityFrameworkCore");
try
{
    migrationType.GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(migration, [downBuilder]);
    Check("migration downgrade fails closed before emitting operations", false);
}
catch (TargetInvocationException exception) when (exception.InnerException is NotSupportedException)
{
    Check("migration downgrade fails closed before emitting operations", downBuilder.Operations.Count == 0);
}

var clock = new ControlledClock(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc));
var contract = new TransactionContractRepository(clock);
contract.AddUser("passenger-public", "US-1");
contract.AddUser("other-public", "US-2");
contract.AddUser("inactive-public", "US-3", active: false);
contract.AddUser("driver-public", "US-4", role: "Cobrador");
var seatService = SeatService("seat-service", "SA-1", 1, 3, "stop-a", "stop-b", departure);
contract.AddService(seatService, "stop-a", "stop-b");
contract.AddInventory("SA-1", Enumerable.Range(1, 46).ToArray());
contract.AddStop("SA-1", 1, "stop-a");
contract.AddStop("SA-1", 3, "stop-b");
var seatQuote = SeatQuote("seat-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 125m);
var seatSearch = new FakeItinerariosService { Offered = [seatQuote] };
var contractService = new IntentosCompraService(contract, seatSearch);
var firstSeatAttempt = await contractService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "seat-service")));
Check("real purchase service creates an automatic-seat attempt through the transaction-contract fake", firstSeatAttempt.IntentoId == "IC-1" && contract.Holds.Count == 1);
Check("46-seat fixture selects the deterministic lowest inventory seat", contract.Holds.Single().Seat == 1);
Check("seat hold is bounded by the attempt's fixed fifteen minute expiry", firstSeatAttempt.HoldVenceEn == clock.GetUtcNow().UtcDateTime.AddMinutes(15) && contract.Holds.Single().Expiry == firstSeatAttempt.HoldVenceEn);
var originalDeadline = firstSeatAttempt.HoldVenceEn;
clock.Advance(TimeSpan.FromMinutes(8));
var readAttempt = await contractService.ConsultarAsync("passenger-public", firstSeatAttempt.IntentoId);
Check("real purchase service read preserves the original deadline after time advances", readAttempt.HoldVenceEn == originalDeadline && readAttempt.Itinerario.PrecioTotal == 125m);
await Expect<KeyNotFoundException>(() => contractService.ConsultarAsync("other-public", firstSeatAttempt.IntentoId), "foreign attempt lookup does not disclose attempt existence");
await Expect<UnauthorizedAccessException>(() => contractService.ConsultarAsync("inactive-public", firstSeatAttempt.IntentoId), "inactive account cannot read a purchase attempt");
await Expect<UnauthorizedAccessException>(() => contractService.ConsultarAsync("driver-public", firstSeatAttempt.IntentoId), "non-passenger role cannot read a purchase attempt");

var expiryClock = new ControlledClock(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc));
var expiryRepo = new TransactionContractRepository(expiryClock);
expiryRepo.AddUser("passenger-public", "US-1");
expiryRepo.AddService(SeatService("expiry-service", "SA-EXP", 1, 3, "stop-a", "stop-b", departure), "stop-a", "stop-b");
expiryRepo.AddInventory("SA-EXP", 1);
expiryRepo.AddStop("SA-EXP", 1, "stop-a");
expiryRepo.AddStop("SA-EXP", 3, "stop-b");
var expiryQuote = SeatQuote("expiry-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 10m, 1, 3, "SA-EXP");
var expiryService = new IntentosCompraService(expiryRepo, new FakeItinerariosService { Offered = [expiryQuote] });
var boundaryBase = await expiryService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "expiry-service")));
await Expect<AsientoNoDisponibleException>(() => expiryService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "expiry-service"))), "overlapping hold blocks the same seat before expiry");
expiryClock.Advance(TimeSpan.FromMinutes(15).Subtract(TimeSpan.FromTicks(1)));
await Expect<AsientoNoDisponibleException>(() => expiryService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "expiry-service"))), "hold remains active one tick before its deadline");
expiryClock.Advance(TimeSpan.FromTicks(1));
var atExactDeadline = await expiryService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "expiry-service")));
Check("hold becomes inactive exactly at its deadline", atExactDeadline.IntentoId == "IC-2" && expiryRepo.Counter == 2);
expiryClock.Advance(TimeSpan.FromMinutes(15));
var afterExpiry = await expiryService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "expiry-service")));
Check("expired history remains durable while a new attempt reuses the released interval", afterExpiry.IntentoId == "IC-3" && expiryRepo.AttemptIds.Count == 3 && expiryRepo.Holds.Count == 3 && boundaryBase.IntentoId == "IC-1");

var noSeat = PurchaseFixtures.Service("no-seat-service", "stop-a", "stop-b", departure, departure.AddHours(1));
contract.AddService(noSeat);
var noSeatQuote = PurchaseFixtures.Itinerary(("no-seat-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 20m));
seatSearch.Offered = [noSeatQuote];
var beforeNoSeat = contract.Holds.Count;
var noSeatAttempt = await contractService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "no-seat-service")));
Check("no-seat selection is retained without inserting a seat hold", noSeatAttempt.Itinerario.Tramos.Single().ServicioId == "no-seat-service" && contract.Holds.Count == beforeNoSeat);

var adjacentRepo = new TransactionContractRepository(new ControlledClock(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc)));
adjacentRepo.AddUser("passenger-public", "US-1");
var adjacentOne = SeatService("adjacent-one", "SA-ADJ", 1, 3, "stop-a", "stop-b", departure);
var adjacentTwo = SeatService("adjacent-two", "SA-ADJ", 3, 5, "stop-b", "stop-c", departure.AddHours(1).AddMinutes(5));
adjacentRepo.AddService(adjacentOne, "stop-a", "stop-b");
adjacentRepo.AddService(adjacentTwo, "stop-b", "stop-c");
adjacentRepo.AddInventory("SA-ADJ", 1);
adjacentRepo.AddStop("SA-ADJ", 1, "stop-a");
adjacentRepo.AddStop("SA-ADJ", 3, "stop-b");
adjacentRepo.AddStop("SA-ADJ", 5, "stop-c");
var adjacentQuote = SeatItinerary(
    ("adjacent-one", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 100m, 1, 3, "SA-ADJ"),
    ("adjacent-two", "stop-b", "B", "stop-c", "C", departure.AddHours(1).AddMinutes(5), departure.AddHours(2), 80m, 3, 5, "SA-ADJ"));
var adjacentService = new IntentosCompraService(adjacentRepo, new FakeItinerariosService { Offered = [adjacentQuote] });
var adjacentAttempt = await adjacentService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "adjacent-one"), (2, "adjacent-two")));
Check("adjacent intervals reuse one seat within a single multi-leg attempt", adjacentAttempt.IntentoId == "IC-1" && adjacentRepo.Holds.Where(hold => hold.AttemptId == adjacentAttempt.IntentoId).Select(hold => hold.Seat).SequenceEqual([1, 1]));

var brokenMapping = SeatService("broken-service", "SA-BAD", 1, 2, "stop-x", "stop-y", departure);
contract.AddService(brokenMapping, "stop-a", "stop-b");
contract.AddInventory("SA-BAD", 1);
contract.AddStop("SA-BAD", 1, "stop-x");
contract.AddStop("SA-BAD", 2, "stop-y");
seatSearch.Offered = [SeatQuote("broken-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 10m, 1, 2, "SA-BAD")];
await Expect<AsientoNoDisponibleException>(() => contractService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "broken-service"))), "service stop mapping mismatch is rejected without guessing endpoints");

var rollbackRepo = new TransactionContractRepository(clock);
rollbackRepo.AddUser("passenger-public", "US-1");
rollbackRepo.AddService(SeatService("rollback-service", "SA-ROLL", 1, 2, "stop-a", "stop-b", departure), "stop-a", "stop-b");
rollbackRepo.AddInventory("SA-ROLL", 1);
rollbackRepo.AddStop("SA-ROLL", 1, "stop-a");
rollbackRepo.AddStop("SA-ROLL", 2, "stop-b");
var rollbackQuote = SeatQuote("rollback-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 10m, 1, 2, "SA-ROLL");
var rollbackService = new IntentosCompraService(rollbackRepo, new FakeItinerariosService { Offered = [rollbackQuote] });
rollbackRepo.FailAfterAllocatingIdentity = true;
await Expect<InvalidOperationException>(() => rollbackService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "rollback-service"))), "transaction-contract fake can inject a failure after provisional identity allocation");
Check("failed attempt rolls back rows and issued IC identity", rollbackRepo.AttemptIds.Count == 0 && rollbackRepo.Counter == 0 && rollbackRepo.Holds.Count == 0);
rollbackRepo.FailAfterAllocatingIdentity = false;
var afterRollback = await rollbackService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "rollback-service")));
Check("next transaction reuses the uncommitted counter value after rollback", afterRollback.IntentoId == "IC-1" && rollbackRepo.Counter == 1);

var raceClock = new ControlledClock(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc));
var raceRepo = new TransactionContractRepository(raceClock);
raceRepo.AddUser("passenger-public", "US-1");
raceRepo.AddService(SeatService("race-service", "SA-RACE", 1, 2, "stop-a", "stop-b", departure), "stop-a", "stop-b");
raceRepo.AddInventory("SA-RACE", 1);
raceRepo.AddStop("SA-RACE", 1, "stop-a");
raceRepo.AddStop("SA-RACE", 2, "stop-b");
var raceQuote = SeatQuote("race-service", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 10m, 1, 2, "SA-RACE");
var raceService = new IntentosCompraService(raceRepo, new FakeItinerariosService { Offered = [raceQuote] });
var race = await Task.WhenAll(
    Capture(() => raceService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "race-service")))),
    Capture(() => raceService.CrearAsync("passenger-public", PurchaseFixtures.Request((1, "race-service")))));
Check("one-seat concurrent transaction-contract calls produce exactly one success and one conflict", race.Count(result => result is IniciarIntentoCompraResponseDto) == 1 && race.Count(result => result is AsientoNoDisponibleException) == 1 && raceRepo.Counter == 1);
Check("multiple required departure locks use stable ordinal ordering", raceRepo.LockedDepartureOrder.SequenceEqual(["SA-RACE"]));

var multiRepo = new TransactionContractRepository(new ControlledClock(new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc)));
multiRepo.AddUser("passenger-public", "US-1");
multiRepo.AddService(SeatService("late-departure", "SA-Z", 1, 2, "stop-b", "stop-c", departure.AddHours(1)), "stop-b", "stop-c");
multiRepo.AddService(SeatService("early-departure", "SA-A", 1, 2, "stop-a", "stop-b", departure), "stop-a", "stop-b");
multiRepo.AddInventory("SA-A", 1);
multiRepo.AddInventory("SA-Z", 1);
multiRepo.AddStop("SA-A", 1, "stop-a");
multiRepo.AddStop("SA-A", 2, "stop-b");
multiRepo.AddStop("SA-Z", 1, "stop-b");
multiRepo.AddStop("SA-Z", 2, "stop-c");
var multiQuote = SeatItinerary(
    ("early-departure", "stop-a", "A", "stop-b", "B", departure, departure.AddHours(1), 10m, 1, 2, "SA-A"),
    ("late-departure", "stop-b", "B", "stop-c", "C", departure.AddHours(1).AddMinutes(10), departure.AddHours(2), 10m, 1, 2, "SA-Z"));
var multiService = new IntentosCompraService(multiRepo, new FakeItinerariosService { Offered = [multiQuote] });
var multiResponse = await multiService.CrearAsync("passenger-public", PurchaseFixtures.Request((9, "late-departure"), (4, "early-departure")));
Check("different departures retain independent seat-one inventories", multiResponse.IntentoId == "IC-1" && multiRepo.Holds.Select(hold => (hold.DepartureId, hold.Seat)).Distinct().Count() == 2);
Check("affected departure rows are locked in ordinal stable order, not request order", multiRepo.LockedDepartureOrder.SequenceEqual(["SA-A", "SA-Z"]));

Servicio SeatService(string id, string departureId, int boarding, int alighting, string origin, string destination, DateTime leave) => new()
{
    Id = id,
    TramoId = $"tramo-{id}",
    Salida = leave,
    Llegada = leave.AddHours(1),
    PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica,
    SalidaId = departureId,
    OrdenEmbarque = boarding,
    OrdenDesembarque = alighting
};

ItinerarioDto SeatQuote(string id, string originId, string origin, string destinationId, string destination,
    DateTime leave, DateTime arrive, decimal amount, int boarding = 1, int alighting = 2, string departureId = "SA-1") =>
    SeatItinerary((id, originId, origin, destinationId, destination, leave, arrive, amount, boarding, alighting, departureId));

ItinerarioDto SeatItinerary(params (string Id, string OriginId, string Origin, string DestinationId, string Destination,
    DateTime Departure, DateTime Arrival, decimal Price, int Boarding, int Alighting, string DepartureId)[] legs)
{
    var tramos = legs.Select((leg, index) => new TramoItinerarioDto
    {
        Orden = index + 1,
        ServicioId = leg.Id,
        TramoId = $"tramo-{leg.Id}",
        Origen = new ParadaDto { Id = leg.OriginId, Nombre = leg.Origin },
        Destino = new ParadaDto { Id = leg.DestinationId, Nombre = leg.Destination },
        Salida = leg.Departure,
        Llegada = leg.Arrival,
        Importe = leg.Price,
        PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica
    }).ToList();
    return new ItinerarioDto { Salida = tramos[0].Salida, Llegada = tramos[^1].Llegada, PrecioTotal = tramos.Sum(leg => leg.Importe), Tramos = tramos };
}

async Task<object> Capture(Func<Task<IniciarIntentoCompraResponseDto>> action)
{
    try { return await action(); }
    catch (Exception exception) { return exception; }
}

bool InvokeGuardAccepts(MethodInfo? guard,
    IReadOnlyDictionary<string, (string? SalidaId, PoliticaAsiento Politica)> initial,
    IReadOnlyDictionary<string, ServicioCompra> current,
    IReadOnlyDictionary<string, DateOnly> lockedDates)
{
    if (guard is null) return false;
    try { guard.Invoke(null, [initial, current, lockedDates]); return true; }
    catch { return false; }
}

bool InvokeGuardRejects(MethodInfo? guard,
    IReadOnlyDictionary<string, (string? SalidaId, PoliticaAsiento Politica)> initial,
    IReadOnlyDictionary<string, ServicioCompra> current,
    IReadOnlyDictionary<string, DateOnly> lockedDates)
{
    if (guard is null) return false;
    try { guard.Invoke(null, [initial, current, lockedDates]); return false; }
    catch (TargetInvocationException exception) { return exception.InnerException is InvalidOperationException; }
}

var failed = checks.Where(check => !check.Passed).ToList();
foreach (var check in checks)
    Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")}: {check.Name}");
Console.WriteLine($"{checks.Count - failed.Count}/{checks.Count} assertions passed.");
return failed.Count == 0 ? 0 : 1;

static TramoIntentoCompra SeatSelection(string attemptId, string serviceId, string departureId,
    int seatNumber, DateTime departureUtc, DateTime arrivalUtc, int boardingOrder = 1, int alightingOrder = 2) => new()
{
    IntentoId = attemptId,
    Orden = 1,
    ServicioId = serviceId,
    TramoId = $"segment-{serviceId}",
    OrigenId = "stop-origin",
    OrigenNombre = "Origin",
    DestinoId = "stop-destination",
    DestinoNombre = "Destination",
    Salida = departureUtc,
    Llegada = arrivalUtc,
    Importe = 100m,
    PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica,
    NumeroAsiento = seatNumber,
    SalidaId = departureId,
    OrdenEmbarque = boardingOrder,
    OrdenDesembarque = alightingOrder
};
