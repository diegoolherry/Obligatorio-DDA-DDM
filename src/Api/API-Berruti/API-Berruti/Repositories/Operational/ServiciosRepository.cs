using API_Berruti.Data;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Operational;

public class ServiciosRepository(BerrutiDbContext contexto) : IServiciosRepository
{
    public Task<List<Servicio>> ListarAsync(DateOnly? dia = null)
    {
        var consulta = contexto.Servicios.AsNoTracking();
        if (dia is { } d)
        {
            // Salida es un DateTime local: el día local es [00:00, 00:00 del día siguiente).
            var desde = d.ToDateTime(TimeOnly.MinValue);
            var hasta = desde.AddDays(1);
            consulta = consulta.Where(s => s.Salida >= desde && s.Salida < hasta);
        }
        return consulta.OrderBy(s => s.Salida).ToListAsync();
    }
    public Task<Servicio?> BuscarAsync(string id) => contexto.Servicios.FirstOrDefaultAsync(s => s.Id == id);
    public Task<bool> ExisteTramoAsync(string tramoId) => contexto.Tramos.AnyAsync(t => t.Id == tramoId);
    public async Task AgregarAsync(Servicio servicio) => await contexto.Servicios.AddAsync(servicio);
    public Task GuardarAsync() => contexto.SaveChangesAsync();
    public Task EliminarAsync(Servicio servicio) { contexto.Servicios.Remove(servicio); return Task.CompletedTask; }
}
