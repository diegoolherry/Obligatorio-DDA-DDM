using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

// Consultas de solo lectura para la búsqueda pública; las reglas de combinación viven en ItinerariosService.
public class BusquedaItinerariosRepository(BerrutiDbContext contexto) : IBusquedaItinerariosRepository
{
    public Task<List<Parada>> ListarParadasAsync() => contexto.Paradas.AsNoTracking().ToListAsync();
    public Task<List<Tramo>> ListarTramosAsync() => contexto.Tramos.AsNoTracking().ToListAsync();
    public Task<List<Servicio>> ListarServiciosAsync(DateTime desde, DateTime hasta) =>
        contexto.Servicios.AsNoTracking().Where(s => s.Salida >= desde && s.Salida < hasta).ToListAsync();
    public Task<List<Tarifa>> ListarTarifasVigentesAsync(DateTime dia) =>
        contexto.Tarifas.AsNoTracking().Where(t => t.VigenteDesde <= dia && t.VigenteHasta >= dia).ToListAsync();
}
