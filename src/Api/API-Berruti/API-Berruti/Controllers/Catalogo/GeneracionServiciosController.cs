using API_Berruti.DTOs.Catalogo;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Catalogo;

// Solo el administrador genera Servicios fechados desde el catálogo.
[ApiController, Route("api/administracion/catalogo/servicios"), Authorize(Roles = "Administrador")]
public class GeneracionServiciosController(IGeneracionServiciosService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResultadoGeneracionDto>> Generar([FromBody] GenerarServiciosDto dto)
    {
        try { return Ok(await service.GenerarAsync(dto)); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
        catch (GeneracionConcurrenteException e) { return Conflict(e.Message + " Volvé a intentarlo: la generación no duplica servicios."); }
    }
}
