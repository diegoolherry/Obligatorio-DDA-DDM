using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public class TarifasRepository(BerrutiDbContext contexto) : ITarifasRepository
{
    public Task<List<Tarifa>> ListarAsync() => contexto.Tarifas.AsNoTracking().OrderBy(t => t.TramoId).ThenBy(t => t.VigenteDesde).ToListAsync();
    public Task<Tarifa?> BuscarAsync(string id) => contexto.Tarifas.FirstOrDefaultAsync(t => t.Id == id);
    public Task<bool> ExisteTramoAsync(string tramoId) => contexto.Tramos.AnyAsync(t => t.Id == tramoId);
    public async Task AgregarAsync(Tarifa tarifa) => await contexto.Tarifas.AddAsync(tarifa);
    public Task GuardarAsync() => contexto.SaveChangesAsync();
    public Task EliminarAsync(Tarifa tarifa) { contexto.Tarifas.Remove(tarifa); return Task.CompletedTask; }
}
