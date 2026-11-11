using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Catalogo;
using Web_App.Services.Authentication;

namespace Web_App.Services.Catalogo;

// Cliente de la consulta pública del catálogo de horarios: no envía token ni depende de la sesión.
public class CatalogoCliente(IHttpClientFactory factory, IConfiguration configuration)
{
    public const string ErrorConexion = "No se pudo conectar con la API. Revisá tu conexión e intentá de nuevo.";
    public const string ErrorServidor = "La API no pudo devolver los horarios. Intentá de nuevo en unos minutos.";
    public const string ErrorConfiguracion = "La URL de la API no está configurada correctamente.";
    private static readonly JsonSerializerOptions Json = CrearOpciones();

    public Task<(CatalogoDto? Item, string? Error)> ObtenerAsync() => ObtenerAsync<CatalogoDto>("api/catalogo");

    public Task<(SalidasDelDiaDto? Item, string? Error)> SalidasDelDiaAsync(DateOnly fecha) =>
        ObtenerAsync<SalidasDelDiaDto>("api/catalogo/salidas?fecha=" + fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    private async Task<(T? Item, string? Error)> ObtenerAsync<T>(string ruta) where T : class
    {
        if (!SesionCliente.TryBaseUrl(configuration["Api:BaseUrl"], out var baseUrl)) return (null, ErrorConfiguracion);
        try
        {
            using var client = factory.CreateClient("Autenticacion");
            using var response = await client.GetAsync(new Uri(baseUrl!, ruta));
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var item = JsonSerializer.Deserialize<T>(body, Json);
                return item is null ? (null, ErrorServidor) : (item, null);
            }
            return (null, response.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(body) && body.Length <= 200
                ? body.Trim('"') : ErrorServidor);
        }
        catch (HttpRequestException) { return (null, ErrorConexion); }
        catch (TaskCanceledException) { return (null, ErrorConexion); }
        catch (JsonException) { return (null, ErrorServidor); }
    }

    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
