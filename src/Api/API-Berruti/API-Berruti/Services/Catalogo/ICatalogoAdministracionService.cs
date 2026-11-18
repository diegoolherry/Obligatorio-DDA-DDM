using API_Berruti.DTOs.Catalogo;

namespace API_Berruti.Services.Catalogo;

// Regla de negocio que impide el cambio pedido (409); los datos inválidos son ArgumentException (400).
public class CatalogoConflictoException(string mensaje) : Exception(mensaje);

public interface ICatalogoAdministracionService
{
    Task<CatalogoAdministracionDto> ObtenerAsync();
    Task<BloqueAdministracionDto> CrearBloqueAsync(BloqueEscrituraDto dto);
    Task<BloqueAdministracionDto> ActualizarBloqueAsync(string id, BloqueEscrituraDto dto);
    Task EliminarBloqueAsync(string id);
    Task<RutaAdministracionDto> CrearRutaAsync(RutaEscrituraDto dto);
    Task<RutaAdministracionDto> ActualizarRutaAsync(string id, RutaEscrituraDto dto);
    Task EliminarRutaAsync(string id);
    Task<SalidaAdministracionDto> CrearSalidaAsync(SalidaEscrituraDto dto);
    Task<SalidaAdministracionDto> ActualizarSalidaAsync(string id, SalidaEscrituraDto dto);
    Task EliminarSalidaAsync(string id);
    Task<FeriadoDto> CrearFeriadoAsync(FeriadoEscrituraDto dto);
    Task<FeriadoDto> ActualizarFeriadoAsync(DateOnly fecha, FeriadoEscrituraDto dto);
    Task EliminarFeriadoAsync(DateOnly fecha);
    Task<ExcepcionAdministracionDto> CrearExcepcionAsync(ExcepcionEscrituraDto dto);
    Task<ExcepcionAdministracionDto> ActualizarExcepcionAsync(string id, ExcepcionEscrituraDto dto);
    Task EliminarExcepcionAsync(string id);
}
