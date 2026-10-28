using Web_App.Models.Authentication;

namespace Web_App.Services.Authentication;

public interface ISesionCliente
{
    EstadoSesion Estado { get; }
    Task<ResultadoAutenticacion> IngresarAsync(SolicitudIngresoDto solicitud);
    Task<ResultadoAutenticacion> SalirAsync();
    Task<ResultadoAutenticacion> ConsultarIdentidadAsync();
    Task<ResultadoAutenticacion> ProbarPasajeroAsync();
    Task<ResultadoAutenticacion> ProbarAdministradorAsync();
    void Descartar();
}
