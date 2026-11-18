using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Catalogo;
using API_Berruti.Repositories.Catalogo;

namespace API_Berruti.Services.Catalogo;

// FIN-008: altas, modificaciones y bajas lógicas de bloques, rutas y salidas.
// Valida antes de guardar: una solicitud rechazada no cambia nada. Los cambios no tocan los Servicios ya
// generados; se aplican a las próximas generaciones.
public class CatalogoAdministracionService(ICatalogoRepository lectura, ICatalogoAdministracionRepository escritura) : ICatalogoAdministracionService
{
    public async Task<CatalogoAdministracionDto> ObtenerAsync()
    {
        var datos = CatalogoActivo.Filtrar(await lectura.ObtenerAsync());
        var servicios = await escritura.ContarServiciosPorSalidaAsync();
        return new CatalogoAdministracionDto
        {
            Bloques = datos.Bloques.OrderBy(b => b.Clave, StringComparer.Ordinal).Select(b =>
            {
                var dto = Bloque(b);
                dto.Rutas = datos.Rutas.Where(r => r.BloqueId == b.Id).OrderBy(r => r.Clave, StringComparer.Ordinal)
                    .Select(r => Ruta(datos, r, servicios)).ToList();
                return dto;
            }).ToList(),
            Feriados = datos.Feriados.OrderBy(f => f.Fecha).Select(Feriado).ToList(),
            Excepciones = datos.Excepciones.OrderBy(e => e.Desde).Select(Excepcion).ToList()
        };
    }

    public async Task<FeriadoDto> CrearFeriadoAsync(FeriadoEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        if (dto.Fecha is not { } fecha) throw new ArgumentException("La fecha es obligatoria.");
        var feriado = new Feriado { Fecha = fecha.ToDateTime(TimeOnly.MinValue), Nombre = Texto(dto.Nombre, 100, "El nombre es obligatorio y tiene hasta 100 caracteres.") };
        if (datos.Feriados.Any(f => f.Fecha == feriado.Fecha)) throw new CatalogoConflictoException($"Ya existe un feriado el {fecha:yyyy-MM-dd}.");
        await escritura.AgregarFeriadoAsync(feriado);
        return Feriado(feriado);
    }

    public async Task<FeriadoDto> ActualizarFeriadoAsync(DateOnly fecha, FeriadoEscrituraDto dto)
    {
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        if ((await lectura.ObtenerAsync()).Feriados.All(f => f.Fecha != dia)) throw new KeyNotFoundException();
        var feriado = new Feriado { Fecha = dia, Nombre = Texto(dto.Nombre, 100, "El nombre es obligatorio y tiene hasta 100 caracteres.") };
        await escritura.ActualizarFeriadoAsync(feriado);
        return Feriado(feriado);
    }

    public async Task EliminarFeriadoAsync(DateOnly fecha)
    {
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        if ((await lectura.ObtenerAsync()).Feriados.All(f => f.Fecha != dia)) throw new KeyNotFoundException();
        await escritura.EliminarFeriadoAsync(dia);
    }

    public async Task<ExcepcionAdministracionDto> CrearExcepcionAsync(ExcepcionEscrituraDto dto)
    {
        var excepcion = ValidarExcepcion(await lectura.ObtenerAsync(), new ExcepcionCalendario(), dto);
        await escritura.AgregarExcepcionAsync(excepcion);
        return Excepcion(excepcion);
    }

    public async Task<ExcepcionAdministracionDto> ActualizarExcepcionAsync(string id, ExcepcionEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        ExcepcionVisible(datos, id);
        var excepcion = ValidarExcepcion(datos, new ExcepcionCalendario { Id = id }, dto);
        await escritura.ActualizarExcepcionAsync(excepcion);
        return Excepcion(excepcion);
    }

    public async Task EliminarExcepcionAsync(string id)
    {
        ExcepcionVisible(await lectura.ObtenerAsync(), id);
        await escritura.EliminarExcepcionAsync(id);
    }

