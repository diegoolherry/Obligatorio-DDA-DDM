using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

// MVP-005: solo el rol Cobrador valida. Los cinco resultados son respuestas de negocio (200); 400/401/403/500 son del contrato HTTP.
[ApiController, Route("api/validaciones-ticket"), Authorize(Roles = "Cobrador")]
public sealed class ValidacionesTicketController : ControllerBase
{
    private readonly ITicketsService _service;

    public ValidacionesTicketController(ITicketsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ResultadoValidacionTicketDto>> Validar(
        [FromBody] ValidarTicketRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            return Ok(await _service.ValidarAsync(identificadorPublico, solicitud, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (CobradorNoAsignadoException)
        {
            return Forbid();
        }
        catch (ArgumentException)
        {
            return BadRequest("El código y el tramo son obligatorios.");
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
