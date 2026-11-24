using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Location;

namespace API_Berruti.Services.Operational;

// El cobrador autenticado no está asignado al servicio indicado (L9). El controller lo responde como 403.
public sealed class CobradorNoAsignadoException : Exception
{
    public CobradorNoAsignadoException() : base("El cobrador no está asignado a este servicio.") { }
}

// MVP-005. Estado de un ticket: utilizado (tiene marca de uso) > vencido (su día de servicio, en hora de Uruguay, es
// anterior al día actual de Uruguay) > sinUsar. La zona horaria es la académica de la sección "Ubicacion".
public sealed class TicketsService : ITicketsService
{
    // qr_opaco mide hasta 128 caracteres ASCII; el emitido son 64 hexadecimales. Cualquier otra cosa no puede existir.
    private const int LongitudMaximaQr = 128;

    private readonly ITicketsRepository _repositorio;
    private readonly TimeProvider _reloj;
    private readonly TimeZoneInfo _zona;

    public TicketsService(ITicketsRepository repositorio, TimeProvider reloj, ParametrosUbicacion parametros)
    {
        _repositorio = repositorio;
        _reloj = reloj;
        _zona = parametros.ZonaHoraria;
    }

    public async Task<ListaTicketsPasajeroDto> ListarDelPasajeroAsync(string identificadorPublico, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new UnauthorizedAccessException("La identidad del pasajero es obligatoria.");
        var hoy = HoyLocal();
        var registros = await _repositorio.ListarDePasajeroAsync(identificadorPublico, cancellationToken);
        var dtos = registros.Select(r => (Registro: r, Dto: Mapear(r, hoy))).ToList();
        return new ListaTicketsPasajeroDto
        {
            Vigentes = dtos.Where(x => x.Dto.Estado == EstadoTicket.SinUsar)
                .OrderBy(x => x.Registro.Salida).ThenBy(x => x.Registro.Orden).Select(x => x.Dto).ToList(),
            Anteriores = dtos.Where(x => x.Dto.Estado != EstadoTicket.SinUsar)
                .OrderByDescending(x => x.Registro.Salida).ThenByDescending(x => x.Registro.Orden).Select(x => x.Dto).ToList()
        };
    }

    public async Task<ResultadoValidacionTicketDto> ValidarAsync(
        string identificadorPublicoCobrador,
        ValidarTicketRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        if (string.IsNullOrWhiteSpace(identificadorPublicoCobrador))
            throw new UnauthorizedAccessException("La identidad del cobrador es obligatoria.");
        if (string.IsNullOrWhiteSpace(solicitud.ServicioId) || string.IsNullOrWhiteSpace(solicitud.QrOpaco))
            throw new ArgumentException("El código y el tramo son obligatorios.");

        var cobradorId = await _repositorio.BuscarCobradorIdAsync(identificadorPublicoCobrador, cancellationToken);
        var qr = solicitud.QrOpaco.Trim();
        var servicioId = solicitud.ServicioId.Trim();
        // L9: solo en los servicios asignados. Se decide antes de leer o consumir cualquier ticket: el UPDATE no cambia
        // y un cobrador no asignado nunca llega a él. Las asignaciones son sembradas (no hay endpoint que las quite).
        if (!await _repositorio.EstaAsignadoAsync(cobradorId, servicioId, cancellationToken))
            throw new CobradorNoAsignadoException();
        if (!TieneFormatoDeQr(qr)) return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.CodigoInexistente };

        // Microsegundos, la precisión de datetime(6): lo devuelto coincide exactamente con lo guardado.
        var ticks = _reloj.GetUtcNow().UtcDateTime.Ticks;
        var ahoraUtc = new DateTime(ticks - ticks % 10, DateTimeKind.Utc);
        var hoy = HoyLocal();

        // La decisión es esta única sentencia condicional; ninguna lectura previa decide si se consume.
        var consumido = await _repositorio.ConsumirAsync(qr, servicioId, cobradorId, ahoraUtc, hoy, cancellationToken);
        var registro = await _repositorio.BuscarPorQrAsync(qr, cancellationToken);

        if (consumido)
        {
            if (registro is null) throw new InvalidOperationException("El ticket consumido no se pudo leer.");
            return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.Valido, Ticket = Describir(registro), UsadoUtc = ahoraUtc };
        }

        // Prioridad de los rechazos: otro tramo, ya utilizado, vencido (el tramo ajeno manda aunque el ticket esté usado).
        if (registro is null) return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.CodigoInexistente };
        if (!string.Equals(registro.ServicioId, servicioId, StringComparison.OrdinalIgnoreCase))
            return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.TramoIncorrecto, Ticket = Describir(registro) };
        if (registro.UsadoUtc is not null)
            return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.YaUtilizado, Ticket = Describir(registro), UsadoUtc = registro.UsadoUtc };
        if (registro.Salida.Date < hoy)
            return new ResultadoValidacionTicketDto { Resultado = ResultadoValidacion.Vencido, Ticket = Describir(registro) };
        // Sin uso, del tramo y vigente, pero el UPDATE no lo consumió: estado imposible; nunca se informa éxito.
        throw new InvalidOperationException("El ticket cumple las condiciones pero no se consumió.");
    }

    // Día actual en Uruguay a las 00:00 (kind sin especificar: igual que Servicio.Salida).
    private DateTime HoyLocal() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(_reloj.GetUtcNow().UtcDateTime, _zona).Date, DateTimeKind.Unspecified);

    private static bool TieneFormatoDeQr(string qr) =>
        qr.Length is > 0 and <= LongitudMaximaQr && qr.All(char.IsAsciiLetterOrDigit);

    private static TicketPasajeroDto Mapear(TicketRegistro registro, DateTime hoy) => new()
    {
        TicketId = registro.TicketId,
        Estado = registro.UsadoUtc is not null ? EstadoTicket.Utilizado
            : registro.Salida.Date < hoy ? EstadoTicket.Vencido
            : EstadoTicket.SinUsar,
        ServicioId = registro.ServicioId,
        TramoId = registro.TramoId,
        OrigenNombre = registro.OrigenNombre,
        DestinoNombre = registro.DestinoNombre,
        Salida = registro.Salida,
        Llegada = registro.Llegada,
        PoliticaAsiento = registro.PoliticaAsiento,
        NumeroAsiento = registro.NumeroAsiento,
        QrOpaco = registro.QrOpaco,
        UsadoUtc = registro.UsadoUtc
    };

    private static TicketValidadoDto Describir(TicketRegistro registro) => new()
    {
        TicketId = registro.TicketId,
        ServicioId = registro.ServicioId,
        TramoId = registro.TramoId,
        OrigenNombre = registro.OrigenNombre,
        DestinoNombre = registro.DestinoNombre,
        Salida = registro.Salida,
        Llegada = registro.Llegada
    };
}
