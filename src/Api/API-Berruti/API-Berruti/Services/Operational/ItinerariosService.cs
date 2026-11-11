using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

// Arma itinerarios directos y de dos tramos para una fecha local. Reglas acordadas para MVP-003:
// ambos servicios salen en la fecha buscada, el transbordo es de 5 a 30 minutos en la misma parada
// y cada tramo necesita una tarifa vigente (la de VigenteDesde más reciente); si falta, no se ofrece.
public class ItinerariosService(IBusquedaItinerariosRepository repository) : IItinerariosService
{
    public const int TransbordoMinimoMinutos = 5;
    public const int TransbordoMaximoMinutos = 30;

    public async Task<IReadOnlyList<ItinerarioDto>> BuscarAsync(string? origenId, string? destinoId, DateOnly fecha)
    {
        if (string.IsNullOrWhiteSpace(origenId) || string.IsNullOrWhiteSpace(destinoId))
            throw new ArgumentException("El origen y el destino son obligatorios.");
        if (origenId == destinoId) throw new ArgumentException("El origen y el destino deben ser paradas distintas.");

        var paradas = (await repository.ListarParadasAsync()).ToDictionary(p => p.Id);
        if (!paradas.ContainsKey(origenId) || !paradas.ContainsKey(destinoId))
            throw new KeyNotFoundException("La parada de origen o de destino no existe.");

        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = dia.AddDays(1);
        var tramos = (await repository.ListarTramosAsync()).ToDictionary(t => t.Id);
        var tarifas = (await repository.ListarTarifasVigentesAsync(dia))
            .Where(t => t.VigenteDesde.Date <= dia && t.VigenteHasta.Date >= dia)
            .GroupBy(t => t.TramoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.VigenteDesde).ThenBy(t => t.Id, StringComparer.Ordinal).First());

        // El repositorio ya filtra por fecha; se repite aquí para que la regla no dependa de la consulta.
        var tramosOfrecibles = (await repository.ListarServiciosAsync(dia, hasta))
            .Where(s => s.Salida >= dia && s.Salida < hasta && tramos.ContainsKey(s.TramoId) && tarifas.ContainsKey(s.TramoId))
            .Select(s => (Servicio: s, Tramo: tramos[s.TramoId], Tarifa: tarifas[s.TramoId]))
            .ToList();

        var itinerarios = new List<ItinerarioDto>();
        foreach (var primero in tramosOfrecibles.Where(x => x.Tramo.OrigenId == origenId))
        {
            if (primero.Tramo.DestinoId == destinoId)
            {
                itinerarios.Add(Armar(paradas, [primero], null));
                continue;
            }
            foreach (var segundo in tramosOfrecibles.Where(x => x.Tramo.OrigenId == primero.Tramo.DestinoId && x.Tramo.DestinoId == destinoId))
            {
                var espera = segundo.Servicio.Salida - primero.Servicio.Llegada;
                if (espera.TotalMinutes < TransbordoMinimoMinutos || espera.TotalMinutes > TransbordoMaximoMinutos) continue;
                var transbordo = new TransbordoDto { Parada = Parada(paradas, primero.Tramo.DestinoId), Minutos = (int)espera.TotalMinutes };
                itinerarios.Add(Armar(paradas, [primero, segundo], transbordo));
            }
        }

        return itinerarios
            .OrderBy(i => i.Salida).ThenBy(i => i.Llegada).ThenBy(i => i.PrecioTotal)
            .ThenBy(i => string.Join("|", i.Tramos.Select(t => t.ServicioId)), StringComparer.Ordinal)
            .ToList();
    }

    private static ItinerarioDto Armar(Dictionary<string, Parada> paradas,
        IReadOnlyList<(Servicio Servicio, Tramo Tramo, Tarifa Tarifa)> tramos, TransbordoDto? transbordo)
    {
        var detalle = tramos.Select((x, indice) => new TramoItinerarioDto
        {
            Orden = indice + 1,
            ServicioId = x.Servicio.Id,
            TramoId = x.Tramo.Id,
            Origen = Parada(paradas, x.Tramo.OrigenId),
            Destino = Parada(paradas, x.Tramo.DestinoId),
            Salida = x.Servicio.Salida,
            Llegada = x.Servicio.Llegada,
            Importe = x.Tarifa.Importe,
            PoliticaAsiento = x.Servicio.PoliticaAsiento
        }).ToList();
        return new ItinerarioDto
        {
            Salida = detalle[0].Salida,
            Llegada = detalle[^1].Llegada,
            PrecioTotal = detalle.Sum(t => t.Importe),
            Transbordo = transbordo,
            Tramos = detalle
        };
    }

    private static ParadaDto Parada(Dictionary<string, Parada> paradas, string id) =>
        paradas.TryGetValue(id, out var parada) ? new ParadaDto { Id = parada.Id, Nombre = parada.Nombre } : new ParadaDto { Id = id };
}
