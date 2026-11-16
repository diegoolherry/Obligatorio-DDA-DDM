using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;

internal sealed class FakePurchaseRepository : IIntentosCompraRepository
{
    public List<ServicioCompra> Services { get; } = [];
    public IniciarIntentoCompraResponseDto? LastCreated { get; private set; }
    public int CreateCount { get; private set; }

    public Task<IReadOnlyList<ServicioCompra>> BuscarServiciosAsync(IReadOnlyCollection<string> servicioIds)
    {
        IReadOnlyList<ServicioCompra> found = Services.Where(item => servicioIds.Contains(item.Servicio.Id, StringComparer.Ordinal)).ToList();
        return Task.FromResult(found);
    }

    public Task<IniciarIntentoCompraResponseDto> CrearAsync(string identificadorPublico, ItinerarioDto itinerario, CancellationToken cancellationToken = default)
    {
        CreateCount++;
        LastCreated = new IniciarIntentoCompraResponseDto { IntentoId = $"IC-{CreateCount}", Itinerario = itinerario, HoldVenceEn = DateTime.UtcNow.AddMinutes(15) };
        return Task.FromResult(LastCreated);
    }

    public Task<IniciarIntentoCompraResponseDto> ConsultarAsync(string identificadorPublico, string intentoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(LastCreated ?? throw new KeyNotFoundException());
}

internal sealed class FakeItinerariosService : IItinerariosService
{
    public IReadOnlyList<ItinerarioDto> Offered { get; set; } = [];
    public string? Origin { get; private set; }
    public string? Destination { get; private set; }
    public DateOnly Date { get; private set; }

    public Task<IReadOnlyList<ItinerarioDto>> BuscarAsync(string? origenId, string? destinoId, DateOnly fecha)
    {
        Origin = origenId;
        Destination = destinoId;
        Date = fecha;
        return Task.FromResult(Offered);
    }
}

internal static class PurchaseFixtures
{
    public static IniciarIntentoCompraRequestDto Request(params (int Order, string ServiceId)[] selections) => new()
    {
        Tramos = selections.Select(selection => new SeleccionTramoCompraDto { Orden = selection.Order, ServicioId = selection.ServiceId }).ToList()
    };

    public static ServicioCompra Service(string id, string origin, string destination, DateTime departure, DateTime arrival) =>
        new(new Servicio
        {
            Id = id,
            TramoId = $"tramo-{id}",
            Salida = departure,
            Llegada = arrival,
            PoliticaAsiento = PoliticaAsiento.SinAsiento
        }, origin, destination);

    public static ItinerarioDto Itinerary(params (string Id, string OriginId, string Origin, string DestinationId, string Destination, DateTime Departure, DateTime Arrival, decimal Price)[] legs)
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
            PoliticaAsiento = PoliticaAsiento.SinAsiento
        }).ToList();
        return new ItinerarioDto
        {
            Salida = tramos[0].Salida,
            Llegada = tramos[^1].Llegada,
            PrecioTotal = tramos.Sum(leg => leg.Importe),
            Tramos = tramos
        };
    }
}

internal sealed class ControlledClock(DateTime initialUtc) : TimeProvider
{
    public DateTimeOffset Now { get; private set; } = new(DateTime.SpecifyKind(initialUtc, DateTimeKind.Utc));
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(TimeSpan duration) => Now = Now.Add(duration);
}

internal sealed class TransactionContractRepository(ControlledClock clock) : IIntentosCompraRepository
{
    private readonly SemaphoreSlim _transaction = new(1, 1);
    private readonly Dictionary<string, (string UserId, bool Active, string Role)> _users = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IniciarIntentoCompraResponseDto> _attempts = new(StringComparer.Ordinal);
    private readonly List<(string AttemptId, string DepartureId, int Seat, int Boarding, int Alighting, DateTime Expiry)> _holds = [];
    private readonly Dictionary<string, HashSet<int>> _inventory = new(StringComparer.Ordinal);
    private readonly Dictionary<(string DepartureId, int Order), string> _stops = [];
    private readonly List<string> _services = [];
    private int _counter;

    public bool FailAfterAllocatingIdentity { get; set; }
    public IReadOnlyCollection<(string AttemptId, string DepartureId, int Seat, int Boarding, int Alighting, DateTime Expiry)> Holds => _holds;
    public IReadOnlyCollection<string> AttemptIds => _attempts.Keys;
    public IReadOnlyList<string> LockedDepartureOrder { get; private set; } = [];
    public int Counter => _counter;

    public void AddUser(string publicId, string internalId, string role = "Pasajero", bool active = true) => _users[publicId] = (internalId, active, role);
    public void AddService(ServicioCompra service)
    {
        _services.Add(service.Servicio.Id);
        Records[service.Servicio.Id] = service;
    }
    public void AddInventory(string departureId, params int[] seats) => _inventory[departureId] = seats.ToHashSet();
    public void AddStop(string departureId, int order, string stopId) => _stops[(departureId, order)] = stopId;

