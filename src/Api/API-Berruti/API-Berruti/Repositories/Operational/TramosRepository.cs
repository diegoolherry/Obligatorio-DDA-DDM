using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public class TramosRepository(BerrutiDbContext contexto) : ITramosRepository
{
    public Task<List<Tramo>> ListarAsync() => contexto.Tramos.AsNoTracking().OrderBy(t => t.OrigenId).ThenBy(t => t.DestinoId).ToListAsync();
    public Task<Tramo?> BuscarAsync(string id) => contexto.Tramos.FirstOrDefaultAsync(t => t.Id == id);
    public Task<bool> ExisteParadaAsync(string id) => contexto.Paradas.AnyAsync(p => p.Id == id);
    public Task<Parada?> BuscarParadaAsync(string id) => contexto.Paradas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    public async Task AgregarAsync(Tramo tramo) => await contexto.Tramos.AddAsync(tramo);
    public Task GuardarAsync() => contexto.SaveChangesAsync();
    public Task EliminarAsync(Tramo tramo) { contexto.Tramos.Remove(tramo); return Task.CompletedTask; }
}
