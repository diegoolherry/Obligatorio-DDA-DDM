using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Operational;
using Web_App.Services.Authentication;

namespace Web_App.Services.Operational;

public class AdministracionOperativaCliente(ISesionCliente sesion)
{
    private static readonly JsonSerializerOptions Json = CrearOpciones();
    public async Task<(IReadOnlyList<ParadaDto> Items, string? Error)> ListarParadasAsync()
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Get, "api/administracion/paradas");
        return (!result.Exito || result.Contenido == null) ? ([], result.Mensaje) :
            (JsonSerializer.Deserialize<List<ParadaDto>>(result.Contenido, Json) ?? [], null);
    }
    public async Task<(IReadOnlyList<TramoDto> Items, string? Error)> ListarTramosAsync()
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Get, "api/administracion/tramos");
        return (!result.Exito || result.Contenido == null) ? ([], result.Mensaje) :
            (JsonSerializer.Deserialize<List<TramoDto>>(result.Contenido, Json) ?? [], null);
    }
    public async Task<(IReadOnlyList<ServicioDto> Items, string? Error)> ListarServiciosAsync()
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Get, "api/administracion/servicios");
        return (!result.Exito || result.Contenido == null) ? ([], result.Mensaje) :
            (JsonSerializer.Deserialize<List<ServicioDto>>(result.Contenido, Json) ?? [], null);
    }
    public async Task<(IReadOnlyList<TarifaDto> Items, string? Error)> ListarTarifasAsync()
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Get, "api/administracion/tarifas");
        return (!result.Exito || result.Contenido == null) ? ([], result.Mensaje) :
            (JsonSerializer.Deserialize<List<TarifaDto>>(result.Contenido, Json) ?? [], null);
    }
    public async Task<string?> GuardarParadaAsync(ParadaDto dto) => await EnviarAsync(dto.Id == null ? HttpMethod.Post : HttpMethod.Put,
        "api/administracion/paradas" + (dto.Id == null ? "" : "/" + Uri.EscapeDataString(dto.Id)), dto);
    public async Task<string?> GuardarTramoAsync(TramoDto dto) => await EnviarAsync(dto.Id == null ? HttpMethod.Post : HttpMethod.Put,
        "api/administracion/tramos" + (dto.Id == null ? "" : "/" + Uri.EscapeDataString(dto.Id)), dto);
    public async Task<string?> GuardarServicioAsync(ServicioDto dto) => await EnviarAsync(dto.Id == null ? HttpMethod.Post : HttpMethod.Put,
        "api/administracion/servicios" + (dto.Id == null ? "" : "/" + Uri.EscapeDataString(dto.Id)), dto);
    public async Task<string?> GuardarTarifaAsync(TarifaDto dto) => await EnviarAsync(dto.Id == null ? HttpMethod.Post : HttpMethod.Put,
        "api/administracion/tarifas" + (dto.Id == null ? "" : "/" + Uri.EscapeDataString(dto.Id)), dto);
    public async Task<ResultadoPreparacionDemoDto> PrepararDemoAsync(SolicitudPreparacionDemoDto dto)
    {
        var response = await sesion.SolicitarAdministradorAsync(HttpMethod.Post, "api/administracion/escenario-demo/preparar-demo",
            JsonSerializer.Serialize(dto, Json));
        if (response.Exito && response.Contenido != null)
            return JsonSerializer.Deserialize<ResultadoPreparacionDemoDto>(response.Contenido, Json)
                ?? new() { Mensaje = "La respuesta de la API no es válida." };
        if (response.EstadoHttp == 409)
            return new() { Conflicto = true, SinCambios = true, Mensaje = "La configuración existente es incompatible; la API no aplicó cambios." };
        return new() { Mensaje = response.Mensaje };
    }
    public async Task<string?> EliminarAsync(string tipo, string id)
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Delete,
            $"api/administracion/{tipo}/{Uri.EscapeDataString(id)}");
        return result.Exito ? null : result.Mensaje;
    }
    private async Task<string?> EnviarAsync(HttpMethod method, string path, object dto)
    {
        var result = await sesion.SolicitarAdministradorAsync(method, path, JsonSerializer.Serialize(dto, Json));
        return result.Exito ? null : result.Mensaje;
    }
    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
