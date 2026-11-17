using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Catalogo;
using Web_App.Models.Operational;
using Web_App.Services.Authentication;

namespace Web_App.Services.Catalogo;

// FIN-008-03: cliente de api/administracion/catalogo. Cada operación devuelve el valor o un mensaje de error en español;
// 400 y 409 muestran el motivo de la API (MotivoApi), 404 y 403 tienen texto propio.
public class AdministracionCatalogoCliente(ISesionCliente sesion)
{
    private const string Base = "api/administracion/catalogo";
    private static readonly JsonSerializerOptions Json = CrearOpciones();

    public Task<(CatalogoAdministracionDto? Item, string? Error)> ObtenerAsync() =>
        SolicitarAsync<CatalogoAdministracionDto>(HttpMethod.Get, Base, null);

    public Task<(BloqueAdministracionDto? Item, string? Error)> CrearBloqueAsync(BloqueEscrituraDto dto) => SolicitarAsync<BloqueAdministracionDto>(HttpMethod.Post, Base + "/bloques", dto);
    public Task<(BloqueAdministracionDto? Item, string? Error)> ActualizarBloqueAsync(string id, BloqueEscrituraDto dto) => SolicitarAsync<BloqueAdministracionDto>(HttpMethod.Put, Ruta("bloques", id), dto);
    public Task<string?> EliminarBloqueAsync(string id) => EliminarAsync(Ruta("bloques", id));

    public Task<(RutaAdministracionDto? Item, string? Error)> CrearRutaAsync(RutaEscrituraDto dto) => SolicitarAsync<RutaAdministracionDto>(HttpMethod.Post, Base + "/rutas", dto);
    public Task<(RutaAdministracionDto? Item, string? Error)> ActualizarRutaAsync(string id, RutaEscrituraDto dto) => SolicitarAsync<RutaAdministracionDto>(HttpMethod.Put, Ruta("rutas", id), dto);
    public Task<string?> EliminarRutaAsync(string id) => EliminarAsync(Ruta("rutas", id));

    public Task<(SalidaAdministracionDto? Item, string? Error)> CrearSalidaAsync(SalidaEscrituraDto dto) => SolicitarAsync<SalidaAdministracionDto>(HttpMethod.Post, Base + "/salidas", dto);
    public Task<(SalidaAdministracionDto? Item, string? Error)> ActualizarSalidaAsync(string id, SalidaEscrituraDto dto) => SolicitarAsync<SalidaAdministracionDto>(HttpMethod.Put, Ruta("salidas", id), dto);
    public Task<string?> EliminarSalidaAsync(string id) => EliminarAsync(Ruta("salidas", id));

    public Task<(FeriadoDto? Item, string? Error)> CrearFeriadoAsync(FeriadoEscrituraDto dto) => SolicitarAsync<FeriadoDto>(HttpMethod.Post, Base + "/feriados", dto);
    public Task<(FeriadoDto? Item, string? Error)> ActualizarFeriadoAsync(DateOnly fecha, FeriadoEscrituraDto dto) => SolicitarAsync<FeriadoDto>(HttpMethod.Put, Ruta("feriados", Fecha(fecha)), dto);
    public Task<string?> EliminarFeriadoAsync(DateOnly fecha) => EliminarAsync(Ruta("feriados", Fecha(fecha)));

    public Task<(ExcepcionAdministracionDto? Item, string? Error)> CrearExcepcionAsync(ExcepcionEscrituraDto dto) => SolicitarAsync<ExcepcionAdministracionDto>(HttpMethod.Post, Base + "/excepciones", dto);
    public Task<(ExcepcionAdministracionDto? Item, string? Error)> ActualizarExcepcionAsync(string id, ExcepcionEscrituraDto dto) => SolicitarAsync<ExcepcionAdministracionDto>(HttpMethod.Put, Ruta("excepciones", id), dto);
    public Task<string?> EliminarExcepcionAsync(string id) => EliminarAsync(Ruta("excepciones", id));

    // Texto de error por estado HTTP; el 403 conserva el mensaje de la sesión.
    public static string MensajeDeError(RespuestaApiAdministracion respuesta) => respuesta.EstadoHttp switch
    {
        400 => respuesta.MotivoApi ?? "Los datos enviados no son válidos. Revisalos y reintentá.",
        409 => respuesta.MotivoApi ?? "La operación entra en conflicto con el estado actual del catálogo.",
        404 => "El elemento no existe o ya fue dado de baja.",
        _ => respuesta.Mensaje
    };

    private async Task<(T? Item, string? Error)> SolicitarAsync<T>(HttpMethod metodo, string ruta, object? dto) where T : class
    {
        var result = await sesion.SolicitarAdministradorAsync(metodo, ruta, dto == null ? null : JsonSerializer.Serialize(dto, Json));
        if (!result.Exito) return (null, MensajeDeError(result));
        if (string.IsNullOrWhiteSpace(result.Contenido)) return (null, "La API respondió sin datos. Reintentá.");
        try { return (JsonSerializer.Deserialize<T>(result.Contenido, Json), null); }
        catch (JsonException) { return (null, "La respuesta de la API no tiene el formato esperado."); }
    }

    private async Task<string?> EliminarAsync(string ruta)
    {
        var result = await sesion.SolicitarAdministradorAsync(HttpMethod.Delete, ruta);
        return result.Exito ? null : MensajeDeError(result);
    }

    private static string Ruta(string tipo, string id) => $"{Base}/{tipo}/{Uri.EscapeDataString(id)}";
    private static string Fecha(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
