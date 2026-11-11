using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Catalogo;

public record DatosCatalogo(
    List<BloqueHorario> Bloques, List<RutaCatalogo> Rutas, List<ParadaRuta> ParadasRuta,
    List<SalidaCatalogo> Salidas, List<HoraSalida> Horas, List<Parada> Paradas,
    List<Feriado> Feriados, List<ExcepcionCalendario> Excepciones);

public interface ICatalogoRepository
{
    // El catálogo es chico (decenas de rutas, cientos de horas): se lee completo.
    Task<DatosCatalogo> ObtenerAsync();
}
