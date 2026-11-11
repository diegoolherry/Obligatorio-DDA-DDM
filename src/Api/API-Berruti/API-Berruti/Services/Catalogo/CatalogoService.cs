using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Catalogo;
using API_Berruti.Repositories.Catalogo;

namespace API_Berruti.Services.Catalogo;

// Consulta de solo lectura del catálogo (FIN-001). Las reglas de calendario viven en CalendarioCatalogo.
public class CatalogoService(ICatalogoRepository repository) : ICatalogoService
{
    public async Task<CatalogoDto> ObtenerAsync()
    {
        var datos = await repository.ObtenerAsync();
        var bloques = datos.Bloques.ToDictionary(b => b.Id);
        return new CatalogoDto
        {
            Bloques = datos.Bloques.OrderBy(b => b.Clave, StringComparer.Ordinal).Select(b => new BloqueCatalogoDto
            {
                Clave = b.Clave, Nombre = b.Nombre, Empresa = b.Empresa, FuenteUrl = b.FuenteUrl,
                VigenteDesde = b.VigenteDesde is null ? null : DateOnly.FromDateTime(b.VigenteDesde.Value),
                VigenteHasta = b.VigenteHasta is null ? null : DateOnly.FromDateTime(b.VigenteHasta.Value),
                Rutas = datos.Rutas.Where(r => r.BloqueId == b.Id).OrderBy(r => r.Clave, StringComparer.Ordinal)
                    .Select(r => Ruta(datos, r)).ToList()
            }).ToList(),
            Feriados = datos.Feriados.OrderBy(f => f.Fecha)
                .Select(f => new FeriadoDto { Fecha = DateOnly.FromDateTime(f.Fecha), Nombre = f.Nombre }).ToList(),
            Excepciones = datos.Excepciones.OrderBy(e => e.Desde).Select(e => new ExcepcionDto
            {
                Bloque = e.BloqueId is not null && bloques.TryGetValue(e.BloqueId, out var bloque) ? bloque.Clave : null,
                Desde = DateOnly.FromDateTime(e.Desde), Hasta = DateOnly.FromDateTime(e.Hasta), Tipo = e.Tipo, Motivo = e.Motivo
            }).ToList()
        };
    }

    public async Task<SalidasDelDiaDto> SalidasDelDiaAsync(DateOnly fecha)
    {
        var datos = await repository.ObtenerAsync();
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        var resultado = new SalidasDelDiaDto { Fecha = fecha, Feriado = datos.Feriados.FirstOrDefault(f => f.Fecha.Date == dia)?.Nombre };
        foreach (var bloque in datos.Bloques.OrderBy(b => b.Clave, StringComparer.Ordinal))
        {
            var resolucion = CalendarioCatalogo.Resolver(fecha, bloque, datos.Feriados, datos.Excepciones);
            var salidas = datos.Rutas.Where(r => r.BloqueId == bloque.Id)
                .SelectMany(r => datos.Salidas.Where(s => s.RutaId == r.Id && resolucion.Dias.Contains(s.Dias))
                    .Select(s => new SalidaDelDiaDto { RutaClave = r.Clave, Ruta = r.Nombre, Familia = r.Familia, Salida = Salida(datos, r, s) }))
                .OrderBy(s => s.Salida.Horas.First().Hora).ThenBy(s => s.RutaClave, StringComparer.Ordinal)
                .ToList();
            resultado.Bloques.Add(new BloqueDelDiaDto
            {
                Clave = bloque.Clave, Nombre = bloque.Nombre, Estado = resolucion.Estado, Motivo = resolucion.Motivo, Salidas = salidas
            });
        }
        return resultado;
    }

    private static RutaCatalogoDto Ruta(DatosCatalogo datos, RutaCatalogo ruta) => new()
    {
        Clave = ruta.Clave, Familia = ruta.Familia, Nombre = ruta.Nombre,
        Paradas = ParadasDe(datos, ruta).Select(p => new ParadaRutaDto { Orden = p.Orden, ParadaId = p.ParadaId, Nombre = p.Nombre }).ToList(),
        Salidas = datos.Salidas.Where(s => s.RutaId == ruta.Id).Select(s => Salida(datos, ruta, s))
            .OrderBy(s => s.Dias).ThenBy(s => s.Horas.First().Hora).ToList()
    };

    private static SalidaCatalogoDto Salida(DatosCatalogo datos, RutaCatalogo ruta, SalidaCatalogo salida)
    {
        var nombres = ParadasDe(datos, ruta).ToDictionary(p => p.Orden, p => p.Nombre);
        var horas = datos.Horas.Where(h => h.SalidaId == salida.Id).OrderBy(h => h.Orden).ToList();
        return new SalidaCatalogoDto
        {
            Id = salida.Id, Dias = salida.Dias, Codigos = salida.Codigos, Nota = salida.Nota,
            Horas = horas.Select((h, i) => new HoraParadaDto
            {
                Orden = h.Orden, Parada = nombres.GetValueOrDefault(h.Orden, string.Empty),
                Hora = TimeOnly.FromTimeSpan(h.Hora), Aproximada = i > 0
            }).ToList()
        };
    }

    private static IEnumerable<(int Orden, string ParadaId, string Nombre)> ParadasDe(DatosCatalogo datos, RutaCatalogo ruta)
    {
        var paradas = datos.Paradas.ToDictionary(p => p.Id);
        return datos.ParadasRuta.Where(p => p.RutaId == ruta.Id).OrderBy(p => p.Orden)
            .Select(p => (p.Orden, p.ParadaId, paradas.TryGetValue(p.ParadaId, out var parada) ? parada.Nombre : string.Empty));
    }
}
