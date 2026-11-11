using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Catalogo;

public class GeneracionServiciosRepository(BerrutiDbContext contexto) : IGeneracionServiciosRepository
{
    public Task<List<Tramo>> ListarTramosAsync() => contexto.Tramos.AsNoTracking().ToListAsync();

    public Task<List<Servicio>> ListarGeneradosAsync(DateTime desde, DateTime hasta) =>
        contexto.Servicios.AsNoTracking()
            .Where(s => s.SalidaCatalogoId != null && s.Salida >= desde && s.Salida < hasta)
            .ToListAsync();

    public async Task GuardarAsync(IReadOnlyList<Tramo> tramosNuevos, IReadOnlyList<Servicio> serviciosNuevos)
    {
        contexto.Tramos.AddRange(tramosNuevos);
        contexto.Servicios.AddRange(serviciosNuevos);
        try
        {
            // Un solo SaveChanges: EF lo envuelve en una transacción, así que no queda una generación a medias.
            await contexto.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            contexto.ChangeTracker.Clear();
            throw new GeneracionConcurrenteException(e);
        }
    }
}
