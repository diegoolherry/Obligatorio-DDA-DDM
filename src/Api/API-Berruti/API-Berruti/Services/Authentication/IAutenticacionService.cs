using System.Security.Claims;
using API_Berruti.DTOs.Authentication;

namespace API_Berruti.Services.Authentication;

public interface IAutenticacionService
{
    Task<RespuestaIngresoDto?> IngresarAsync(SolicitudIngresoDto solicitud);
    Task<ClaimsPrincipal?> AutenticarAsync(string token);
    Task SalirAsync(string token);
}
