using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;

// FIN-008 S4/S8: a deleted (inactive) bloque, ruta or salida disappears from the public catalog,
// the day query and the service generation; its children disappear with it.
internal static class InactiveCatalogChecks
{
    private static readonly DateOnly Lunes = new(2026, 11, 9);

    public static async Task RunAsync(string json, Action<bool, string> check)
    {
        var sembrado = Catalog.Build(json);
        var datos = Catalog.Data(sembrado);
        check(datos.Bloques.All(b => b.Activo) && datos.Rutas.All(r => r.Activo) && datos.Salidas.All(s => s.Activo),
            "seeded catalog items start active");

        var completo = await Counts(datos);

        // Inactive salida: only that departure disappears.
        var salida = datos.Salidas.First(s => s.Dias == API_Berruti.Models.Catalogo.DiasOperacion.LunesAViernes);
        salida.Activo = false;
        var sinSalida = await Counts(datos);
        check(sinSalida.Catalogo == completo.Catalogo - 1 && !sinSalida.Ids.Contains(salida.Id), "an inactive salida is hidden from GET /api/catalogo");
        check(sinSalida.Dia == completo.Dia - 1, "an inactive salida is hidden from the day query");
        check(sinSalida.Generados < completo.Generados && !sinSalida.GeneradosDe.Contains(salida.Id), "an inactive salida generates no services");
        salida.Activo = true;

        // Inactive ruta: all its salidas disappear and the route itself is not listed.
        var ruta = datos.Rutas.First(r => r.Id == salida.RutaId);
        var salidasRuta = datos.Salidas.Where(s => s.RutaId == ruta.Id).Select(s => s.Id).ToHashSet();
        ruta.Activo = false;
        var sinRuta = await Counts(datos);
        check(!sinRuta.Rutas.Contains(ruta.Clave) && sinRuta.Catalogo == completo.Catalogo - salidasRuta.Count, "an inactive ruta and its salidas are hidden from the catalog");
        check(!sinRuta.GeneradosDe.Overlaps(salidasRuta) && sinRuta.Generados < completo.Generados, "an inactive ruta generates no services");
        ruta.Activo = true;

        // Inactive bloque: its rutas, salidas and block exceptions disappear.
        var bloque = datos.Bloques.First(b => b.Id == ruta.BloqueId);
        var rutasBloque = datos.Rutas.Where(r => r.BloqueId == bloque.Id).Select(r => r.Clave).ToHashSet();
        bloque.Activo = false;
        var sinBloque = await Counts(datos);
        check(!sinBloque.Bloques.Contains(bloque.Clave) && !sinBloque.Rutas.Overlaps(rutasBloque), "an inactive bloque and its rutas are hidden from the catalog");
        check(!sinBloque.DiaBloques.Contains(bloque.Clave), "an inactive bloque is hidden from the day query");
        check(!sinBloque.Excepciones.Contains(bloque.Clave), "exceptions of an inactive bloque are hidden");
        check(sinBloque.Generados < completo.Generados, "an inactive bloque generates no services");
        bloque.Activo = true;

        // Already generated services are kept: generation never deletes, and reactivating nothing is needed.
        var repo = new MemoryGeneracionRepository();
        var generacion = new GeneracionServiciosService(new MemoryCatalogoRepository(datos), repo);
        await generacion.GenerarAsync(Dto());
        var antes = repo.Servicios.Select(s => s.Id).ToList();
        salida.Activo = false;
        var segunda = await generacion.GenerarAsync(Dto());
        check(segunda.ServiciosCreados == 0 && repo.Servicios.Select(s => s.Id).SequenceEqual(antes), "services already generated from an inactive salida are kept");
        salida.Activo = true;
    }

    private static GenerarServiciosDto Dto() => new() { Desde = Lunes, Hasta = Lunes, Capacidad = 40, PoliticaAsiento = PoliticaAsiento.SinAsiento };

    private sealed record Conteo(int Catalogo, HashSet<string> Ids, HashSet<string> Rutas, HashSet<string> Bloques, HashSet<string> Excepciones,
        int Dia, HashSet<string> DiaBloques, int Generados, HashSet<string> GeneradosDe);

    private static async Task<Conteo> Counts(DatosCatalogo datos)
    {
        var lectura = new CatalogoService(new MemoryCatalogoRepository(datos));
        var catalogo = await lectura.ObtenerAsync();
        var salidas = catalogo.Bloques.SelectMany(b => b.Rutas).SelectMany(r => r.Salidas).ToList();
        var dia = await lectura.SalidasDelDiaAsync(Lunes);
        var repo = new MemoryGeneracionRepository();
        await new GeneracionServiciosService(new MemoryCatalogoRepository(datos), repo).GenerarAsync(Dto());
        return new Conteo(salidas.Count, salidas.Select(s => s.Id).ToHashSet(),
            catalogo.Bloques.SelectMany(b => b.Rutas).Select(r => r.Clave).ToHashSet(),
            catalogo.Bloques.Select(b => b.Clave).ToHashSet(),
            catalogo.Excepciones.Where(e => e.Bloque != null).Select(e => e.Bloque!).ToHashSet(),
            dia.Bloques.Sum(b => b.Salidas.Count), dia.Bloques.Select(b => b.Clave).ToHashSet(),
            repo.Servicios.Count, repo.Servicios.Select(s => s.SalidaCatalogoId!).ToHashSet());
    }
}
