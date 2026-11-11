using API_Berruti.DTOs.Catalogo;

namespace API_Berruti.Services.Catalogo;

public interface IGeneracionServiciosService
{
    Task<ResultadoGeneracionDto> GenerarAsync(GenerarServiciosDto dto);
}
