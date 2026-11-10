using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface ITramosService
{
    Task<IReadOnlyList<TramoDto>> ListarAsync();
    Task<TramoDto> CrearAsync(TramoDto dto);
    Task<TramoDto> ActualizarAsync(string id, TramoDto dto);
    Task EliminarAsync(string id);
}
