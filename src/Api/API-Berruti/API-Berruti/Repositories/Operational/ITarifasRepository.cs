using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface ITarifasRepository
{
    Task<List<Tarifa>> ListarAsync();
    Task<Tarifa?> BuscarAsync(string id);
    Task<bool> ExisteTramoAsync(string tramoId);
    Task AgregarAsync(Tarifa tarifa);
    Task GuardarAsync();
    Task EliminarAsync(Tarifa tarifa);
}
