using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public sealed class IntentosPendientesRepository : IIntentosPendientesRepository
{
    private readonly BerrutiDbContext contexto;

    public IntentosPendientesRepository(BerrutiDbContext contexto)
    {
        this.contexto = contexto;
    }

    // Requiere atención: (a) pago en seguimiento de devolución, o (b) intento sin compra confirmada con
    // hold vigente o con un pago Pendiente/Desconocido/Aprobado (aprobado sin compra sigue sin confirmar).
    public async Task<string?> BuscarIdPendienteAsync(
        string identificadorPublico,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe estar en UTC.", nameof(ahoraUtc));

        var pasajeroId = await (from cuenta in contexto.Usuarios.AsNoTracking()
                                join membresia in contexto.UsuariosRoles.AsNoTracking() on cuenta.Identificador equals membresia.UsuarioId
                                join rol in contexto.Roles.AsNoTracking() on membresia.RolId equals rol.Identificador
                                where cuenta.IdentificadorPublico == identificadorPublico && cuenta.Activo && rol.Nombre == "Pasajero"
                                select cuenta.Identificador)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("La identidad no corresponde a un pasajero activo.");

        var vivos = await contexto.IntentosCompra.AsNoTracking()
            .Where(intento => intento.UsuarioId == pasajeroId && intento.HoldVenceUtc > ahoraUtc)
            .Select(intento => intento.Id)
            .ToListAsync(cancellationToken);
        var pagos = await (from pago in contexto.Set<Pago>().AsNoTracking()
                           join intento in contexto.IntentosCompra.AsNoTracking() on pago.IntentoId equals intento.Id
                           where intento.UsuarioId == pasajeroId
                               && (pago.Estado == EstadoPago.Pendiente || pago.Estado == EstadoPago.Desconocido
                                   || pago.Estado == EstadoPago.Aprobado || pago.Estado == EstadoPago.DevolucionPendiente)
                           select new { pago.IntentoId, pago.Estado })
            .ToListAsync(cancellationToken);

        var candidatos = vivos.Concat(pagos.Select(pago => pago.IntentoId)).Distinct(StringComparer.Ordinal).ToList();
        if (candidatos.Count == 0) return null;

        var comprados = (await contexto.Set<Compra>().AsNoTracking()
                .Where(compra => candidatos.Contains(compra.IntentoId))
                .Select(compra => compra.IntentoId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var enDevolucion = pagos.Where(pago => pago.Estado == EstadoPago.DevolucionPendiente)
            .Select(pago => pago.IntentoId).ToHashSet(StringComparer.Ordinal);
        var elegibles = candidatos.Where(id => !comprados.Contains(id) || enDevolucion.Contains(id)).ToList();
        if (elegibles.Count == 0) return null;

        var intentos = await contexto.IntentosCompra.AsNoTracking()
            .Where(intento => elegibles.Contains(intento.Id))
            .Select(intento => new { intento.Id, intento.CreadoUtc })
            .ToListAsync(cancellationToken);
        return intentos.OrderByDescending(intento => intento.CreadoUtc)
            .ThenByDescending(intento => intento.Id.Length)
            .ThenByDescending(intento => intento.Id, StringComparer.Ordinal)
            .First().Id;
    }
}
