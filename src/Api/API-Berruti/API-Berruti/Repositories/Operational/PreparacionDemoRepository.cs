using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public class PreparacionDemoRepository(BerrutiDbContext contexto) : IPreparacionDemoRepository
{
    public Task<List<Parada>> ListarParadasAsync() => contexto.Paradas.AsNoTracking().ToListAsync();
    public Task<List<Tramo>> ListarTramosAsync() => contexto.Tramos.AsNoTracking().ToListAsync();
    public Task<List<Servicio>> ListarServiciosAsync() => contexto.Servicios.AsNoTracking().ToListAsync();
    public Task<List<Tarifa>> ListarTarifasAsync() => contexto.Tarifas.AsNoTracking().ToListAsync();
    public async Task AgregarParadaAsync(Parada parada) => await contexto.Paradas.AddAsync(parada);
    public async Task AgregarTramoAsync(Tramo tramo) => await contexto.Tramos.AddAsync(tramo);
    public async Task AgregarServicioAsync(Servicio servicio) => await contexto.Servicios.AddAsync(servicio);
    public async Task AgregarTarifaAsync(Tarifa tarifa) => await contexto.Tarifas.AddAsync(tarifa);
    public Task GuardarAsync() => contexto.SaveChangesAsync();
}
