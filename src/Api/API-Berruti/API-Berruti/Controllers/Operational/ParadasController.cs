using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/administracion/paradas"), Authorize(Roles = "Administrador")]
public class ParadasController(IParadasService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ParadaDto>>> Listar() => Ok(await service.ListarAsync());
    [HttpPost] public async Task<ActionResult<ParadaDto>> Crear(ParadaDto dto)
    { try { var creada = await service.CrearAsync(dto); return CreatedAtAction(nameof(Listar), creada); } catch (ArgumentException e) { return BadRequest(e.Message); } }
    [HttpPut("{id}")] public async Task<ActionResult<ParadaDto>> Actualizar(string id, ParadaDto dto)
    { try { return Ok(await service.ActualizarAsync(id, dto)); } catch (ArgumentException) { return BadRequest("Los datos de la parada no son válidos."); } catch (KeyNotFoundException) { return NotFound(); } }
    [HttpDelete("{id}")] public async Task<IActionResult> Eliminar(string id)
    { try { await service.EliminarAsync(id); return NoContent(); } catch (KeyNotFoundException) { return NotFound(); } catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Conflict("No se puede eliminar una parada que ya pertenece a un tramo."); } }
}
