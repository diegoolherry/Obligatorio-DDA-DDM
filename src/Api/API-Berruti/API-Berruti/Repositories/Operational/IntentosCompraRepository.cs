using System.Data;
using API_Berruti.Data;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Authentication;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Identifiers;
using API_Berruti.Services.Operational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API_Berruti.Repositories.Operational;

public class IntentosCompraRepository : IIntentosCompraRepository
{
    private const string PrefijoIntento = "IC-";
    private readonly BerrutiDbContext contexto;
    private readonly IContadoresRepository contadores;
    private readonly ParametrosCompra parametros;
    private readonly TimeProvider reloj;

    public IntentosCompraRepository(
        BerrutiDbContext contexto,
        IContadoresRepository contadores,
        ParametrosCompra parametros,
        TimeProvider reloj)
    {
        this.contexto = contexto;
        this.contadores = contadores;
        this.parametros = parametros;
        this.reloj = reloj;
    }

    public async Task<IReadOnlyList<ServicioCompra>> BuscarServiciosAsync(IReadOnlyCollection<string> servicioIds)
    {
        if (servicioIds.Count == 0) return [];
        var ids = servicioIds.Distinct(StringComparer.Ordinal).ToArray();
        return await (from servicio in contexto.Servicios.AsNoTracking()
                      join tramo in contexto.Tramos.AsNoTracking() on servicio.TramoId equals tramo.Id
                      where ids.Contains(servicio.Id)
                      select new ServicioCompra(servicio, tramo.OrigenId, tramo.DestinoId))
            .ToListAsync();
    }

