using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

[ApiController, Route("api/administracion/escenario-demo"), Authorize(Roles = "Administrador")]
public class PreparacionDemoController(IPreparacionDemoService service) : ControllerBase
{
    [HttpPost("preparar-demo")]
    public async Task<ActionResult<ResultadoPreparacionDemoDto>> Preparar(SolicitudPreparacionDemoDto solicitud)
    {
        try
        {
            var resultado = await service.PrepararAsync(solicitud);
            return resultado.Conflicto ? Conflict(resultado) : Ok(resultado);
        }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }
}
