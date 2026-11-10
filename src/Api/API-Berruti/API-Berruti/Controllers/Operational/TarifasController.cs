using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/administracion/tarifas"), Authorize(Roles = "Administrador")]
public class TarifasController(ITarifasService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<TarifaDto>>> Listar() => Ok(await service.ListarAsync());
    [HttpPost] public async Task<ActionResult<TarifaDto>> Crear(TarifaDto dto)
    { try { var creada = await service.CrearAsync(dto); return CreatedAtAction(nameof(Listar), creada); } catch (ArgumentException e) { return BadRequest(e.Message); } }
    [HttpPut("{id}")] public async Task<ActionResult<TarifaDto>> Actualizar(string id, TarifaDto dto)
    { try { return Ok(await service.ActualizarAsync(id, dto)); } catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException e) { return BadRequest(e.Message); } }
    [HttpDelete("{id}")] public async Task<IActionResult> Eliminar(string id)
    { try { await service.EliminarAsync(id); return NoContent(); } catch (KeyNotFoundException) { return NotFound(); } }
}
