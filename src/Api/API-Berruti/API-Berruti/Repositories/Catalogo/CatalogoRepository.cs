using API_Berruti.Data;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Catalogo;

public class CatalogoRepository(BerrutiDbContext contexto) : ICatalogoRepository
{
    public async Task<DatosCatalogo> ObtenerAsync() => new(
        await contexto.BloquesHorario.AsNoTracking().ToListAsync(),
        await contexto.Rutas.AsNoTracking().ToListAsync(),
        await contexto.ParadasRuta.AsNoTracking().ToListAsync(),
        await contexto.SalidasCatalogo.AsNoTracking().ToListAsync(),
        await contexto.HorasSalida.AsNoTracking().ToListAsync(),
        await contexto.Paradas.AsNoTracking().ToListAsync(),
        await contexto.Feriados.AsNoTracking().ToListAsync(),
        await contexto.ExcepcionesCalendario.AsNoTracking().ToListAsync());
}
