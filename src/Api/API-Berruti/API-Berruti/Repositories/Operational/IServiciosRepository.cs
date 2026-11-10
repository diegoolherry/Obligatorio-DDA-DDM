using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IServiciosRepository
{
    Task<List<Servicio>> ListarAsync();
    Task<Servicio?> BuscarAsync(string id);
    Task<bool> ExisteTramoAsync(string tramoId);
    Task AgregarAsync(Servicio servicio);
    Task GuardarAsync();
    Task EliminarAsync(Servicio servicio);
}
