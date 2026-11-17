using API_Berruti.Models.Catalogo;

namespace API_Berruti.Repositories.Catalogo;

// Otro cambio guardó una clave repetida o tocó las mismas filas al mismo tiempo.
public class CatalogoConcurrenteException(Exception interna)
    : Exception("Otro cambio del catálogo se guardó al mismo tiempo; recargá y volvé a intentar.", interna);

// FIN-008: escritura del catálogo. La lectura completa (incluidas las bajas) es ICatalogoRepository.
// Una baja se guarda como actualización con Activo = false.
public interface ICatalogoAdministracionRepository
{
    Task<Dictionary<string, int>> ContarServiciosPorSalidaAsync();
    Task AgregarBloqueAsync(BloqueHorario bloque);
    Task ActualizarBloqueAsync(BloqueHorario bloque);
    Task AgregarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta> paradas);
    // paradas null conserva las paradas actuales.
    Task ActualizarRutaAsync(RutaCatalogo ruta, IReadOnlyList<ParadaRuta>? paradas);
    Task AgregarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas);
    Task ActualizarSalidaAsync(SalidaCatalogo salida, IReadOnlyList<HoraSalida> horas);
}
