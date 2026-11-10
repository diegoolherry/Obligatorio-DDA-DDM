using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public class ParadasRepository(BerrutiDbContext contexto) : IParadasRepository
{
    public Task<List<Parada>> ListarAsync() => contexto.Paradas.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
    public Task<Parada?> BuscarAsync(string id) => contexto.Paradas.FirstOrDefaultAsync(p => p.Id == id);
    public async Task AgregarAsync(Parada parada) => await contexto.Paradas.AddAsync(parada);
    public Task GuardarAsync() => contexto.SaveChangesAsync();
    public Task EliminarAsync(Parada parada) { contexto.Paradas.Remove(parada); return Task.CompletedTask; }
}
