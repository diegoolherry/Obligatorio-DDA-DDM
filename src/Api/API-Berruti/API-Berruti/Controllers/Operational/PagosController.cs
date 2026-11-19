using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/intentos-compra/{intentoId}/pagos"), Authorize(Roles = "Pasajero")]
public sealed class PagosController : ControllerBase
{
    private readonly IPagosService _service;

    public PagosController(IPagosService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<PagoResponseDto>> Iniciar(
        string intentoId,
        [FromBody] IniciarPagoRequestDto? solicitud,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();
        if (solicitud is null) return BadRequest("Se requiere aceptar la cotización del servidor.");

        try
        {
            var pago = await _service.IniciarAsync(identificadorPublico, intentoId, solicitud, cancellationToken);
            return pago.Estado == EstadoPago.Desconocido
                ? StatusCode(StatusCodes.Status202Accepted, pago)
                : Ok(pago);
        }
        catch (CotizacionModificadaException exception)
        {
            return Conflict(exception.Cotizacion);
        }
        catch (CotizacionNoDisponibleException)
        {
            return Conflict("El intento venció o la selección ya no está disponible.");
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

    [HttpGet("actual")]
    public async Task<ActionResult<PagoResponseDto>> ConsultarActual(
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            var pago = await _service.ConsultarActualAsync(identificadorPublico, intentoId, cancellationToken);
            return pago is null ? NotFound() : Ok(pago);
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
