using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/administracion/tramos"), Authorize(Roles = "Administrador")]
public class TramosController(ITramosService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<TramoDto>>> Listar() => Ok(await service.ListarAsync());
    [HttpPost] public async Task<ActionResult<TramoDto>> Crear(TramoDto dto)
    { try { var creado = await service.CrearAsync(dto); return CreatedAtAction(nameof(Listar), creado); } catch (ArgumentException e) { return BadRequest(e.Message); } }
    [HttpPut("{id}")] public async Task<ActionResult<TramoDto>> Actualizar(string id, TramoDto dto)
    { try { return Ok(await service.ActualizarAsync(id, dto)); } catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException e) { return BadRequest(e.Message); } }
    [HttpDelete("{id}")] public async Task<IActionResult> Eliminar(string id)
    { try { await service.EliminarAsync(id); return NoContent(); } catch (KeyNotFoundException) { return NotFound(); } catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Conflict("No se puede eliminar un tramo utilizado por otros datos."); } }
}
