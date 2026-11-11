using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Operational;
using Web_App.Services.Authentication;

namespace Web_App.Services.Operational;

// Cliente de las rutas públicas de búsqueda: no envía token ni depende de la sesión.
public class BusquedaItinerariosCliente(IHttpClientFactory factory, IConfiguration configuration)
{
    public const string ErrorConexion = "No se pudo conectar con la API. Revisá tu conexión e intentá de nuevo.";
    public const string ErrorServidor = "La API no pudo completar la búsqueda. Intentá de nuevo en unos minutos.";
    public const string ErrorParadaInexistente = "Una de las paradas elegidas ya no existe. Actualizá la lista de paradas.";
    public const string ErrorConfiguracion = "La URL de la API no está configurada correctamente.";
    private static readonly JsonSerializerOptions Json = CrearOpciones();

    public Task<(IReadOnlyList<ParadaDto> Items, string? Error)> ListarParadasAsync() => ObtenerAsync<ParadaDto>("api/paradas");

    public Task<(IReadOnlyList<ItinerarioDto> Items, string? Error)> BuscarAsync(string origenId, string destinoId, DateOnly fecha) =>
        ObtenerAsync<ItinerarioDto>("api/itinerarios?origenId=" + Uri.EscapeDataString(origenId) +
            "&destinoId=" + Uri.EscapeDataString(destinoId) + "&fecha=" + fecha.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

    private async Task<(IReadOnlyList<T> Items, string? Error)> ObtenerAsync<T>(string ruta)
    {
        if (!SesionCliente.TryBaseUrl(configuration["Api:BaseUrl"], out var baseUrl)) return ([], ErrorConfiguracion);
        try
        {
            using var client = factory.CreateClient("Autenticacion");
            using var response = await client.GetAsync(new Uri(baseUrl!, ruta));
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return (JsonSerializer.Deserialize<List<T>>(body, Json) ?? [], null);
            return ([], response.StatusCode switch
            {
                HttpStatusCode.BadRequest => string.IsNullOrWhiteSpace(body) || body.Length > 200 ? "Revisá los datos de la búsqueda." : body.Trim('"'),
                HttpStatusCode.NotFound => ErrorParadaInexistente,
                _ => ErrorServidor
            });
        }
        catch (HttpRequestException) { return ([], ErrorConexion); }
        catch (TaskCanceledException) { return ([], ErrorConexion); }
        catch (JsonException) { return ([], ErrorServidor); }
    }

    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
