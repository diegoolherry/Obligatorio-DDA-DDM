using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IServiciosService
{
    Task<IReadOnlyList<ServicioDto>> ListarAsync();
    Task<ServicioDto> CrearAsync(ServicioDto dto);
    Task<ServicioDto> ActualizarAsync(string id, ServicioDto dto);
    Task EliminarAsync(string id);
}
