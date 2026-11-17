using API_Berruti.DTOs.Catalogo;
using API_Berruti.Repositories.Catalogo;
using API_Berruti.Services.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Berruti.Controllers.Catalogo;

// FIN-008: ABM del catálogo. Eliminar es una baja lógica: el elemento deja de mostrarse y de generar
// Servicios, y los Servicios ya generados se conservan.
[ApiController, Route("api/administracion/catalogo"), Authorize(Roles = "Administrador")]
public class CatalogoAdministracionController(ICatalogoAdministracionService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<CatalogoAdministracionDto>> Obtener() => Ok(await service.ObtenerAsync());

    [HttpPost("bloques")] public Task<ActionResult<BloqueAdministracionDto>> CrearBloque(BloqueEscrituraDto dto) =>
        Ejecutar(() => service.CrearBloqueAsync(dto), creado => CreatedAtAction(nameof(Obtener), creado));
    [HttpPut("bloques/{id}")] public Task<ActionResult<BloqueAdministracionDto>> ActualizarBloque(string id, BloqueEscrituraDto dto) =>
        Ejecutar(() => service.ActualizarBloqueAsync(id, dto), Ok);
    [HttpDelete("bloques/{id}")] public Task<IActionResult> EliminarBloque(string id) => Eliminar(() => service.EliminarBloqueAsync(id));

    [HttpPost("rutas")] public Task<ActionResult<RutaAdministracionDto>> CrearRuta(RutaEscrituraDto dto) =>
        Ejecutar(() => service.CrearRutaAsync(dto), creada => CreatedAtAction(nameof(Obtener), creada));
    [HttpPut("rutas/{id}")] public Task<ActionResult<RutaAdministracionDto>> ActualizarRuta(string id, RutaEscrituraDto dto) =>
        Ejecutar(() => service.ActualizarRutaAsync(id, dto), Ok);
    [HttpDelete("rutas/{id}")] public Task<IActionResult> EliminarRuta(string id) => Eliminar(() => service.EliminarRutaAsync(id));

    [HttpPost("salidas")] public Task<ActionResult<SalidaAdministracionDto>> CrearSalida(SalidaEscrituraDto dto) =>
        Ejecutar(() => service.CrearSalidaAsync(dto), creada => CreatedAtAction(nameof(Obtener), creada));
    [HttpPut("salidas/{id}")] public Task<ActionResult<SalidaAdministracionDto>> ActualizarSalida(string id, SalidaEscrituraDto dto) =>
        Ejecutar(() => service.ActualizarSalidaAsync(id, dto), Ok);
    [HttpDelete("salidas/{id}")] public Task<IActionResult> EliminarSalida(string id) => Eliminar(() => service.EliminarSalidaAsync(id));

    // El Service valida y lanza; acá se traduce cada caso esperado a su código HTTP.
    private async Task<ActionResult<T>> Ejecutar<T>(Func<Task<T>> accion, Func<T, ActionResult> exito)
    {
        try { return exito(await accion()); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (CatalogoConflictoException e) { return Conflict(e.Message); }
        catch (CatalogoConcurrenteException e) { return Conflict(e.Message); }
    }

    private async Task<IActionResult> Eliminar(Func<Task> accion)
    {
        try { await accion(); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (CatalogoConcurrenteException e) { return Conflict(e.Message); }
    }
}
