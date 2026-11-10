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
        servicio.Fecha = dto.Fecha.Date;
        servicio.HoraSalida = dto.HoraSalida;
        servicio.HoraLlegada = dto.HoraLlegada;
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
        if (dto.Fecha == default) throw new ArgumentException("La fecha del servicio es obligatoria.");
        if (dto.Capacidad < 1) throw new ArgumentException("La capacidad debe ser mayor a cero.");
        if (dto.HoraSalida < TimeSpan.Zero || dto.HoraSalida >= TimeSpan.FromDays(1) ||
            dto.HoraLlegada < TimeSpan.Zero || dto.HoraLlegada >= TimeSpan.FromDays(1))
            throw new ArgumentException("Los horarios deben estar dentro del mismo día.");
        if (!Enum.IsDefined(dto.PoliticaAsiento)) throw new ArgumentException("La política de asiento no es válida.");
    }
    private static Servicio DesdeDto(ServicioDto dto) => new()
    {
        TramoId = dto.TramoId,
        Fecha = dto.Fecha.Date,
        HoraSalida = dto.HoraSalida,
        HoraLlegada = dto.HoraLlegada,
        Capacidad = dto.Capacidad,
        PoliticaAsiento = dto.PoliticaAsiento
    };
    private static ServicioDto ToDto(Servicio s) => new()
    {
        Id = s.Id,
        TramoId = s.TramoId,
        Fecha = s.Fecha.Date,
        HoraSalida = s.HoraSalida,
        HoraLlegada = s.HoraLlegada,
        Capacidad = s.Capacidad,
        PoliticaAsiento = s.PoliticaAsiento
    };
}
