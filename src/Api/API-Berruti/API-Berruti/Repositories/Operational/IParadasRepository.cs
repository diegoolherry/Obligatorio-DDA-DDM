using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IParadasRepository
{
    Task<List<Parada>> ListarAsync();
    Task<Parada?> BuscarAsync(string id);
    Task AgregarAsync(Parada parada);
    Task GuardarAsync();
    Task EliminarAsync(Parada parada);
}
