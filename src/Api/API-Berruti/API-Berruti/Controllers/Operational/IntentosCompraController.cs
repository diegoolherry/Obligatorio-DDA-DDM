using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/intentos-compra"), Authorize(Roles = "Pasajero")]
public class IntentosCompraController : ControllerBase
{
    private readonly IIntentosCompraService _service;

    public IntentosCompraController(IIntentosCompraService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<IniciarIntentoCompraResponseDto>> Crear(
        [FromBody] IniciarIntentoCompraRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();
        if (solicitud is null) return BadRequest("La solicitud es obligatoria.");

        try
        {
            var intento = await _service.CrearAsync(identificadorPublico, solicitud, cancellationToken);
            return CreatedAtAction(nameof(Consultar), new { intentoId = intento.IntentoId }, intento);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (AsientoNoDisponibleException)
        {
            return Conflict("No se pudo reservar un asiento para el itinerario solicitado.");
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

    [HttpGet("{intentoId}")]
    public async Task<ActionResult<IniciarIntentoCompraResponseDto>> Consultar(
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            return Ok(await _service.ConsultarAsync(identificadorPublico, intentoId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
