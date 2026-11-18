using API_Berruti.Repositories.Catalogo;

namespace API_Berruti.Services.Catalogo;

// FIN-008: la baja de un bloque, una ruta o una salida es lógica. Las lecturas y la generación de Servicios
// trabajan solo con lo activo; dar de baja un bloque oculta sus rutas y excepciones, y una ruta sus salidas.
public static class CatalogoActivo
{
    public static DatosCatalogo Filtrar(DatosCatalogo datos)
    {
        var bloques = datos.Bloques.Where(b => b.Activo).ToList();
        var bloqueIds = bloques.Select(b => b.Id).ToHashSet();
        var rutas = datos.Rutas.Where(r => r.Activo && bloqueIds.Contains(r.BloqueId)).ToList();
        var rutaIds = rutas.Select(r => r.Id).ToHashSet();
        var salidas = datos.Salidas.Where(s => s.Activo && rutaIds.Contains(s.RutaId)).ToList();
        var salidaIds = salidas.Select(s => s.Id).ToHashSet();
        return new DatosCatalogo(
            bloques, rutas,
            datos.ParadasRuta.Where(p => rutaIds.Contains(p.RutaId)).ToList(),
            salidas,
            datos.Horas.Where(h => salidaIds.Contains(h.SalidaId)).ToList(),
            datos.Paradas, datos.Feriados,
            datos.Excepciones.Where(e => e.BloqueId is null || bloqueIds.Contains(e.BloqueId)).ToList());
    }
}
