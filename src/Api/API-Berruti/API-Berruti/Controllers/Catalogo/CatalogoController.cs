using System.Globalization;
using API_Berruti.DTOs.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Catalogo;

// Consulta pública del catálogo de horarios (FIN-001): no requiere sesión, igual que la búsqueda de itinerarios.
[ApiController, Route("api/catalogo"), AllowAnonymous]
public class CatalogoController(ICatalogoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogoDto>> Obtener() => Ok(await service.ObtenerAsync());

    [HttpGet("salidas")]
    public async Task<ActionResult<SalidasDelDiaDto>> Salidas([FromQuery] string? fecha)
    {
        if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            return BadRequest("La fecha es obligatoria y debe tener el formato yyyy-MM-dd.");
        return Ok(await service.SalidasDelDiaAsync(dia));
    }
}
