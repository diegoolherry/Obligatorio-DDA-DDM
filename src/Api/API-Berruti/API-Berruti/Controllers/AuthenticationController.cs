using System.Security.Claims;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Security.Authentication;
using API_Berruti.Services.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers;

[ApiController]
[Route("api/autenticacion")]
public class AuthenticationController : ControllerBase
{
    private readonly IAutenticacionService _autenticacion;
    public AuthenticationController(IAutenticacionService autenticacion)
    {
        _autenticacion = autenticacion;
    }

    [AllowAnonymous]
    [HttpPost("ingreso")]
    public async Task<ActionResult<RespuestaIngresoDto>> Ingresar(SolicitudIngresoDto solicitud)
    {
        var respuesta = await _autenticacion.IngresarAsync(solicitud);
        if (respuesta != null) return Ok(respuesta);
        Response.Headers.WWWAuthenticate = "Bearer";
        return Unauthorized(new RespuestaErrorIngresoDto { Codigo = "ingresoRechazado",
            Mensaje = "No se pudo ingresar con las credenciales proporcionadas." });
    }

    [Authorize]
    [HttpPost("salida")]
    public async Task<IActionResult> Salir()
    {
        await _autenticacion.SalirAsync(EncabezadoBearer.Token(Request.Headers.Authorization.ToString())!);
        return NoContent();
    }

    [Authorize]
    [HttpGet("identidad")]
    public ActionResult<RespuestaSesionDto> Identidad() => Ok(RespuestaIdentidad());

    // Read-only authorization probes, not transport business operations.
    [Authorize(Roles = "Pasajero")]
    [HttpGet("prueba-pasajero")]
    public ActionResult<RespuestaSesionDto> PruebaPasajero() => Ok(RespuestaIdentidad());

    [Authorize(Roles = "Administrador")]
    [HttpGet("prueba-administrador")]
    public ActionResult<RespuestaSesionDto> PruebaAdministrador() => Ok(RespuestaIdentidad());

    private RespuestaSesionDto RespuestaIdentidad() => new()
    {
        IdentificadorUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        Roles = PoliticaSesion.Roles(User.FindAll(ClaimTypes.Role).Select(c => c.Value))
    };
}
