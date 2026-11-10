using Web_App.Models.Authentication;
using Web_App.Models.Operational;

namespace Web_App.Services.Authentication;

public interface ISesionCliente
{
    EstadoSesion Estado { get; }
    Task<ResultadoAutenticacion> IngresarAsync(SolicitudIngresoDto solicitud);
    Task<ResultadoAutenticacion> SalirAsync();
    Task<ResultadoAutenticacion> ConsultarIdentidadAsync();
    Task<ResultadoAutenticacion> ProbarPasajeroAsync();
    Task<ResultadoAutenticacion> ProbarAdministradorAsync();
    Task<RespuestaApiAdministracion> SolicitarAdministradorAsync(HttpMethod metodo, string ruta, string? json = null);
    void Descartar();
}
