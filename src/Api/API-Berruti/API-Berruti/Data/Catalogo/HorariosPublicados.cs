using System.Globalization;
using System.Text.Json;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Operational;

namespace API_Berruti.Data.Catalogo;

// Transcripción versionada del horario publicado (horarios-invierno-2026.json) y su conversión a entidades.
// Validar falla con todos los errores juntos: una transcripción inconsistente no se siembra a medias.
public static class HorariosPublicados
{
    public const string Recurso = "API_Berruti.Data.Catalogo.horarios-invierno-2026.json";

    private static readonly JsonSerializerOptions Opciones = new() { PropertyNameCaseInsensitive = true };

    public static ArchivoHorarios LeerRecurso()
    {
        using var flujo = typeof(HorariosPublicados).Assembly.GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso {Recurso}.");
        using var lector = new StreamReader(flujo);
        return Leer(lector.ReadToEnd());
    }

    public static ArchivoHorarios Leer(string json)
    {
        var archivo = JsonSerializer.Deserialize<ArchivoHorarios>(json, Opciones)
            ?? throw new InvalidOperationException("La transcripción de horarios está vacía.");
        var errores = Validar(archivo);
        if (errores.Count > 0)
            throw new InvalidOperationException("Transcripción de horarios inválida:" + Environment.NewLine + string.Join(Environment.NewLine, errores));
        return archivo;
    }

