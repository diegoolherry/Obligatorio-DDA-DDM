using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class ServiciosService(IServiciosRepository repository) : IServiciosService
{
    public async Task<IReadOnlyList<ServicioDto>> ListarAsync() => (await repository.ListarAsync()).Select(ToDto).ToArray();
    public async Task<ServicioDto> CrearAsync(ServicioDto dto)
    {
        await ValidarAsync(dto);
        var servicio = DesdeDto(dto);
        await repository.AgregarAsync(servicio);
        await repository.GuardarAsync();
        return ToDto(servicio);
    }
    public async Task<ServicioDto> ActualizarAsync(string id, ServicioDto dto)
    {
        var servicio = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await ValidarAsync(dto);
        servicio.TramoId = dto.TramoId;
        servicio.Salida = dto.Salida;
        servicio.Llegada = dto.Llegada;
        servicio.Capacidad = dto.Capacidad;
        servicio.PoliticaAsiento = dto.PoliticaAsiento;
        await repository.GuardarAsync();
        return ToDto(servicio);
    }
    public async Task EliminarAsync(string id)
    {
        var servicio = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await repository.EliminarAsync(servicio);
        await repository.GuardarAsync();
    }
    private async Task ValidarAsync(ServicioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TramoId)) throw new ArgumentException("El tramo es obligatorio.");
        if (!await repository.ExisteTramoAsync(dto.TramoId)) throw new ArgumentException("El tramo seleccionado no existe.");
        if (dto.Salida == default) throw new ArgumentException("La salida del servicio es obligatoria.");
        if (dto.Llegada <= dto.Salida) throw new ArgumentException("La llegada debe ser posterior a la salida.");
        if (dto.Capacidad < 1) throw new ArgumentException("La capacidad debe ser mayor a cero.");
        if (!Enum.IsDefined(dto.PoliticaAsiento)) throw new ArgumentException("La política de asiento no es válida.");
    }
    private static Servicio DesdeDto(ServicioDto dto) => new()
    {
        TramoId = dto.TramoId,
        Salida = dto.Salida,
        Llegada = dto.Llegada,
        Capacidad = dto.Capacidad,
        PoliticaAsiento = dto.PoliticaAsiento
    };
    private static ServicioDto ToDto(Servicio s) => new()
    {
        Id = s.Id,
        TramoId = s.TramoId,
        Salida = s.Salida,
        Llegada = s.Llegada,
        Capacidad = s.Capacidad,
        PoliticaAsiento = s.PoliticaAsiento
    };
}
