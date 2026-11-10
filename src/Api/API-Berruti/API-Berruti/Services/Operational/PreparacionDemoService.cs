using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class PreparacionDemoService(IPreparacionDemoRepository repository) : IPreparacionDemoService
{
    private const decimal MaximoImporte = 9999999999999999.99m;
    private sealed record TramoPlan(Tramo Entidad, bool Crear);
    private sealed record ServicioPlan(Servicio Entidad, bool Crear);
    private sealed record TarifaPlan(Tarifa Entidad, bool Crear);

    public async Task<ResultadoPreparacionDemoDto> PrepararAsync(SolicitudPreparacionDemoDto solicitud)
    {
        ValidarSolicitud(solicitud);
        var conflictos = new List<string>();
        var paradas = await repository.ListarParadasAsync();
        var tramos = await repository.ListarTramosAsync();
        var servicios = await repository.ListarServiciosAsync();
        var tarifas = await repository.ListarTarifasAsync();
        var paradasNuevas = new List<Parada>();

        var ombues = ResolverParada("Ombúes", paradas, paradasNuevas, conflictos);
        var radial = ResolverParada("Radial de Conchillas", paradas, paradasNuevas, conflictos);
        var colonia = ResolverParada("Colonia", paradas, paradasNuevas, conflictos);
        var tramoOmbuesRadial = ombues == null || radial == null ? null : ResolverTramo(ombues, radial, "Ombúes → Radial de Conchillas", tramos, conflictos);
        var tramoRadialColonia = radial == null || colonia == null ? null : ResolverTramo(radial, colonia, "Radial de Conchillas → Colonia", tramos, conflictos);

        var servicioOmbuesRadial = tramoOmbuesRadial == null ? null : ResolverServicio(tramoOmbuesRadial.Entidad.Id,
            solicitud.Fecha, solicitud.HoraSalidaOmbuesRadial, solicitud.HoraLlegadaOmbuesRadial,
            solicitud.CapacidadOmbuesRadial, PoliticaAsiento.SinAsiento, "Ombúes → Radial de Conchillas", servicios, conflictos);
        var servicioRadialColonia = tramoRadialColonia == null ? null : ResolverServicio(tramoRadialColonia.Entidad.Id,
            solicitud.Fecha, solicitud.HoraSalidaRadialColonia, solicitud.HoraLlegadaRadialColonia,
            solicitud.CapacidadRadialColonia, PoliticaAsiento.AsignacionAutomatica, "Radial de Conchillas → Colonia", servicios, conflictos);

        var tarifaOmbuesRadial = tramoOmbuesRadial == null ? null : ResolverTarifa(tramoOmbuesRadial.Entidad.Id,
            solicitud.VigenteDesdeOmbuesRadial, solicitud.VigenteHastaOmbuesRadial, solicitud.ImporteOmbuesRadial,
            "Ombúes → Radial de Conchillas", tarifas, conflictos);
        var tarifaRadialColonia = tramoRadialColonia == null ? null : ResolverTarifa(tramoRadialColonia.Entidad.Id,
            solicitud.VigenteDesdeRadialColonia, solicitud.VigenteHastaRadialColonia, solicitud.ImporteRadialColonia,
            "Radial de Conchillas → Colonia", tarifas, conflictos);

        if (conflictos.Count > 0)
            return new() { Conflicto = true, SinCambios = true,
                Mensaje = "La configuración existente no coincide. No se modificó ningún registro.", Conflictos = conflictos };

        var tramosNuevos = new[] { tramoOmbuesRadial, tramoRadialColonia }.Where(t => t is { Crear: true }).Select(t => t!.Entidad).ToArray();
        var serviciosNuevos = new[] { servicioOmbuesRadial, servicioRadialColonia }.Where(s => s is { Crear: true }).Select(s => s!.Entidad).ToArray();
        var tarifasNuevas = new[] { tarifaOmbuesRadial, tarifaRadialColonia }.Where(t => t is { Crear: true }).Select(t => t!.Entidad).ToArray();

        if (paradasNuevas.Count == 0 && tramosNuevos.Length == 0 && serviciosNuevos.Length == 0 && tarifasNuevas.Length == 0)
            return new() { Exito = true, SinCambios = true, Mensaje = "El escenario de demostración ya estaba preparado." };

        foreach (var parada in paradasNuevas) await repository.AgregarParadaAsync(parada);
        foreach (var tramo in tramosNuevos) await repository.AgregarTramoAsync(tramo);
        foreach (var servicio in serviciosNuevos) await repository.AgregarServicioAsync(servicio);
        foreach (var tarifa in tarifasNuevas) await repository.AgregarTarifaAsync(tarifa);
        await repository.GuardarAsync();
        return new() { Exito = true, Mensaje = "Se completaron únicamente los registros faltantes.",
            ParadasCreadas = paradasNuevas.Count, TramosCreados = tramosNuevos.Length,
            ServiciosCreados = serviciosNuevos.Length, TarifasCreadas = tarifasNuevas.Length };
    }

    private static Parada? ResolverParada(string nombre, List<Parada> existentes, List<Parada> nuevas, List<string> conflictos)
    {
        var coincidencias = existentes.Where(p => string.Equals(p.Nombre.Trim(), nombre, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (coincidencias.Length > 1) { conflictos.Add($"Hay más de una parada equivalente a '{nombre}'."); return null; }
        if (coincidencias.Length == 1) return coincidencias[0];
        var nueva = new Parada { Nombre = nombre };
        nuevas.Add(nueva);
        return nueva;
    }

    private static TramoPlan? ResolverTramo(Parada origen, Parada destino, string descripcion,
        List<Tramo> existentes, List<string> conflictos)
    {
        var coincidencias = existentes.Where(t => t.OrigenId == origen.Id && t.DestinoId == destino.Id).ToArray();
        if (coincidencias.Length > 1) { conflictos.Add($"Hay más de un tramo para {descripcion}."); return null; }
        return coincidencias.Length == 1 ? new(coincidencias[0], false) : new(new Tramo { OrigenId = origen.Id, DestinoId = destino.Id }, true);
    }

    private static ServicioPlan? ResolverServicio(string tramoId, DateTime fecha, TimeSpan salida, TimeSpan llegada,
        int capacidad, PoliticaAsiento politica, string descripcion, List<Servicio> existentes, List<string> conflictos)
    {
        var coincidencias = existentes.Where(s => s.TramoId == tramoId && s.Fecha.Date == fecha.Date).ToArray();
        if (coincidencias.Length > 1) { conflictos.Add($"Hay más de un servicio existente para {descripcion} en la fecha solicitada."); return null; }
        if (coincidencias.Length == 0)
            return new(new Servicio { TramoId = tramoId, Fecha = fecha.Date, HoraSalida = salida, HoraLlegada = llegada, Capacidad = capacidad, PoliticaAsiento = politica }, true);
        var existente = coincidencias[0];
        if (existente.HoraSalida != salida || existente.HoraLlegada != llegada || existente.Capacidad != capacidad || existente.PoliticaAsiento != politica)
            conflictos.Add($"El servicio existente para {descripcion} tiene horario, capacidad o política diferente.");
        return new(existente, false);
    }

    private static TarifaPlan? ResolverTarifa(string tramoId, DateTime desde, DateTime hasta, decimal importe,
        string descripcion, List<Tarifa> existentes, List<string> conflictos)
    {
        var coincidencias = existentes.Where(t => t.TramoId == tramoId && t.VigenteDesde.Date == desde.Date && t.VigenteHasta.Date == hasta.Date).ToArray();
        if (coincidencias.Length > 1) { conflictos.Add($"Hay más de una tarifa existente para {descripcion} con la misma vigencia."); return null; }
        if (coincidencias.Length == 0)
            return new(new Tarifa { TramoId = tramoId, Importe = importe, VigenteDesde = desde.Date, VigenteHasta = hasta.Date }, true);
        var existente = coincidencias[0];
        if (existente.Importe != importe) conflictos.Add($"La tarifa existente para {descripcion} tiene otro importe.");
        return new(existente, false);
    }

    private static void ValidarSolicitud(SolicitudPreparacionDemoDto solicitud)
    {
        if (solicitud.Fecha == default) throw new ArgumentException("La fecha del escenario es obligatoria.");
        ValidarHorario(solicitud.HoraSalidaOmbuesRadial, "salida Ombúes–Radial");
        ValidarHorario(solicitud.HoraLlegadaOmbuesRadial, "llegada Ombúes–Radial");
        ValidarHorario(solicitud.HoraSalidaRadialColonia, "salida Radial–Colonia");
        ValidarHorario(solicitud.HoraLlegadaRadialColonia, "llegada Radial–Colonia");
        if (solicitud.CapacidadOmbuesRadial < 1 || solicitud.CapacidadRadialColonia < 1)
            throw new ArgumentException("Las capacidades deben ser mayores a cero.");
        ValidarImporte(solicitud.ImporteOmbuesRadial);
        ValidarImporte(solicitud.ImporteRadialColonia);
        ValidarVigencia(solicitud.VigenteDesdeOmbuesRadial, solicitud.VigenteHastaOmbuesRadial, solicitud.Fecha, "Ombúes–Radial");
        ValidarVigencia(solicitud.VigenteDesdeRadialColonia, solicitud.VigenteHastaRadialColonia, solicitud.Fecha, "Radial–Colonia");
    }
    private static void ValidarHorario(TimeSpan hora, string nombre)
    {
        if (hora < TimeSpan.Zero || hora >= TimeSpan.FromDays(1)) throw new ArgumentException($"El horario de {nombre} debe estar dentro del día.");
    }
    private static void ValidarImporte(decimal importe)
    {
        if (importe < 0 || importe > MaximoImporte || decimal.Round(importe, 2) != importe)
            throw new ArgumentException("Los importes deben ser no negativos y tener como máximo dos decimales.");
    }
    private static void ValidarVigencia(DateTime desde, DateTime hasta, DateTime fecha, string nombre)
    {
        if (desde == default || hasta == default || hasta.Date < desde.Date || fecha.Date < desde.Date || fecha.Date > hasta.Date)
            throw new ArgumentException($"La vigencia de la tarifa {nombre} debe cubrir la fecha del escenario.");
    }
}