    public static List<string> Validar(ArchivoHorarios archivo)
    {
        var errores = new List<string>();
        if (string.IsNullOrWhiteSpace(archivo.Fuente?.Url)) errores.Add("Falta la URL de la fuente.");
        var leyenda = archivo.Leyenda ?? new Dictionary<string, string>();

        var feriados = archivo.Feriados ?? [];
        foreach (var f in feriados) if (!EsFecha(f.Fecha)) errores.Add($"Feriado con fecha inválida: {f.Fecha}.");
        foreach (var g in feriados.GroupBy(f => f.Fecha).Where(g => g.Count() > 1)) errores.Add($"Feriado repetido: {g.Key}.");

        foreach (var e in archivo.Excepciones ?? [])
        {
            if (!EsFecha(e.Desde) || !EsFecha(e.Hasta)) errores.Add($"Excepción con fecha inválida: {e.Desde}–{e.Hasta}.");
            else if (Fecha(e.Hasta) < Fecha(e.Desde)) errores.Add($"Excepción con rango invertido: {e.Desde}–{e.Hasta}.");
            if (!Enum.TryParse<TipoExcepcion>(e.Tipo, out _)) errores.Add($"Excepción con tipo desconocido: {e.Tipo}.");
            if (string.IsNullOrWhiteSpace(e.Motivo)) errores.Add($"Excepción sin motivo: {e.Desde}.");
            if (e.Bloque is not null && (archivo.Bloques ?? []).All(b => b.Clave != e.Bloque)) errores.Add($"Excepción de un bloque inexistente: {e.Bloque}.");
        }

        var bloques = archivo.Bloques ?? [];
        foreach (var g in bloques.GroupBy(b => b.Clave).Where(g => g.Count() > 1)) errores.Add($"Bloque repetido: {g.Key}.");
        var rutas = bloques.SelectMany(b => b.Rutas ?? []).ToList();
        foreach (var g in rutas.GroupBy(r => r.Clave).Where(g => g.Count() > 1)) errores.Add($"Ruta repetida: {g.Key}.");

        foreach (var bloque in bloques)
        {
            if (string.IsNullOrWhiteSpace(bloque.Clave) || string.IsNullOrWhiteSpace(bloque.Nombre)) errores.Add("Bloque sin clave o nombre.");
            if (bloque.VigenteDesde is not null && !EsFecha(bloque.VigenteDesde)) errores.Add($"{bloque.Clave}: vigencia desde inválida.");
            if (bloque.VigenteHasta is not null && !EsFecha(bloque.VigenteHasta)) errores.Add($"{bloque.Clave}: vigencia hasta inválida.");
            if (EsFecha(bloque.VigenteDesde) && EsFecha(bloque.VigenteHasta) && Fecha(bloque.VigenteHasta!) < Fecha(bloque.VigenteDesde!))
                errores.Add($"{bloque.Clave}: vigencia invertida.");
            if ((bloque.Rutas ?? []).Count == 0) errores.Add($"{bloque.Clave}: el bloque no tiene rutas.");

            foreach (var ruta in bloque.Rutas ?? [])
            {
                var paradas = ruta.Paradas ?? [];
                if (string.IsNullOrWhiteSpace(ruta.Clave) || string.IsNullOrWhiteSpace(ruta.Familia) || string.IsNullOrWhiteSpace(ruta.Nombre))
                    errores.Add($"{bloque.Clave}: ruta sin clave, familia o nombre.");
                if (paradas.Count < 2) errores.Add($"{ruta.Clave}: una ruta necesita al menos dos paradas.");
                if (paradas.Any(string.IsNullOrWhiteSpace)) errores.Add($"{ruta.Clave}: parada sin nombre.");
                for (var i = 1; i < paradas.Count; i++)
                    if (paradas[i] == paradas[i - 1]) errores.Add($"{ruta.Clave}: parada repetida de forma consecutiva ({paradas[i]}).");
                if ((ruta.Salidas ?? []).Count == 0) errores.Add($"{ruta.Clave}: la ruta no tiene salidas.");

                foreach (var (salida, indice) in (ruta.Salidas ?? []).Select((s, i) => (s, i + 1)))
                {
                    var donde = $"{ruta.Clave} salida {indice}";
                    if (!Enum.TryParse<DiasOperacion>(salida.Dias, out _)) errores.Add($"{donde}: tipo de día desconocido ({salida.Dias}).");
                    var horas = salida.Horas ?? [];
                    if (horas.Count != paradas.Count) { errores.Add($"{donde}: tiene {horas.Count} horas para {paradas.Count} paradas."); continue; }
                    if (horas[0] is null) errores.Add($"{donde}: la primera parada debe tener la hora de salida publicada.");
                    TimeSpan? anterior = null;
                    foreach (var texto in horas.Where(h => h is not null))
                    {
                        if (!TimeSpan.TryParseExact(texto, @"hh\:mm", CultureInfo.InvariantCulture, out var hora)) { errores.Add($"{donde}: hora inválida ({texto})."); continue; }
                        if (anterior is not null && hora <= anterior) errores.Add($"{donde}: las horas deben crecer a lo largo de la ruta ({texto}).");
                        anterior = hora;
                    }
                    foreach (var codigo in (salida.Codigos ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries))
                        if (!leyenda.ContainsKey(codigo)) errores.Add($"{donde}: código fuera de la leyenda ({codigo}).");
                }
            }
        }
        return errores;
    }

