using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Location;
using Web_App.Services.Authentication;

namespace Web_App.Services.Location;

public class UbicacionPasajeroCliente(ISesionCliente sesion)
{
    private static readonly JsonSerializerOptions Json = CrearOpciones();

    public async Task<(IReadOnlyList<ServicioUbicacionDto> Items, int? Estado, string? Error)> ListarServiciosAsync()
    {
        var (items, estado, error) = await LeerAsync<List<ServicioUbicacionDto>>("api/ubicacion/servicios", []);
        return (items, estado, error);
    }

    public Task<(ConsultaUbicacionResponse? Item, int? Estado, string? Error)> ConsultarAsync(string servicioId) =>
        LeerAsync<ConsultaUbicacionResponse?>("api/ubicacion/servicios/" + Uri.EscapeDataString(servicioId), null);

    private async Task<(T Item, int? Estado, string? Error)> LeerAsync<T>(string ruta, T vacio)
    {
        var result = await sesion.SolicitarAutenticadaAsync(HttpMethod.Get, ruta);
        if (!result.Exito || result.Contenido == null)
            return (vacio, result.EstadoHttp, Mensaje(result.EstadoHttp, result.Mensaje));
        try
        {
            var item = JsonSerializer.Deserialize<T>(result.Contenido, Json);
            return item == null ? (vacio, result.EstadoHttp, "La respuesta de la API no es válida.") : (item, result.EstadoHttp, null);
        }
        catch (JsonException) { return (vacio, result.EstadoHttp, "La respuesta de la API no es válida."); }
    }

    private static string Mensaje(int? estado, string mensaje) => estado switch
    {
        403 => "Tu cuenta no tiene permiso de pasajero para consultar la ubicación.",
        404 => "El servicio ya no existe. Actualizá la lista.",
        _ => mensaje
    };

    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
