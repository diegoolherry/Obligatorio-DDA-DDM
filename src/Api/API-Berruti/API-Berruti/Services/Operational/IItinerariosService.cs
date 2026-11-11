using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IItinerariosService
{
    // Lanza ArgumentException si faltan las paradas o son iguales, y KeyNotFoundException si una no existe.
    Task<IReadOnlyList<ItinerarioDto>> BuscarAsync(string? origenId, string? destinoId, DateOnly fecha);
}
