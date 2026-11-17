using API_Berruti.Data;
using API_Berruti.Models.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Catalogo;

public class CatalogoAdministracionRepository(BerrutiDbContext contexto) : ICatalogoAdministracionRepository
{
    public Task<Dictionary<string, int>> ContarServiciosPorSalidaAsync() =>
        contexto.Servicios.AsNoTracking().Where(s => s.SalidaCatalogoId != null)
            .GroupBy(s => s.SalidaCatalogoId!).Select(g => new { g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Cantidad);

    public Task AgregarBloqueAsync(BloqueHorario bloque) => GuardarAsync(() => { contexto.BloquesHorario.Add(bloque); return Task.CompletedTask; });

    public Task ActualizarBloqueAsync(BloqueHorario bloque) => GuardarAsync(() => { contexto.BloquesHorario.Update(bloque); return Task.CompletedTask; });

    public Task AgregarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta> paradas) => GuardarAsync(() =>
    {
        contexto.Rutas.Add(ruta);
        contexto.ParadasRuta.AddRange(paradas);
        return Task.CompletedTask;
    });

    public Task ActualizarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta>? paradas) => GuardarAsync(async () =>
    {
        contexto.Rutas.Update(ruta);
        if (paradas is null) return;
        // La clave de parada_ruta es (ruta, orden): se reemplaza la lista completa dentro de la transacción.
        await contexto.ParadasRuta.Where(p => p.RutaId == ruta.Id).ExecuteDeleteAsync();
        contexto.ParadasRuta.AddRange(paradas);
    });

    public Task AgregarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas) => GuardarAsync(() =>
    {
        contexto.SalidasCatalogo.Add(salida);
        contexto.HorasSalida.AddRange(horas);
        return Task.CompletedTask;
    });

    public Task ActualizarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas) => GuardarAsync(async () =>
    {
        contexto.SalidasCatalogo.Update(salida);
        await contexto.HorasSalida.Where(h => h.SalidaId == salida.Id).ExecuteDeleteAsync();
        contexto.HorasSalida.AddRange(horas);
    });

    // Cada cambio va en una transacción: si falla, no quedan paradas u horas a medias.
    private async Task GuardarAsync(Func<Task> cambios)
    {
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            await cambios();
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException e)
        {
            contexto.ChangeTracker.Clear();
            throw new CatalogoConcurrenteException(e);
        }
    }
}
