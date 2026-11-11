using API_Berruti.Data;
using API_Berruti.DTOs.Location;
using API_Berruti.Models.Location;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Location;

public class UbicacionRepository(BerrutiDbContext contexto) : IUbicacionRepository
{
    public Task<string?> BuscarUsuarioIdPorPublicoAsync(string identificadorPublico) =>
        contexto.Usuarios.AsNoTracking().Where(u => u.IdentificadorPublico == identificadorPublico && u.Activo)
            .Select(u => u.Identificador).FirstOrDefaultAsync();

    public Task<ServicioUbicacionDto?> BuscarServicioAsync(string servicioId) =>
        Servicios().FirstOrDefaultAsync(s => s.Id == servicioId);

    public async Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsync() =>
        await Servicios().OrderBy(s => s.Salida).ToListAsync();

    public async Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsignadosAsync(string usuarioId)
    {
        var asignados = contexto.AsignacionesCobrador.Where(a => a.UsuarioId == usuarioId).Select(a => a.ServicioId);
        return await Servicios().Where(s => asignados.Contains(s.Id)).OrderBy(s => s.Salida).ToListAsync();
    }

    public Task<bool> EstaAsignadoAsync(string usuarioId, string servicioId) =>
        contexto.AsignacionesCobrador.AnyAsync(a => a.UsuarioId == usuarioId && a.ServicioId == servicioId);

    public Task<PosicionRecibida?> BuscarUltimaPosicionAsync(string servicioId) =>
        contexto.PosicionesRecibidas.AsNoTracking().Where(p => p.ServicioId == servicioId)
            .OrderByDescending(p => p.RecibidaUtc).FirstOrDefaultAsync();

    public async Task AgregarPosicionAsync(PosicionRecibida posicion) => await contexto.PosicionesRecibidas.AddAsync(posicion);

    public Task GuardarAsync() => contexto.SaveChangesAsync();

    private IQueryable<ServicioUbicacionDto> Servicios() =>
        from s in contexto.Servicios.AsNoTracking()
        join t in contexto.Tramos on s.TramoId equals t.Id
        join o in contexto.Paradas on t.OrigenId equals o.Id
        join d in contexto.Paradas on t.DestinoId equals d.Id
        select new ServicioUbicacionDto { Id = s.Id, Origen = o.Nombre, Destino = d.Nombre, Salida = s.Salida, Llegada = s.Llegada };
}