    private static ExcepcionCalendario ValidarExcepcion(DatosCatalogo datos, ExcepcionCalendario excepcion, ExcepcionEscrituraDto dto)
    {
        if (dto.Desde is not { } desde || dto.Hasta is not { } hasta) throw new ArgumentException("Las fechas desde y hasta son obligatorias.");
        if (hasta < desde) throw new ArgumentException("La fecha hasta no puede ser anterior a la fecha desde.");
        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo)) throw new ArgumentException("El tipo de excepción es obligatorio.");
        var bloqueId = string.IsNullOrWhiteSpace(dto.BloqueId) ? null : dto.BloqueId;
        if (bloqueId is not null && !datos.Bloques.Any(b => b.Id == bloqueId && b.Activo)) throw new ArgumentException("El bloque no existe o fue dado de baja.");
        excepcion.BloqueId = bloqueId;
        excepcion.Desde = desde.ToDateTime(TimeOnly.MinValue);
        excepcion.Hasta = hasta.ToDateTime(TimeOnly.MinValue);
        excepcion.Tipo = tipo;
        excepcion.Motivo = Texto(dto.Motivo, 200, "El motivo es obligatorio y tiene hasta 200 caracteres.");
        return excepcion;
    }

    // Una excepción de un bloque dado de baja queda oculta, igual que en la lectura pública.
    private static ExcepcionCalendario ExcepcionVisible(DatosCatalogo datos, string id) =>
        CatalogoActivo.Filtrar(datos).Excepciones.FirstOrDefault(e => e.Id == id) ?? throw new KeyNotFoundException();

    private static FeriadoDto Feriado(Feriado f) => new() { Fecha = DateOnly.FromDateTime(f.Fecha), Nombre = f.Nombre };

    private static ExcepcionAdministracionDto Excepcion(ExcepcionCalendario e) => new()
    {
        Id = e.Id, BloqueId = e.BloqueId, Desde = DateOnly.FromDateTime(e.Desde), Hasta = DateOnly.FromDateTime(e.Hasta), Tipo = e.Tipo, Motivo = e.Motivo
    };

    public async Task<BloqueAdministracionDto> CrearBloqueAsync(BloqueEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var bloque = ValidarBloque(datos, new BloqueHorario(), dto);
        await escritura.AgregarBloqueAsync(bloque);
        return Bloque(bloque);
    }

    public async Task<BloqueAdministracionDto> ActualizarBloqueAsync(string id, BloqueEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var actual = BloqueActivo(datos, id);
        var bloque = ValidarBloque(datos, new BloqueHorario { Id = actual.Id }, dto);
        await escritura.ActualizarBloqueAsync(bloque);
        return Bloque(bloque);
    }

    public async Task EliminarBloqueAsync(string id)
    {
        var actual = BloqueActivo(await lectura.ObtenerAsync(), id);
        await escritura.ActualizarBloqueAsync(Copia(actual, activo: false));
    }

    public async Task<RutaAdministracionDto> CrearRutaAsync(RutaEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var (ruta, paradas) = ValidarRuta(datos, new RutaCatalogo(), dto);
        await escritura.AgregarRutaAsync(ruta, paradas);
        return await RutaGuardadaAsync(ruta.Id);
    }

    public async Task<RutaAdministracionDto> ActualizarRutaAsync(string id, RutaEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var actual = RutaActiva(datos, id);
        var (ruta, paradas) = ValidarRuta(datos, new RutaCatalogo { Id = actual.Id }, dto);
        var actuales = datos.ParadasRuta.Where(p => p.RutaId == id).OrderBy(p => p.Orden).Select(p => p.ParadaId);
        var cambianParadas = !actuales.SequenceEqual(paradas.Select(p => p.ParadaId));
        // Las horas de una salida se guardan por orden de parada: cambiar las paradas las dejaría apuntando a otras.
        if (cambianParadas && datos.Salidas.Any(s => s.RutaId == id && s.Activo))
            throw new CatalogoConflictoException("La ruta tiene salidas activas: dalas de baja antes de cambiar sus paradas.");
        await escritura.ActualizarRutaAsync(ruta, cambianParadas ? paradas : null);
        return await RutaGuardadaAsync(ruta.Id);
    }

    public async Task EliminarRutaAsync(string id)
    {
        var actual = RutaActiva(await lectura.ObtenerAsync(), id);
        await escritura.ActualizarRutaAsync(Copia(actual, activo: false), null);
    }

    public async Task<SalidaAdministracionDto> CrearSalidaAsync(SalidaEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var (salida, horas) = ValidarSalida(datos, new SalidaCatalogo(), dto);
        await escritura.AgregarSalidaAsync(salida, horas);
        return Salida(salida, horas, 0);
    }

    public async Task<SalidaAdministracionDto> ActualizarSalidaAsync(string id, SalidaEscrituraDto dto)
    {
        var datos = await lectura.ObtenerAsync();
        var actual = SalidaActiva(datos, id);
        if (dto.RutaId != actual.RutaId) throw new ArgumentException("Una salida no puede cambiar de ruta.");
        var (salida, horas) = ValidarSalida(datos, new SalidaCatalogo { Id = actual.Id }, dto);
        await escritura.ActualizarSalidaAsync(salida, horas);
        var servicios = await escritura.ContarServiciosPorSalidaAsync();
        return Salida(salida, horas, servicios.GetValueOrDefault(salida.Id));
    }

    public async Task EliminarSalidaAsync(string id)
    {
        var datos = await lectura.ObtenerAsync();
        var actual = SalidaActiva(datos, id);
        var baja = new SalidaCatalogo { Id = actual.Id, RutaId = actual.RutaId, Dias = actual.Dias, Codigos = actual.Codigos, Nota = actual.Nota, Activo = false };
        await escritura.ActualizarSalidaAsync(baja, datos.Horas.Where(h => h.SalidaId == id).Select(h => new HoraSalida { SalidaId = id, Orden = h.Orden, Hora = h.Hora }).ToList());
    }

    private static BloqueHorario ValidarBloque(DatosCatalogo datos, BloqueHorario bloque, BloqueEscrituraDto dto)
    {
        bloque.Clave = Texto(dto.Clave, 16, "La clave es obligatoria y tiene hasta 16 caracteres.");
        bloque.Nombre = Texto(dto.Nombre, 100, "El nombre es obligatorio y tiene hasta 100 caracteres.");
        bloque.Empresa = Texto(dto.Empresa, 100, "La empresa es obligatoria y tiene hasta 100 caracteres.");
        bloque.FuenteUrl = dto.FuenteUrl?.Trim() ?? string.Empty;
        if (bloque.FuenteUrl.Length > 200) throw new ArgumentException("La URL de la fuente tiene hasta 200 caracteres.");
        if (dto.VigenteDesde is { } desde && dto.VigenteHasta is { } hasta && hasta < desde)
            throw new ArgumentException("La vigencia hasta no puede ser anterior a la vigencia desde.");
        bloque.VigenteDesde = dto.VigenteDesde?.ToDateTime(TimeOnly.MinValue);
        bloque.VigenteHasta = dto.VigenteHasta?.ToDateTime(TimeOnly.MinValue);
        // La clave es única también entre bloques dados de baja (índice ux_bloque_clave).
        if (datos.Bloques.Any(b => b.Id != bloque.Id && b.Clave == bloque.Clave))
            throw new CatalogoConflictoException($"Ya existe un bloque con la clave {bloque.Clave}.");
        return bloque;
    }

    private static (RutaCatalogo, List<ParadaRuta>) ValidarRuta(DatosCatalogo datos, RutaCatalogo ruta, RutaEscrituraDto dto)
    {
        if (!datos.Bloques.Any(b => b.Id == dto.BloqueId && b.Activo)) throw new ArgumentException("El bloque no existe o fue dado de baja.");
        ruta.BloqueId = dto.BloqueId!;
        ruta.Clave = Texto(dto.Clave, 16, "La clave es obligatoria y tiene hasta 16 caracteres.");
        ruta.Familia = Texto(dto.Familia, 100, "La familia es obligatoria y tiene hasta 100 caracteres.");
        ruta.Nombre = Texto(dto.Nombre, 100, "El nombre es obligatorio y tiene hasta 100 caracteres.");
        var ids = dto.ParadaIds ?? [];
        if (ids.Count < 2) throw new ArgumentException("Una ruta necesita al menos dos paradas.");
        var existentes = datos.Paradas.Select(p => p.Id).ToHashSet();
        foreach (var id in ids)
            if (!existentes.Contains(id)) throw new ArgumentException($"La parada {id} no existe.");
        for (var i = 1; i < ids.Count; i++)
            if (ids[i] == ids[i - 1]) throw new ArgumentException("Una ruta no puede repetir la misma parada de forma consecutiva.");
        if (datos.Rutas.Any(r => r.Id != ruta.Id && r.Clave == ruta.Clave))
            throw new CatalogoConflictoException($"Ya existe una ruta con la clave {ruta.Clave}.");
        return (ruta, ids.Select((id, i) => new ParadaRuta { RutaId = ruta.Id, Orden = i + 1, ParadaId = id }).ToList());
    }

    private static (SalidaCatalogo, List<HoraSalida>) ValidarSalida(DatosCatalogo datos, SalidaCatalogo salida, SalidaEscrituraDto dto)
    {
        var activas = CatalogoActivo.Filtrar(datos);
        if (!activas.Rutas.Any(r => r.Id == dto.RutaId)) throw new ArgumentException("La ruta no existe o fue dada de baja.");
        if (dto.Dias is not { } dias || !Enum.IsDefined(dias)) throw new ArgumentException("Los días de operación son obligatorios.");
        salida.RutaId = dto.RutaId!;
        salida.Dias = dias;
        salida.Codigos = Opcional(dto.Codigos, 16, "Los códigos tienen hasta 16 caracteres.");
        salida.Nota = Opcional(dto.Nota, 200, "La nota tiene hasta 200 caracteres.");

        var horas = dto.Horas ?? [];
        var paradas = datos.ParadasRuta.Count(p => p.RutaId == salida.RutaId);
        if (horas.Count < 2) throw new ArgumentException("Una salida necesita al menos dos horas.");
        if (horas.Any(h => h.Hora is null)) throw new ArgumentException("Cada hora es obligatoria.");
        foreach (var h in horas)
            if (h.Orden < 1 || h.Orden > paradas) throw new ArgumentException($"La hora de orden {h.Orden} no corresponde a una parada de la ruta.");
        foreach (var g in horas.GroupBy(h => h.Orden))
            if (g.Count() > 1) throw new ArgumentException($"Hay más de una hora para la parada de orden {g.Key}.");
        var ordenadas = horas.OrderBy(h => h.Orden).ToList();
        if (ordenadas[0].Orden != 1) throw new ArgumentException("La primera parada de la ruta debe tener hora de salida.");
        for (var i = 1; i < ordenadas.Count; i++)
            if (ordenadas[i].Hora <= ordenadas[i - 1].Hora) throw new ArgumentException("Las horas deben ser crecientes a lo largo de la ruta.");
        return (salida, ordenadas.Select(h => new HoraSalida { SalidaId = salida.Id, Orden = h.Orden, Hora = h.Hora!.Value.ToTimeSpan() }).ToList());
    }

    private async Task<RutaAdministracionDto> RutaGuardadaAsync(string id)
    {
        var datos = await lectura.ObtenerAsync();
        return Ruta(datos, datos.Rutas.Single(r => r.Id == id), await escritura.ContarServiciosPorSalidaAsync());
    }

    private static BloqueHorario BloqueActivo(DatosCatalogo datos, string id) =>
        datos.Bloques.FirstOrDefault(b => b.Id == id && b.Activo) ?? throw new KeyNotFoundException();

    private static RutaCatalogo RutaActiva(DatosCatalogo datos, string id) =>
        CatalogoActivo.Filtrar(datos).Rutas.FirstOrDefault(r => r.Id == id) ?? throw new KeyNotFoundException();

    private static SalidaCatalogo SalidaActiva(DatosCatalogo datos, string id) =>
        CatalogoActivo.Filtrar(datos).Salidas.FirstOrDefault(s => s.Id == id) ?? throw new KeyNotFoundException();

    private static BloqueHorario Copia(BloqueHorario b, bool activo) => new()
    {
        Id = b.Id, Clave = b.Clave, Nombre = b.Nombre, Empresa = b.Empresa, VigenteDesde = b.VigenteDesde, VigenteHasta = b.VigenteHasta, FuenteUrl = b.FuenteUrl, Activo = activo
    };

    private static RutaCatalogo Copia(RutaCatalogo r, bool activo) => new()
    {
        Id = r.Id, BloqueId = r.BloqueId, Clave = r.Clave, Familia = r.Familia, Nombre = r.Nombre, Activo = activo
    };

    private static string Texto(string? valor, int maximo, string mensaje)
    {
        var texto = valor?.Trim() ?? string.Empty;
        if (texto.Length == 0 || texto.Length > maximo) throw new ArgumentException(mensaje);
        return texto;
    }

    private static string? Opcional(string? valor, int maximo, string mensaje)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        if (texto.Length > maximo) throw new ArgumentException(mensaje);
        return texto;
    }

    private static BloqueAdministracionDto Bloque(BloqueHorario b) => new()
    {
        Id = b.Id, Clave = b.Clave, Nombre = b.Nombre, Empresa = b.Empresa, FuenteUrl = b.FuenteUrl,
        VigenteDesde = b.VigenteDesde is null ? null : DateOnly.FromDateTime(b.VigenteDesde.Value),
        VigenteHasta = b.VigenteHasta is null ? null : DateOnly.FromDateTime(b.VigenteHasta.Value)
    };

    private static RutaAdministracionDto Ruta(DatosCatalogo datos, RutaCatalogo ruta, Dictionary<string, int> servicios)
    {
        var nombres = datos.Paradas.ToDictionary(p => p.Id, p => p.Nombre);
        return new RutaAdministracionDto
        {
            Id = ruta.Id, BloqueId = ruta.BloqueId, Clave = ruta.Clave, Familia = ruta.Familia, Nombre = ruta.Nombre,
            Paradas = datos.ParadasRuta.Where(p => p.RutaId == ruta.Id).OrderBy(p => p.Orden)
                .Select(p => new ParadaRutaDto { Orden = p.Orden, ParadaId = p.ParadaId, Nombre = nombres.GetValueOrDefault(p.ParadaId, string.Empty) }).ToList(),
            Salidas = datos.Salidas.Where(s => s.RutaId == ruta.Id && s.Activo)
                .Select(s => Salida(s, datos.Horas.Where(h => h.SalidaId == s.Id).ToList(), servicios.GetValueOrDefault(s.Id)))
                .OrderBy(s => s.Dias).ThenBy(s => s.Horas.FirstOrDefault()?.Hora).ToList()
        };
    }

    private static SalidaAdministracionDto Salida(SalidaCatalogo s, IEnumerable<HoraSalida> horas, int servicios) => new()
    {
        Id = s.Id, RutaId = s.RutaId, Dias = s.Dias, Codigos = s.Codigos, Nota = s.Nota, ServiciosGenerados = servicios,
        Horas = horas.OrderBy(h => h.Orden).Select(h => new HoraEscrituraDto { Orden = h.Orden, Hora = TimeOnly.FromTimeSpan(h.Hora) }).ToList()
    };
}