    // Convierte la transcripción en entidades. Reutiliza las paradas existentes por nombre y crea las faltantes.
    public static CatalogoSembrado Construir(ArchivoHorarios archivo, IEnumerable<Parada> paradasExistentes)
    {
        var paradas = paradasExistentes.ToDictionary(p => p.Nombre, StringComparer.Ordinal);
        var nuevas = new List<Parada>();
        Parada ObtenerParada(string nombre)
        {
            if (paradas.TryGetValue(nombre, out var existente)) return existente;
            var parada = new Parada { Nombre = nombre };
            paradas[nombre] = parada;
            nuevas.Add(parada);
            return parada;
        }

        var resultado = new CatalogoSembrado { ParadasNuevas = nuevas };
        var bloquesPorClave = new Dictionary<string, BloqueHorario>();
        foreach (var b in archivo.Bloques ?? [])
        {
            var bloque = new BloqueHorario
            {
                Clave = b.Clave, Nombre = b.Nombre, Empresa = b.Empresa ?? string.Empty,
                VigenteDesde = b.VigenteDesde is null ? null : Fecha(b.VigenteDesde),
                VigenteHasta = b.VigenteHasta is null ? null : Fecha(b.VigenteHasta),
                FuenteUrl = archivo.Fuente?.Url ?? string.Empty
            };
            bloquesPorClave[b.Clave] = bloque;
            resultado.Bloques.Add(bloque);
            foreach (var r in b.Rutas ?? [])
            {
                var ruta = new RutaCatalogo { BloqueId = bloque.Id, Clave = r.Clave, Familia = r.Familia, Nombre = r.Nombre };
                resultado.Rutas.Add(ruta);
                foreach (var (nombre, orden) in (r.Paradas ?? []).Select((n, i) => (n, i + 1)))
                    resultado.ParadasRuta.Add(new ParadaRuta { RutaId = ruta.Id, Orden = orden, ParadaId = ObtenerParada(nombre).Id });
                foreach (var s in r.Salidas ?? [])
                {
                    var salida = new SalidaCatalogo { RutaId = ruta.Id, Dias = Enum.Parse<DiasOperacion>(s.Dias), Codigos = s.Codigos, Nota = s.Nota };
                    resultado.Salidas.Add(salida);
                    foreach (var (texto, orden) in (s.Horas ?? []).Select((h, i) => (h, i + 1)).Where(x => x.h is not null))
                        resultado.Horas.Add(new HoraSalida
                        {
                            SalidaId = salida.Id, Orden = orden,
                            Hora = TimeSpan.ParseExact(texto!, @"hh\:mm", CultureInfo.InvariantCulture)
                        });
                }
            }
        }
        resultado.Feriados.AddRange((archivo.Feriados ?? []).Select(f => new Feriado { Fecha = Fecha(f.Fecha), Nombre = f.Nombre }));
        resultado.Excepciones.AddRange((archivo.Excepciones ?? []).Select(e => new ExcepcionCalendario
        {
            BloqueId = e.Bloque is null ? null : bloquesPorClave[e.Bloque].Id,
            Desde = Fecha(e.Desde), Hasta = Fecha(e.Hasta), Tipo = Enum.Parse<TipoExcepcion>(e.Tipo), Motivo = e.Motivo
        }));
        return resultado;
    }

    private static bool EsFecha(string? texto) =>
        DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static DateTime Fecha(string texto) =>
        DateTime.ParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
}

public class CatalogoSembrado
{
    public List<Parada> ParadasNuevas { get; init; } = [];
    public List<BloqueHorario> Bloques { get; } = [];
    public List<RutaCatalogo> Rutas { get; } = [];
    public List<ParadaRuta> ParadasRuta { get; } = [];
    public List<SalidaCatalogo> Salidas { get; } = [];
    public List<HoraSalida> Horas { get; } = [];
    public List<Feriado> Feriados { get; } = [];
    public List<ExcepcionCalendario> Excepciones { get; } = [];
}

public class ArchivoHorarios
{
    public FuenteHorarios? Fuente { get; set; }
    public Dictionary<string, string>? Leyenda { get; set; }
    public List<FeriadoArchivo>? Feriados { get; set; }
    public List<ExcepcionArchivo>? Excepciones { get; set; }
    public List<BloqueArchivo>? Bloques { get; set; }
}

public class FuenteHorarios
{
    public string? Url { get; set; }
    public string? Titulo { get; set; }
    public string? Consultada { get; set; }
    public string? Nota { get; set; }
}

public class FeriadoArchivo
{
    public string Fecha { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class ExcepcionArchivo
{
    public string? Bloque { get; set; }
    public string Desde { get; set; } = string.Empty;
    public string Hasta { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public class BloqueArchivo
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    public string? VigenteDesde { get; set; }
    public string? VigenteHasta { get; set; }
    public List<RutaArchivo>? Rutas { get; set; }
}

public class RutaArchivo
{
    public string Clave { get; set; } = string.Empty;
    public string Familia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<string>? Paradas { get; set; }
    public List<SalidaArchivo>? Salidas { get; set; }
}

public class SalidaArchivo
{
    public string Dias { get; set; } = string.Empty;
    public List<string?>? Horas { get; set; }
    public string? Codigos { get; set; }
    public string? Nota { get; set; }
}
