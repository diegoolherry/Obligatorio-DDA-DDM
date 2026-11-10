using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface ITarifasService
{
    Task<IReadOnlyList<TarifaDto>> ListarAsync();
    Task<TarifaDto> CrearAsync(TarifaDto dto);
    Task<TarifaDto> ActualizarAsync(string id, TarifaDto dto);
    Task EliminarAsync(string id);
}
