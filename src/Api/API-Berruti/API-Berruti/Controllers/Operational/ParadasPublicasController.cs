using API_Berruti.DTOs.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Operational;

// Lista pública de paradas para elegir origen y destino de la búsqueda; la edición sigue en administración.
[ApiController, Route("api/paradas"), AllowAnonymous]
public class ParadasPublicasController(IParadasService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<ParadaDto>>> Listar() => Ok(await service.ListarAsync());
}
