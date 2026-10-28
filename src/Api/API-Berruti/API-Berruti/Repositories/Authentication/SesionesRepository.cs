using API_Berruti.Data;
using API_Berruti.Models.Authentication;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Authentication;

public class SesionesRepository : ISesionesRepository
{
    private readonly BerrutiDbContext _contexto;
    public SesionesRepository(BerrutiDbContext contexto) { _contexto = contexto; }

    public async Task CrearAsync(Sesion sesion)
    {
        _contexto.Sesiones.Add(sesion);
        // The primary key is the digest. A collision fails; never reuse an existing session.
        await _contexto.SaveChangesAsync();
    }

    public Task<Sesion?> BuscarAsync(string huella) =>
        _contexto.Sesiones.AsNoTracking().SingleOrDefaultAsync(s => s.Huella == huella);

    public async Task RevocarAsync(string huella, DateTime ahoraUtc) =>
        await _contexto.Sesiones.Where(s => s.Huella == huella && s.RevocadaUtc == null)
            .ExecuteUpdateAsync(cambios => cambios.SetProperty(s => s.RevocadaUtc, ahoraUtc));
}
