using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

// El segmento literal "pendiente" tiene precedencia de enrutamiento sobre "{intentoId}" de IntentosCompraController.
[ApiController, Route("api/intentos-compra/pendiente"), Authorize(Roles = "Pasajero")]
public sealed class IntentosPendientesController : ControllerBase
{
    private readonly IIntentosPendientesService _service;

    public IntentosPendientesController(IIntentosPendientesService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IntentoPendienteResponseDto>> Consultar(CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            var pendiente = await _service.ConsultarAsync(identificadorPublico, cancellationToken);
            return pendiente is null ? NoContent() : Ok(pendiente);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NoContent();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error interno del servidor");
        }
    }
}