    public Task<IReadOnlyList<ServicioCompra>> BuscarServiciosAsync(IReadOnlyCollection<string> serviceIds) =>
        Task.FromResult<IReadOnlyList<ServicioCompra>>(_services
            .Where(serviceIds.Contains)
            .Select(id => new ServicioCompra(Records[id].Servicio, Records[id].OrigenId, Records[id].DestinoId))
            .ToList());

    public Dictionary<string, ServicioCompra> Records { get; } = new(StringComparer.Ordinal);
    public void AddService(Servicio service, string origin, string destination)
    {
        _services.Add(service.Id);
        Records[service.Id] = new ServicioCompra(service, origin, destination);
    }

    public async Task<IniciarIntentoCompraResponseDto> CrearAsync(string publicId, ItinerarioDto itinerary, CancellationToken cancellationToken = default)
    {
        await _transaction.WaitAsync(cancellationToken);
        try
        {
            var user = ActivePassenger(publicId);
            var selected = itinerary.Tramos.Select(leg => (Leg: leg, Service: Records[leg.ServicioId].Servicio)).OrderBy(item => item.Leg.Orden).ToList();
            var departures = selected.Where(item => item.Service.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
                .Select(item => item.Service.SalidaId!).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();
            LockedDepartureOrder = departures;

            var allocated = new List<(string DepartureId, int Seat, int Boarding, int Alighting)>();
            foreach (var item in selected.Where(item => item.Service.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica))
            {
                var service = item.Service;
                if (service.SalidaId is null || service.OrdenEmbarque is null || service.OrdenDesembarque is null ||
                    !_stops.TryGetValue((service.SalidaId, service.OrdenEmbarque.Value), out var boarding) || boarding != item.Leg.Origen.Id ||
                    !_stops.TryGetValue((service.SalidaId, service.OrdenDesembarque.Value), out var alighting) || alighting != item.Leg.Destino.Id ||
                    service.OrdenEmbarque >= service.OrdenDesembarque)
                    throw new AsientoNoDisponibleException("The fixture has no valid service stop mapping.");

                var seat = _inventory[service.SalidaId].Order().FirstOrDefault(candidate =>
                {
                    var persistedConflict = _holds.Any(hold => hold.DepartureId == service.SalidaId && hold.Seat == candidate && hold.Expiry > clock.GetUtcNow().UtcDateTime &&
                        Overlap(service.OrdenEmbarque.Value, service.OrdenDesembarque.Value, hold.Boarding, hold.Alighting));
                    var pendingConflict = allocated.Any(hold => hold.DepartureId == service.SalidaId && hold.Seat == candidate &&
                        Overlap(service.OrdenEmbarque.Value, service.OrdenDesembarque.Value, hold.Boarding, hold.Alighting));
                    return !persistedConflict && !pendingConflict;
                });
                if (seat == 0) throw new AsientoNoDisponibleException("No seat is available for the requested interval.");
                allocated.Add((service.SalidaId, seat, service.OrdenEmbarque.Value, service.OrdenDesembarque.Value));
            }

            var nextId = $"IC-{_counter + 1}";
            var expiry = clock.GetUtcNow().UtcDateTime.AddMinutes(15);
            if (FailAfterAllocatingIdentity) throw new InvalidOperationException("Injected transaction failure after provisional identity allocation.");

            _counter++;
            _attempts.Add(nextId, new IniciarIntentoCompraResponseDto { IntentoId = nextId, Itinerario = itinerary, HoldVenceEn = expiry });
            _owners.Add(nextId, user);
            foreach (var allocation in allocated)
                _holds.Add((nextId, allocation.DepartureId, allocation.Seat, allocation.Boarding, allocation.Alighting, expiry));
            return _attempts[nextId];
        }
        finally { _transaction.Release(); }
    }

    public Task<IniciarIntentoCompraResponseDto> ConsultarAsync(string publicId, string attemptId, CancellationToken cancellationToken = default)
    {
        var user = ActivePassenger(publicId);
        if (!_attempts.TryGetValue(attemptId, out var attempt) || _attempts[attemptId].Itinerario.Tramos.Count == 0 ||
            !_owners.TryGetValue(attemptId, out var owner) || owner != user)
            throw new KeyNotFoundException("The attempt does not exist.");
        return Task.FromResult(attempt);
    }

    private readonly Dictionary<string, string> _owners = new(StringComparer.Ordinal);
    private string ActivePassenger(string publicId)
    {
        if (!_users.TryGetValue(publicId, out var user) || !user.Active || user.Role != "Pasajero")
            throw new UnauthorizedAccessException();
        return user.UserId;
    }

    private static bool Overlap(int a, int b, int c, int d) => a < d && c < b;
}
