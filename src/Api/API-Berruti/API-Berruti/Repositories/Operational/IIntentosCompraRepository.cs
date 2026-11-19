using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public record ServicioCompra(Servicio Servicio, string OrigenId, string DestinoId);

public interface IIntentosCompraRepository
{
    Task<IReadOnlyList<ServicioCompra>> BuscarServiciosAsync(IReadOnlyCollection<string> servicioIds);
    Task<IniciarIntentoCompraResponseDto> CrearAsync(string identificadorPublico, ItinerarioDto itinerario, CancellationToken cancellationToken = default);
    Task<IniciarIntentoCompraResponseDto> ConsultarAsync(string identificadorPublico, string intentoId, CancellationToken cancellationToken = default);
}
