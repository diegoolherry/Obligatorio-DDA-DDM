using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IParadasService
{
    Task<IReadOnlyList<ParadaDto>> ListarAsync();
    Task<ParadaDto> CrearAsync(ParadaDto dto);
    Task<ParadaDto> ActualizarAsync(string id, ParadaDto dto);
    Task EliminarAsync(string id);
}
