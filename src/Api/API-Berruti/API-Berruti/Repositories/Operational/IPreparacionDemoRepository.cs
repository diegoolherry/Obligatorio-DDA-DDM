using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface IPreparacionDemoRepository
{
    Task<List<Parada>> ListarParadasAsync();
    Task<List<Tramo>> ListarTramosAsync();
    Task<List<Servicio>> ListarServiciosAsync();
    Task<List<Tarifa>> ListarTarifasAsync();
    Task AgregarParadaAsync(Parada parada);
    Task AgregarTramoAsync(Tramo tramo);
    Task AgregarServicioAsync(Servicio servicio);
    Task AgregarTarifaAsync(Tarifa tarifa);
    Task GuardarAsync();
}