    public async Task<IniciarIntentoCompraResponseDto> CrearAsync(
        string identificadorPublico,
        ItinerarioDto itinerario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itinerario);
        if (contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("El contexto debe estar libre de cambios pendientes.");
        if (itinerario.Tramos.Count == 0)
            throw new ArgumentException("El itinerario debe contener al menos un tramo.", nameof(itinerario));

        await using var transaccion = await contexto.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var agregado = new List<TramoIntentoCompra>();
        var holds = new List<HoldAsiento>();
        IntentoCompra? intento = null;
        try
        {
            var pasajeroId = await BuscarPasajeroActivoAsync(identificadorPublico, cancellationToken);
            var cotizacionPorServicio = itinerario.Tramos.ToDictionary(tramo => tramo.ServicioId, StringComparer.Ordinal);
            var iniciales = await contexto.Servicios.AsNoTracking()
                .Where(servicio => cotizacionPorServicio.Keys.Contains(servicio.Id))
                .Select(servicio => new { servicio.Id, servicio.SalidaId, servicio.PoliticaAsiento })
                .ToListAsync(cancellationToken);
            if (iniciales.Count != itinerario.Tramos.Count)
                throw new InvalidOperationException("El itinerario ofrecido cambió antes de crear el intento.");

            var salidasInicialesPorServicio = iniciales.ToDictionary(
                item => item.Id,
                item => (item.SalidaId, item.PoliticaAsiento),
                StringComparer.Ordinal);
            var salidas = iniciales.Where(item => item.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
                .Select(item => item.SalidaId)
                .Where(id => id is not null)
                .Select(id => id!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
            var fechasSalidasBloqueadas = new Dictionary<string, DateOnly>(StringComparer.Ordinal);
            foreach (var salidaId in salidas)
                fechasSalidasBloqueadas.Add(salidaId, await BloquearSalidaAsync(salidaId, transaccion, cancellationToken));

            var serviciosActuales = await BuscarServiciosAsync(cotizacionPorServicio.Keys.ToArray());
            if (serviciosActuales.Count != itinerario.Tramos.Count)
                throw new InvalidOperationException("El itinerario ofrecido cambió durante la reserva del intento.");
            var serviciosPorId = serviciosActuales.ToDictionary(item => item.Servicio.Id, StringComparer.Ordinal);
            ValidarSalidasYFechasBloqueadas(salidasInicialesPorServicio, serviciosPorId, fechasSalidasBloqueadas);

            foreach (var cotizado in itinerario.Tramos)
            {
                if (!serviciosPorId.TryGetValue(cotizado.ServicioId, out var actual))
                    throw new InvalidOperationException("El servicio cotizado ya no está disponible.");
                ValidarServicioCotizado(cotizado, actual);
                if (actual.Servicio.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
                {
                    if (actual.Servicio.SalidaId is null || actual.Servicio.OrdenEmbarque is null || actual.Servicio.OrdenDesembarque is null)
                        throw new AsientoNoDisponibleException("El servicio no tiene una salida y un intervalo de asiento válidos.");
                    await ValidarParadasServicioAsync(actual.Servicio, actual.OrigenId, actual.DestinoId, cancellationToken);
                }
            }

            var ahoraUtc = TruncarMicrosegundos(reloj.GetUtcNow().UtcDateTime);
            intento = new IntentoCompra
            {
                UsuarioId = pasajeroId,
                CreadoUtc = ahoraUtc,
                HoldVenceUtc = TruncarMicrosegundos(ahoraUtc.AddMinutes(parametros.HoldMinutos))
            };
            intento.Id = await contadores.IncrementarAsync(PrefijoIntento);

            var asignacionesPendientes = new List<(string SalidaId, int Asiento, int Embarque, int Desembarque)>();
            foreach (var cotizado in itinerario.Tramos.OrderBy(tramo => tramo.Orden))
            {
                if (!serviciosPorId.TryGetValue(cotizado.ServicioId, out var actual))
                    throw new InvalidOperationException("El servicio cotizado ya no está disponible.");
                ValidarServicioCotizado(cotizado, actual);

                var servicio = actual.Servicio;
                int? numeroAsiento = null;
                if (servicio.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
                {
                    if (servicio.SalidaId is null || servicio.OrdenEmbarque is null || servicio.OrdenDesembarque is null)
                        throw new AsientoNoDisponibleException("El servicio no tiene una salida y un intervalo de asiento válidos.");
                    await ValidarParadasServicioAsync(servicio, actual.OrigenId, actual.DestinoId, cancellationToken);
                    numeroAsiento = await SeleccionarAsientoAsync(intento.Id, servicio, asignacionesPendientes, ahoraUtc, cancellationToken);
                    asignacionesPendientes.Add((servicio.SalidaId, numeroAsiento.Value, servicio.OrdenEmbarque.Value, servicio.OrdenDesembarque.Value));
                }

                agregado.Add(new TramoIntentoCompra
                {
                    IntentoId = intento.Id,
                    Orden = cotizado.Orden,
                    ServicioId = servicio.Id,
                    TramoId = servicio.TramoId,
                    OrigenId = cotizado.Origen.Id ?? throw new InvalidOperationException("La cotización no contiene el identificador del origen."),
                    OrigenNombre = cotizado.Origen.Nombre ?? string.Empty,
                    DestinoId = cotizado.Destino.Id ?? throw new InvalidOperationException("La cotización no contiene el identificador del destino."),
                    DestinoNombre = cotizado.Destino.Nombre ?? string.Empty,
                    Salida = servicio.Salida,
                    Llegada = servicio.Llegada,
                    Importe = cotizado.Importe,
                    PoliticaAsiento = servicio.PoliticaAsiento,
                    NumeroAsiento = numeroAsiento,
                    SalidaId = numeroAsiento is null ? null : servicio.SalidaId,
                    OrdenEmbarque = numeroAsiento is null ? null : servicio.OrdenEmbarque,
                    OrdenDesembarque = numeroAsiento is null ? null : servicio.OrdenDesembarque
                });

                if (numeroAsiento is not null)
                    holds.Add(new HoldAsiento
                    {
                        IntentoId = intento.Id,
                        Orden = cotizado.Orden,
                        SalidaId = servicio.SalidaId!,
                        NumeroAsiento = numeroAsiento.Value,
                        OrdenEmbarque = servicio.OrdenEmbarque!.Value,
                        OrdenDesembarque = servicio.OrdenDesembarque!.Value
                    });
            }

            contexto.IntentosCompra.Add(intento);
            contexto.TramosIntentoCompra.AddRange(agregado);
            contexto.HoldsAsiento.AddRange(holds);
            await contexto.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return new IniciarIntentoCompraResponseDto
            {
                IntentoId = intento.Id,
                Itinerario = itinerario,
                HoldVenceEn = intento.HoldVenceUtc
            };
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(cancellationToken);
            }
            finally
            {
                foreach (var hold in holds) contexto.Entry(hold).State = EntityState.Detached;
                foreach (var tramo in agregado) contexto.Entry(tramo).State = EntityState.Detached;
                if (intento is not null) contexto.Entry(intento).State = EntityState.Detached;
            }
            throw;
        }
    }

    public async Task<IniciarIntentoCompraResponseDto> ConsultarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var pasajeroId = await BuscarPasajeroActivoAsync(identificadorPublico, cancellationToken);
        var intento = await contexto.IntentosCompra.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == intentoId && item.UsuarioId == pasajeroId, cancellationToken)
            ?? throw new KeyNotFoundException("El intento de compra no existe.");
        var selecciones = await contexto.TramosIntentoCompra.AsNoTracking()
            .Where(tramo => tramo.IntentoId == intento.Id)
            .OrderBy(tramo => tramo.Orden)
            .ToListAsync(cancellationToken);
        return new IniciarIntentoCompraResponseDto
        {
            IntentoId = intento.Id,
            HoldVenceEn = intento.HoldVenceUtc,
            Itinerario = ReconstruirItinerario(selecciones)
        };
    }

    private async Task<string> BuscarPasajeroActivoAsync(string identificadorPublico, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        var usuario = await (from cuenta in contexto.Usuarios.AsNoTracking()
                             join membresia in contexto.UsuariosRoles.AsNoTracking() on cuenta.Identificador equals membresia.UsuarioId
                             join rol in contexto.Roles.AsNoTracking() on membresia.RolId equals rol.Identificador
                             where cuenta.IdentificadorPublico == identificadorPublico && cuenta.Activo && rol.Nombre == "Pasajero"
                             select cuenta.Identificador)
            .SingleOrDefaultAsync(cancellationToken);
        return usuario ?? throw new UnauthorizedAccessException("La identidad no corresponde a un pasajero activo.");
    }

    private async Task<DateOnly> BloquearSalidaAsync(string salidaId, IDbContextTransaction transaccion, CancellationToken cancellationToken)
    {
        await using var comando = contexto.Database.GetDbConnection().CreateCommand();
        comando.Transaction = transaccion.GetDbTransaction();
        comando.CommandText = "SELECT fecha FROM salida WHERE salida_id = @salidaId FOR UPDATE";
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@salidaId";
        parametro.Value = salidaId;
        comando.Parameters.Add(parametro);
        var resultado = await comando.ExecuteScalarAsync(cancellationToken);
        if (resultado is null || resultado == DBNull.Value)
            throw new AsientoNoDisponibleException("La salida seleccionada ya no existe.");
        return resultado switch
        {
            DateTime fecha => DateOnly.FromDateTime(fecha),
            DateOnly fecha => fecha,
            _ => throw new InvalidOperationException("La fecha de salida tiene un tipo inesperado.")
        };
    }

    internal static void ValidarSalidasYFechasBloqueadas(
        IReadOnlyDictionary<string, (string? SalidaId, PoliticaAsiento Politica)> salidasInicialesPorServicio,
        IReadOnlyDictionary<string, ServicioCompra> serviciosActuales,
        IReadOnlyDictionary<string, DateOnly> fechasSalidasBloqueadas)
    {
        foreach (var (servicioId, inicial) in salidasInicialesPorServicio)
        {
            if (!serviciosActuales.TryGetValue(servicioId, out var actual))
                throw new InvalidOperationException("El servicio desapareció al revalidar el itinerario.");
            var servicio = actual.Servicio;
            if (servicio.PoliticaAsiento != inicial.Politica)
                throw new InvalidOperationException("La política de asiento cambió durante la creación del intento.");
            if (inicial.Politica != PoliticaAsiento.AsignacionAutomatica) continue;
            if (string.IsNullOrWhiteSpace(inicial.SalidaId) || string.IsNullOrWhiteSpace(servicio.SalidaId))
                throw new AsientoNoDisponibleException("El servicio asignado no tiene una salida estable.");
            if (!string.Equals(servicio.SalidaId, inicial.SalidaId, StringComparison.Ordinal))
                throw new InvalidOperationException("La salida del servicio cambió después de adquirir sus locks.");
            if (!fechasSalidasBloqueadas.TryGetValue(inicial.SalidaId, out var fechaBloqueada))
                throw new AsientoNoDisponibleException("No se adquirió el lock de la salida asignada al servicio.");
            if (DateOnly.FromDateTime(servicio.Salida) != fechaBloqueada)
                throw new AsientoNoDisponibleException("La fecha local del servicio no coincide con la fecha de su salida.");
        }

        foreach (var (servicioId, actual) in serviciosActuales)
            if (!salidasInicialesPorServicio.ContainsKey(servicioId) && actual.Servicio.PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica)
                throw new InvalidOperationException("Se encontró un servicio asignado fuera del conjunto inicial bloqueado.");
    }

    private async Task ValidarParadasServicioAsync(Servicio servicio, string origenId, string destinoId, CancellationToken cancellationToken)
    {
        var origen = await contexto.ParadasSalida.AsNoTracking().SingleOrDefaultAsync(
            parada => parada.SalidaId == servicio.SalidaId && parada.Orden == servicio.OrdenEmbarque, cancellationToken);
        var destino = await contexto.ParadasSalida.AsNoTracking().SingleOrDefaultAsync(
            parada => parada.SalidaId == servicio.SalidaId && parada.Orden == servicio.OrdenDesembarque, cancellationToken);
        if (origen is null || destino is null || origen.ParadaId != origenId || destino.ParadaId != destinoId ||
            servicio.OrdenEmbarque >= servicio.OrdenDesembarque)
            throw new AsientoNoDisponibleException("El servicio no coincide con las ocurrencias ordenadas de su salida.");
    }

    private async Task<int> SeleccionarAsientoAsync(
        string intentoId,
        Servicio servicio,
        IReadOnlyCollection<(string SalidaId, int Asiento, int Embarque, int Desembarque)> asignacionesPendientes,
        DateTime ahoraUtc,
        CancellationToken cancellationToken)
    {
        var salidaId = servicio.SalidaId!;
        var embarque = servicio.OrdenEmbarque!.Value;
        var desembarque = servicio.OrdenDesembarque!.Value;
        var inventario = await contexto.AsientosSalida.AsNoTracking()
            .Where(asiento => asiento.SalidaId == salidaId)
            .OrderBy(asiento => asiento.NumeroAsiento)
            .Select(asiento => asiento.NumeroAsiento)
            .ToListAsync(cancellationToken);
        var activos = await (from hold in contexto.HoldsAsiento.AsNoTracking()
                             join intento in contexto.IntentosCompra.AsNoTracking() on hold.IntentoId equals intento.Id
                             where hold.SalidaId == salidaId && intento.HoldVenceUtc > ahoraUtc &&
                                   hold.OrdenEmbarque < desembarque && embarque < hold.OrdenDesembarque
                             select new { hold.NumeroAsiento, hold.OrdenEmbarque, hold.OrdenDesembarque })
            .ToListAsync(cancellationToken);
        var candidato = new TramoIntentoCompra
        {
            IntentoId = intentoId,
            SalidaId = salidaId,
            PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica,
            OrdenEmbarque = embarque,
            OrdenDesembarque = desembarque
        };
        var asientosConfirmados = await OcupacionAsientosCompra.TicketsConfirmados(
                contexto.Set<Ticket>().AsNoTracking(),
                contexto.Set<CompraServicio>().AsNoTracking(),
                contexto.TramosIntentoCompra.AsNoTracking())
            .Where(OcupacionAsientosCompra.IntervalosSuperpuestos(candidato))
            .Where(tramo => tramo.NumeroAsiento.HasValue)
            .Select(tramo => tramo.NumeroAsiento!.Value)
            .ToListAsync(cancellationToken);

        foreach (var asiento in inventario)
        {
            var conflictoPersistido = activos.Any(hold => hold.NumeroAsiento == asiento &&
                IntervalsOverlap(embarque, desembarque, hold.OrdenEmbarque, hold.OrdenDesembarque));
            var conflictoConfirmado = asientosConfirmados.Contains(asiento);
            var conflictoPendiente = asignacionesPendientes.Any(hold => hold.SalidaId == salidaId && hold.Asiento == asiento &&
                IntervalsOverlap(embarque, desembarque, hold.Embarque, hold.Desembarque));
            if (!conflictoPersistido && !conflictoConfirmado && !conflictoPendiente) return asiento;
        }
        throw new AsientoNoDisponibleException("No hay un asiento disponible para el intervalo solicitado.");
    }

    private static void ValidarServicioCotizado(TramoItinerarioDto cotizado, ServicioCompra actual)
    {
        var servicio = actual.Servicio;
        if (servicio.Id != cotizado.ServicioId || servicio.TramoId != cotizado.TramoId ||
            servicio.Salida != cotizado.Salida || servicio.Llegada != cotizado.Llegada ||
            servicio.PoliticaAsiento != cotizado.PoliticaAsiento ||
            actual.OrigenId != cotizado.Origen.Id || actual.DestinoId != cotizado.Destino.Id)
            throw new InvalidOperationException("El servicio ofrecido cambió antes de guardar el intento.");
        if (cotizado.Importe < 0)
            throw new InvalidOperationException("La cotización ofrecida contiene un importe inválido.");
    }

    private static ItinerarioDto ReconstruirItinerario(IReadOnlyList<TramoIntentoCompra> tramos)
    {
        if (tramos.Count == 0) throw new InvalidOperationException("El intento guardado no contiene tramos.");
        var dto = tramos.Select(tramo => new TramoItinerarioDto
        {
            Orden = tramo.Orden,
            ServicioId = tramo.ServicioId,
            TramoId = tramo.TramoId,
            Origen = new ParadaDto { Id = tramo.OrigenId, Nombre = tramo.OrigenNombre },
            Destino = new ParadaDto { Id = tramo.DestinoId, Nombre = tramo.DestinoNombre },
            Salida = tramo.Salida,
            Llegada = tramo.Llegada,
            Importe = tramo.Importe,
            PoliticaAsiento = tramo.PoliticaAsiento
        }).ToList();
        var primero = dto[0];
        var ultimo = dto[^1];
        var transbordo = dto.Count > 1
            ? new TransbordoDto { Parada = primero.Destino, Minutos = (int)(dto[1].Salida - primero.Llegada).TotalMinutes }
            : null;
        return new ItinerarioDto
        {
            Salida = primero.Salida,
            Llegada = ultimo.Llegada,
            PrecioTotal = dto.Sum(tramo => tramo.Importe),
            Transbordo = transbordo,
            Tramos = dto
        };
    }

    private static DateTime TruncarMicrosegundos(DateTime valorUtc) =>
        DateTime.SpecifyKind(new DateTime(valorUtc.Ticks - valorUtc.Ticks % 10, DateTimeKind.Utc), DateTimeKind.Utc);

    internal static bool IntervalsOverlap(int firstBoarding, int firstAlighting, int secondBoarding, int secondAlighting) =>
        firstBoarding < secondAlighting && secondBoarding < firstAlighting;
}
