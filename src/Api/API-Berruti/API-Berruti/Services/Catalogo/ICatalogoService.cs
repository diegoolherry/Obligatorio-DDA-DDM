using API_Berruti.DTOs.Catalogo;

namespace API_Berruti.Services.Catalogo;

public interface ICatalogoService
{
    Task<CatalogoDto> ObtenerAsync();
    Task<SalidasDelDiaDto> SalidasDelDiaAsync(DateOnly fecha);
}
