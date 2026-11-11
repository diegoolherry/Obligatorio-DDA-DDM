using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IBusquedaItinerariosRepository
{
    Task<List<Parada>> ListarParadasAsync();
    Task<List<Tramo>> ListarTramosAsync();
    // Servicios con salida en [desde, hasta).
    Task<List<Servicio>> ListarServiciosAsync(DateTime desde, DateTime hasta);
    // Tarifas cuya vigencia incluye el día indicado.
    Task<List<Tarifa>> ListarTarifasVigentesAsync(DateTime dia);
}
