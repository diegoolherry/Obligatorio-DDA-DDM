using API_Berruti.Models.Catalogo;

namespace API_Berruti.Services.Catalogo;

public enum EstadoDia
{
    Opera,
    FueraDeVigencia,
    SinServicio,
    ConsultarAgencia,
    CalendarioNoCargado
}

public record ResolucionDia(EstadoDia Estado, string? Motivo, IReadOnlySet<DiasOperacion> Dias);

// Reglas de calendario de FIN-001, en este orden:
// 1. Fuera de la vigencia publicada del bloque no hay salidas (un bloque sin fecha publicada no tiene ese límite).
// 2. Un año sin feriados cargados no ofrece salidas («calendario no cargado»).
// 3. Una excepción «sin servicio» gana sobre «consultar agencia»; ambas dejan el día sin salidas.
// 4. Domingo o feriado: «Domingos y feriados». Sábado: «Sábados» y «Lunes a sábado».
//    Lunes a viernes: «Lunes a viernes», «Lunes a sábado» y, lunes y viernes, «Lunes y viernes».
//    Las salidas de días hábiles no corren en feriados.
public static class CalendarioCatalogo
{
    private static readonly IReadOnlySet<DiasOperacion> Ninguno = new HashSet<DiasOperacion>();

    public static ResolucionDia Resolver(DateOnly fecha, BloqueHorario bloque,
        IEnumerable<Feriado> feriados, IEnumerable<ExcepcionCalendario> excepciones)
    {
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        if ((bloque.VigenteDesde is not null && dia < bloque.VigenteDesde.Value.Date) ||
            (bloque.VigenteHasta is not null && dia > bloque.VigenteHasta.Value.Date))
            return new ResolucionDia(EstadoDia.FueraDeVigencia, "Fuera de la vigencia publicada del bloque.", Ninguno);

        // Sin feriados cargados para el año no se puede saber qué horario corre: no se ofrecen salidas (decisión de Diego).
        var feriadosCargados = feriados.ToList();
        if (!feriadosCargados.Any(f => f.Fecha.Year == fecha.Year))
            return new ResolucionDia(EstadoDia.CalendarioNoCargado, $"No está cargado el calendario de feriados de {fecha.Year}.", Ninguno);

        var aplicables = excepciones
            .Where(e => (e.BloqueId is null || e.BloqueId == bloque.Id) && e.Desde.Date <= dia && e.Hasta.Date >= dia)
            .OrderBy(e => e.Tipo == TipoExcepcion.SinServicio ? 0 : 1)
            .ToList();
        if (aplicables.Count > 0)
        {
            var excepcion = aplicables[0];
            var estado = excepcion.Tipo == TipoExcepcion.SinServicio ? EstadoDia.SinServicio : EstadoDia.ConsultarAgencia;
            return new ResolucionDia(estado, excepcion.Motivo, Ninguno);
        }

        var esFeriado = feriadosCargados.Any(f => f.Fecha.Date == dia);
        return new ResolucionDia(EstadoDia.Opera, null, TiposDeDia(fecha, esFeriado));
    }

    public static IReadOnlySet<DiasOperacion> TiposDeDia(DateOnly fecha, bool esFeriado)
    {
        if (esFeriado || fecha.DayOfWeek == DayOfWeek.Sunday) return new HashSet<DiasOperacion> { DiasOperacion.DomingosYFeriados };
        if (fecha.DayOfWeek == DayOfWeek.Saturday) return new HashSet<DiasOperacion> { DiasOperacion.Sabados, DiasOperacion.LunesASabado };
        var habiles = new HashSet<DiasOperacion> { DiasOperacion.LunesAViernes, DiasOperacion.LunesASabado };
        if (fecha.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Friday) habiles.Add(DiasOperacion.LunesYViernes);
        return habiles;
    }
}
