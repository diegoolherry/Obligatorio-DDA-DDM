using API_Berruti.Models.Authentication;

namespace API_Berruti.Repositories.Authentication;

public interface ISesionesRepository
{
    Task CrearAsync(Sesion sesion);
    Task<Sesion?> BuscarAsync(string huella);
    Task RevocarAsync(string huella, DateTime ahoraUtc);
}
