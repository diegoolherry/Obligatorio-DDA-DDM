using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/intentos-compra"), Authorize(Roles = "Pasajero")]
public sealed class CotizacionesCompraController : ControllerBase
{
    private readonly ICotizacionesCompraService _service;

    public CotizacionesCompraController(ICotizacionesCompraService service)
    {
        _service = service;
    }

    [HttpGet("{intentoId}/cotizacion")]
    public async Task<ActionResult<CotizarCompraResponseDto>> Cotizar(
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            return Ok(await _service.CotizarAsync(identificadorPublico, intentoId, cancellationToken));
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
