using API_Berruti.DTOs.Location;
using API_Berruti.Models.Location;

namespace API_Berruti.Repositories.Location;

public interface IUbicacionRepository
{
    Task<string?> BuscarUsuarioIdPorPublicoAsync(string identificadorPublico);
    Task<ServicioUbicacionDto?> BuscarServicioAsync(string servicioId);
    Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsync();
    Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsignadosAsync(string usuarioId);
    Task<bool> EstaAsignadoAsync(string usuarioId, string servicioId);
    Task<PosicionRecibida?> BuscarUltimaPosicionAsync(string servicioId);
    Task AgregarPosicionAsync(PosicionRecibida posicion);
    Task GuardarAsync();
}
