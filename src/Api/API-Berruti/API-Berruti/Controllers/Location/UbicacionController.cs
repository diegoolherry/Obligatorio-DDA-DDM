using System.Security.Claims;
using API_Berruti.DTOs.Location;
using API_Berruti.Services.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Location;

[ApiController, Route("api/ubicacion")]
public class UbicacionController(IUbicacionService service) : ControllerBase
{
    [HttpGet("cobrador/servicios"), Authorize(Roles = "Cobrador")]
    public async Task<ActionResult<IReadOnlyList<ServicioUbicacionDto>>> ListarAsignados()
    {
        var publico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(publico)) return Unauthorized();
        try { return Ok(await service.ListarServiciosAsignadosAsync(publico)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("servicios/{servicioId}/posiciones"), Authorize(Roles = "Cobrador")]
    public async Task<ActionResult<PublicacionPosicionResponse>> Publicar(string servicioId, PublicarPosicionRequest solicitud)
    {
        var publico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(publico)) return Unauthorized();
        try { return StatusCode(StatusCodes.Status201Created, await service.PublicarAsync(publico, servicioId, solicitud)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }

    [HttpGet("servicios"), Authorize(Roles = "Pasajero")]
    public async Task<ActionResult<IReadOnlyList<ServicioUbicacionDto>>> ListarServicios() => Ok(await service.ListarServiciosAsync());

    [HttpGet("servicios/{servicioId}"), Authorize(Roles = "Pasajero")]
    public async Task<ActionResult<ConsultaUbicacionResponse>> Consultar(string servicioId)
    {
        try { return Ok(await service.ConsultarAsync(servicioId)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
