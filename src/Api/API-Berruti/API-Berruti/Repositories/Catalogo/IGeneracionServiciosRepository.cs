using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Catalogo;

// Otra generación guardó los mismos servicios al mismo tiempo (índice único ux_servicio_generado).
public class GeneracionConcurrenteException(Exception interna)
    : Exception("Otra generación de servicios guardó los mismos datos al mismo tiempo.", interna);

public interface IGeneracionServiciosRepository
{
    Task<List<Tramo>> ListarTramosAsync();
    // Servicios generados desde el catálogo con salida en [desde, hasta).
    Task<List<Servicio>> ListarGeneradosAsync(DateTime desde, DateTime hasta);
    // Guarda todo en una transacción; lanza GeneracionConcurrenteException si choca con el índice único.
    Task GuardarAsync(IReadOnlyList<Tramo> tramosNuevos, IReadOnlyList<Servicio> serviciosNuevos);
}
