using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/intentos-compra/{intentoId}/pagos"), Authorize(Roles = "Pasajero")]
public sealed class ConciliacionesPagoController(IConciliacionesPagoService service) : ControllerBase
{
    [HttpPost("sincronizar")]
    public async Task<ActionResult<SincronizarPagoResponseDto>> Sincronizar(
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        var identidad = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identidad)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(intentoId)) return BadRequest("El identificador del intento es obligatorio.");

        try
        {
            var resultado = await service.SincronizarAsync(identidad, intentoId, cancellationToken);
            return resultado.Pago.Estado == EstadoPago.Desconocido
                ? StatusCode(StatusCodes.Status202Accepted, resultado)
                : Ok(resultado);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return NotFound();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Error interno del servidor");
        }
    }
}
