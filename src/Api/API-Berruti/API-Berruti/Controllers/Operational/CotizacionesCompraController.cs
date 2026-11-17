using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/intentos-compra"), Authorize(Roles = "Pasajero")]
public sealed class CotizacionesCompraController(ICotizacionesCompraService service) : ControllerBase
{
    [HttpGet("{intentoId}/cotizacion")]
    public async Task<ActionResult<CotizarCompraResponseDto>> Cotizar(
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            return Ok(await service.CotizarAsync(identificadorPublico, intentoId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (CotizacionNoDisponibleException)
        {
            return Conflict("El intento venció o la selección ya no está disponible.");
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
