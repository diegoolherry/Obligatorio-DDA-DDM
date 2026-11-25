using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public sealed class TicketsRepository : ITicketsRepository
{
    private readonly BerrutiDbContext _contexto;

    public TicketsRepository(BerrutiDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<TicketRegistro>> ListarDePasajeroAsync(string identificadorPublico, CancellationToken cancellationToken = default)
    {
        var pasajeroId = await BuscarUsuarioActivoConRolAsync(identificadorPublico, "Pasajero", cancellationToken)
            ?? throw new UnauthorizedAccessException("La identidad no corresponde a un pasajero activo.");
        return await ConsultaDePasajero(_contexto, pasajeroId).ToListAsync(cancellationToken);
    }

    public async Task<string> BuscarCobradorIdAsync(string identificadorPublico, CancellationToken cancellationToken = default) =>
        await BuscarUsuarioActivoConRolAsync(identificadorPublico, "Cobrador", cancellationToken)
            ?? throw new UnauthorizedAccessException("La identidad no corresponde a un cobrador activo.");

    public Task<bool> EstaAsignadoAsync(string cobradorId, string servicioId, CancellationToken cancellationToken = default) =>
        _contexto.AsignacionesCobrador.AsNoTracking().AnyAsync(a => a.UsuarioId == cobradorId && a.ServicioId == servicioId, cancellationToken);

    // Un solo UPDATE condicional: el motor bloquea la fila del ticket y reevalúa «usado_utc IS NULL» con el valor
    // confirmado, así de dos escaneos simultáneos solo uno afecta una fila. Un código inexistente, de otro servicio o
    // de un día anterior afecta cero filas y no modifica nada. qr_opaco se compara con la colación ascii_bin de la columna.
    public async Task<bool> ConsumirAsync(
        string qrOpaco,
        string servicioId,
        string cobradorId,
        DateTime ahoraUtc,
        DateTime inicioDiaLocal,
        CancellationToken cancellationToken = default)
    {
        var filas = await _contexto.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE ticket t
JOIN compra_servicio cs ON cs.compra_servicio_id = t.compra_servicio_id
JOIN tramo_intento_compra ti ON ti.intento_id = cs.intento_id AND ti.orden = cs.orden
SET t.usado_utc = {ahoraUtc}, t.validado_por_usuario_id = {cobradorId}
WHERE t.qr_opaco = {qrOpaco}
  AND t.usado_utc IS NULL
  AND ti.servicio_id = {servicioId}
  AND ti.salida >= {inicioDiaLocal}", cancellationToken);
        return filas == 1;
    }

    public async Task<TicketRegistro?> BuscarPorQrAsync(string qrOpaco, CancellationToken cancellationToken = default) =>
        await ConsultaPorQr(_contexto, qrOpaco).SingleOrDefaultAsync(cancellationToken);

    // El propietario sale de intento_compra.usuario_id: ticket → compra_servicio → intento. Nunca de un dato del cliente.
    public static IQueryable<TicketRegistro> ConsultaDePasajero(BerrutiDbContext contexto, string pasajeroId) =>
        from intento in contexto.IntentosCompra.AsNoTracking()
        where intento.UsuarioId == pasajeroId
        join compraServicio in contexto.Set<CompraServicio>().AsNoTracking() on intento.Id equals compraServicio.IntentoId
        join ticket in contexto.Set<Ticket>().AsNoTracking() on compraServicio.Id equals ticket.CompraServicioId
        join tramo in contexto.TramosIntentoCompra.AsNoTracking()
            on new { compraServicio.IntentoId, compraServicio.Orden } equals new { tramo.IntentoId, tramo.Orden }
        select new TicketRegistro(ticket.Id, ticket.QrOpaco, ticket.UsadoUtc, tramo.Orden, tramo.ServicioId, tramo.TramoId,
            tramo.OrigenNombre, tramo.DestinoNombre, tramo.Salida, tramo.Llegada, tramo.PoliticaAsiento, tramo.NumeroAsiento);

    public static IQueryable<TicketRegistro> ConsultaPorQr(BerrutiDbContext contexto, string qrOpaco) =>
        from ticket in contexto.Set<Ticket>().AsNoTracking()
        where ticket.QrOpaco == qrOpaco
        join compraServicio in contexto.Set<CompraServicio>().AsNoTracking() on ticket.CompraServicioId equals compraServicio.Id
        join tramo in contexto.TramosIntentoCompra.AsNoTracking()
            on new { compraServicio.IntentoId, compraServicio.Orden } equals new { tramo.IntentoId, tramo.Orden }
        select new TicketRegistro(ticket.Id, ticket.QrOpaco, ticket.UsadoUtc, tramo.Orden, tramo.ServicioId, tramo.TramoId,
            tramo.OrigenNombre, tramo.DestinoNombre, tramo.Salida, tramo.Llegada, tramo.PoliticaAsiento, tramo.NumeroAsiento);

    private Task<string?> BuscarUsuarioActivoConRolAsync(string identificadorPublico, string rolNombre, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública es obligatoria.", nameof(identificadorPublico));
        return (from cuenta in _contexto.Usuarios.AsNoTracking()
                join membresia in _contexto.UsuariosRoles.AsNoTracking() on cuenta.Identificador equals membresia.UsuarioId
                join rol in _contexto.Roles.AsNoTracking() on membresia.RolId equals rol.Identificador
                where cuenta.IdentificadorPublico == identificadorPublico && cuenta.Activo && rol.Nombre == rolNombre
                select cuenta.Identificador)
            .Cast<string?>().FirstOrDefaultAsync(cancellationToken);
    }
}
