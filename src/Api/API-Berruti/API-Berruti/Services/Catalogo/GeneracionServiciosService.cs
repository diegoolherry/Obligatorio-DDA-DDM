using API_Berruti.DTOs.Catalogo;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Catalogo;

namespace API_Berruti.Services.Catalogo;

// Genera Servicios fechados desde el catálogo (decisión de Diego para FIN-001): por cada día del rango,
// cada salida que corre según el calendario y cada par de paradas (i < j) con hora publicada en ambas,
// un Servicio de ese tramo. Las paradas sin hora no generan servicios. No crea tarifas: la búsqueda no
// ofrece un servicio hasta que el administrador cargue la tarifa de su tramo.
// Repetir la generación no duplica: se omiten los servicios ya generados para la misma salida, tramo y hora.
public class GeneracionServiciosService(ICatalogoRepository catalogo, IGeneracionServiciosRepository repository) : IGeneracionServiciosService
{
    public const int DiasMaximos = 31;

    public async Task<ResultadoGeneracionDto> GenerarAsync(GenerarServiciosDto dto)
    {
        if (dto.Desde is null || dto.Hasta is null) throw new ArgumentException("Las fechas desde y hasta son obligatorias.");
        if (dto.Hasta < dto.Desde) throw new ArgumentException("La fecha hasta no puede ser anterior a la fecha desde.");
        var desde = dto.Desde.Value;
        var hasta = dto.Hasta.Value;
        if (hasta.DayNumber - desde.DayNumber + 1 > DiasMaximos)
            throw new ArgumentException($"El rango no puede superar {DiasMaximos} días.");
        if (dto.Capacidad is null || dto.Capacidad < 1) throw new ArgumentException("La capacidad es obligatoria y debe ser al menos 1.");
        if (dto.PoliticaAsiento is null || !Enum.IsDefined(dto.PoliticaAsiento.Value))
            throw new ArgumentException("La política de asiento es obligatoria.");

        var datos = await catalogo.ObtenerAsync();
        var tramos = (await repository.ListarTramosAsync())
            .GroupBy(t => (t.OrigenId, t.DestinoId))
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Id, StringComparer.Ordinal).First());
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.ToDateTime(TimeOnly.MinValue).AddDays(1);
        var existentes = (await repository.ListarGeneradosAsync(inicio, fin))
            .Select(s => (s.SalidaCatalogoId!, s.TramoId, s.Salida)).ToHashSet();

        var tramosNuevos = new List<Tramo>();
        var serviciosNuevos = new List<Servicio>();
        var repetidos = 0;
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            var dia = fecha.ToDateTime(TimeOnly.MinValue);
            foreach (var bloque in datos.Bloques)
            {
                var resolucion = CalendarioCatalogo.Resolver(fecha, bloque, datos.Feriados, datos.Excepciones);
                if (resolucion.Estado != EstadoDia.Opera) continue;
                foreach (var ruta in datos.Rutas.Where(r => r.BloqueId == bloque.Id))
                {
                    var paradas = datos.ParadasRuta.Where(p => p.RutaId == ruta.Id).ToDictionary(p => p.Orden, p => p.ParadaId);
                    foreach (var salida in datos.Salidas.Where(s => s.RutaId == ruta.Id && resolucion.Dias.Contains(s.Dias)))
                    {
                        var horas = datos.Horas.Where(h => h.SalidaId == salida.Id).OrderBy(h => h.Orden).ToList();
                        for (var i = 0; i < horas.Count; i++)
                        for (var j = i + 1; j < horas.Count; j++)
                        {
                            var clave = (paradas[horas[i].Orden], paradas[horas[j].Orden]);
                            if (!tramos.TryGetValue(clave, out var tramo))
                            {
                                tramo = new Tramo { OrigenId = clave.Item1, DestinoId = clave.Item2 };
                                tramos[clave] = tramo;
                                tramosNuevos.Add(tramo);
                            }
                            var horaSalida = dia + horas[i].Hora;
                            if (!existentes.Add((salida.Id, tramo.Id, horaSalida))) { repetidos++; continue; }
                            serviciosNuevos.Add(new Servicio
                            {
                                TramoId = tramo.Id, Salida = horaSalida, Llegada = dia + horas[j].Hora,
                                Capacidad = dto.Capacidad.Value, PoliticaAsiento = dto.PoliticaAsiento.Value, SalidaCatalogoId = salida.Id
                            });
                        }
                    }
                }
            }
        }

        if (tramosNuevos.Count > 0 || serviciosNuevos.Count > 0) await repository.GuardarAsync(tramosNuevos, serviciosNuevos);
        return new ResultadoGeneracionDto
        {
            Desde = desde, Hasta = hasta, ServiciosCreados = serviciosNuevos.Count, ServiciosExistentes = repetidos, TramosCreados = tramosNuevos.Count
        };
    }
}
