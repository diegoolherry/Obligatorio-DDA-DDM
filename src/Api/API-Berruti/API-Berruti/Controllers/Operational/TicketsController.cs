using System.Security.Claims;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

// MVP-005: el pasajero consulta solo sus tickets; el propietario sale del token, nunca de la solicitud.
[ApiController, Route("api/tickets"), Authorize(Roles = "Pasajero")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketsService _service;

    public TicketsController(ITicketsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ListaTicketsPasajeroDto>> Listar(CancellationToken cancellationToken = default)
    {
        var identificadorPublico = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identificadorPublico)) return Unauthorized();

        try
        {
            return Ok(await _service.ListarDelPasajeroAsync(identificadorPublico, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
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
