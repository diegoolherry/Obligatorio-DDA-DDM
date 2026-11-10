using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class TarifasService(ITarifasRepository repository) : ITarifasService
{
    private const decimal MaximoImporte = 9999999999999999.99m;
    public async Task<IReadOnlyList<TarifaDto>> ListarAsync() => (await repository.ListarAsync()).Select(ToDto).ToArray();
    public async Task<TarifaDto> CrearAsync(TarifaDto dto)
    {
        await ValidarAsync(dto);
        var tarifa = DesdeDto(dto);
        await repository.AgregarAsync(tarifa);
        await repository.GuardarAsync();
        return ToDto(tarifa);
    }
    public async Task<TarifaDto> ActualizarAsync(string id, TarifaDto dto)
    {
        var tarifa = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await ValidarAsync(dto);
        tarifa.TramoId = dto.TramoId;
        tarifa.Importe = dto.Importe;
        tarifa.VigenteDesde = dto.VigenteDesde.Date;
        tarifa.VigenteHasta = dto.VigenteHasta.Date;
        await repository.GuardarAsync();
        return ToDto(tarifa);
    }
    public async Task EliminarAsync(string id)
    {
        var tarifa = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await repository.EliminarAsync(tarifa);
        await repository.GuardarAsync();
    }
    private async Task ValidarAsync(TarifaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TramoId)) throw new ArgumentException("El tramo es obligatorio.");
        if (!await repository.ExisteTramoAsync(dto.TramoId)) throw new ArgumentException("El tramo seleccionado no existe.");
        if (dto.Importe < 0 || dto.Importe > MaximoImporte || decimal.Round(dto.Importe, 2) != dto.Importe)
            throw new ArgumentException("El importe debe ser no negativo y tener como máximo dos decimales.");
        if (dto.VigenteDesde == default || dto.VigenteHasta == default)
            throw new ArgumentException("El período de vigencia es obligatorio.");
        if (dto.VigenteHasta.Date < dto.VigenteDesde.Date)
            throw new ArgumentException("La fecha final de vigencia no puede preceder a la inicial.");
    }
    private static Tarifa DesdeDto(TarifaDto dto) => new()
    {
        TramoId = dto.TramoId,
        Importe = dto.Importe,
        VigenteDesde = dto.VigenteDesde.Date,
        VigenteHasta = dto.VigenteHasta.Date
    };
    private static TarifaDto ToDto(Tarifa t) => new()
    {
        Id = t.Id,
        TramoId = t.TramoId,
        Importe = t.Importe,
        VigenteDesde = t.VigenteDesde.Date,
        VigenteHasta = t.VigenteHasta.Date
    };
}
