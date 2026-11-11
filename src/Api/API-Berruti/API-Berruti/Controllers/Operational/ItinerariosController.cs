using System.Globalization;
using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

// Búsqueda pública: el pasajero consulta itinerarios sin sesión; la compra exigirá autenticación.
[ApiController, Route("api/itinerarios"), AllowAnonymous]
public class ItinerariosController(IItinerariosService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItinerarioDto>>> Buscar(
        [FromQuery] string? origenId, [FromQuery] string? destinoId, [FromQuery] string? fecha)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            return BadRequest("La fecha es obligatoria y debe tener el formato yyyy-MM-dd.");
        try { return Ok(await service.BuscarAsync(origenId, destinoId, dia)); }
        catch (KeyNotFoundException e) { return NotFound(e.Message); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }
}
