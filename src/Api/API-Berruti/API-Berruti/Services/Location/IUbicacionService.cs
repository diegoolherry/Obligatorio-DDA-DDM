using API_Berruti.DTOs.Location;

namespace API_Berruti.Services.Location;

public interface IUbicacionService
{
    Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsignadosAsync(string identificadorPublico);
    Task<PublicacionPosicionResponse> PublicarAsync(string identificadorPublico, string servicioId, PublicarPosicionRequest solicitud);
    Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsync();
    Task<ConsultaUbicacionResponse> ConsultarAsync(string servicioId);
}
