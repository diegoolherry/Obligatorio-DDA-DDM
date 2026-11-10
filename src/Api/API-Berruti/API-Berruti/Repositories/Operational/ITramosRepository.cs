using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface ITramosRepository
{
    Task<List<Tramo>> ListarAsync();
    Task<Tramo?> BuscarAsync(string id);
    Task<bool> ExisteParadaAsync(string id);
    Task<Parada?> BuscarParadaAsync(string id);
    Task AgregarAsync(Tramo tramo);
    Task GuardarAsync();
    Task EliminarAsync(Tramo tramo);
}
