using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web_App.Models.Authentication;

namespace Web_App.Services.Authentication;

public class SesionCliente(IHttpClientFactory factory, IConfiguration configuration, TimeProvider clock) : ISesionCliente
{
    private string? _token;
    private DateTimeOffset _vence;
    private int _ocupada;
    private long _version;
    private static readonly JsonSerializerOptions Json = CrearOpciones();
    private static readonly string[] MensajesValidacion =
    ["El correo es obligatorio.", "El correo no es válido.", "La contraseña es obligatoria."];

    public EstadoSesion Estado => new(_token != null && _vence > clock.GetUtcNow(), _ocupada != 0);
    public void Descartar()
    {
        Interlocked.Increment(ref _version);
        _token = null;
        _vence = default;
    }

    public Task<ResultadoAutenticacion> IngresarAsync(SolicitudIngresoDto solicitud) =>
        EjecutarAsync("ingreso", solicitud);
    public Task<ResultadoAutenticacion> SalirAsync() => EjecutarAsync("salida");
    public Task<ResultadoAutenticacion> ConsultarIdentidadAsync() => EjecutarAsync("identidad");
    public Task<ResultadoAutenticacion> ProbarPasajeroAsync() => EjecutarAsync("prueba-pasajero");
    public Task<ResultadoAutenticacion> ProbarAdministradorAsync() => EjecutarAsync("prueba-administrador");

    private async Task<ResultadoAutenticacion> EjecutarAsync(string operacion, SolicitudIngresoDto? solicitud = null)
    {
        if (Interlocked.CompareExchange(ref _ocupada, 1, 0) != 0)
            return Error(null, "ocupado", "Hay una solicitud en curso.");
        long version = Interlocked.Read(ref _version);
        bool ingreso = operacion == "ingreso", salida = operacion == "salida";
        ResultadoAutenticacion Fallo(int? status, string codigo, string mensaje) => Error(status, codigo,
            salida ? "Revocación no confirmada. " + mensaje + " Puede reintentar el cierre." : mensaje);
        try
        {
            if (!TryBaseUrl(configuration["Api:BaseUrl"], out var baseUrl))
                return Fallo(null, "configuracion", "Configure Api:BaseUrl (Api__BaseUrl) con una URL HTTPS válida, sin credenciales, query ni fragmento.");
            if (ingreso)
            {
                if (Estado.Autenticada) return Error(null, "sesionExistente", "Cierre la sesión actual antes de ingresar.");
                var errores = new List<ValidationResult>();
                if (solicitud == null || !Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), errores, true))
                    return Error(null, "validacion", string.Join(" ", errores.Select(e => e.ErrorMessage).DefaultIfEmpty("Complete el formulario.")));
            }
            else if (!Estado.Autenticada)
            {
                Descartar();
                return Fallo(null, "sesionInvalida", "La sesión local no es válida o venció. Ingrese nuevamente.");
            }

            using var client = factory.CreateClient("Autenticacion");
            using var request = new HttpRequestMessage(ingreso || salida ? HttpMethod.Post : HttpMethod.Get,
                new Uri(baseUrl!, "api/autenticacion/" + operacion));
            if (ingreso) request.Content = JsonContent.Create(solicitud, options: Json);
            else request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            using var response = await client.SendAsync(request);
            int status = (int)response.StatusCode;
            if (version != Interlocked.Read(ref _version))
                return Fallo(status, "descartada", "La respuesta pertenece a una sesión descartada.");
            if (status == 401)
            {
                if (!ingreso) Descartar();
                return ingreso
                    ? Error(status, "ingresoRechazado", "No se pudo ingresar con las credenciales proporcionadas.")
                    : Fallo(status, "sesionInvalida", "La sesión no es válida o venció. Ingrese nuevamente.");
            }
            if (status == 403) return Fallo(status, "denegado", "La API denegó el acceso a esta operación.");
            if (status >= 500) return Fallo(status, "servidor", "El servicio no está disponible. Reintente más tarde.");
            if (salida && status == 204)
            {
                Descartar();
                return new(true, status, "salidaConfirmada", "Sesión cerrada en el servidor.");
            }
            if (status != 200 && status != 400)
                return Fallo(status, "respuestaInvalida", "La respuesta del servicio no es válida.");
            string body = await response.Content.ReadAsStringAsync();
            if (version != Interlocked.Read(ref _version))
                return Fallo(status, "descartada", "La respuesta pertenece a una sesión descartada.");
            try
            {
                using var document = JsonDocument.Parse(body);
                if (status == 400)
                {
                    string? mensaje = ValidacionReconocida(document.RootElement);
                    return Fallo(status, mensaje == null ? "respuestaInvalida" : "validacion",
                        mensaje ?? "La respuesta de validación no es válida.");
                }
                if (salida || !FormaSesion(document.RootElement, ingreso))
                    return Fallo(status, "respuestaInvalida", "La respuesta del servicio no es válida.");
                if (ingreso)
                {
                    var dto = document.RootElement.Deserialize<RespuestaIngresoDto>(Json)!;
                    if (!Opaco(dto.Token) || !DateTimeOffset.TryParseExact(dto.VenceEnUtc, "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var vence)
                        || vence <= clock.GetUtcNow() || vence > clock.GetUtcNow().AddDays(7))
                        return Error(status, "respuestaInvalida", "La respuesta de sesión no es válida.");
                    _token = dto.Token;
                    _vence = vence;
                }
                else
                {
                    // El DTO propio verifica el conversor real, no configuración MVC ajena.
                    _ = document.RootElement.Deserialize<RespuestaSesionDto>(Json);
                }
                return new(true, status, ingreso ? "ingresoConfirmado" : "autorizado",
                    ingreso ? "Sesión confirmada por la API." : "Operación autorizada por la API.");
            }
            catch (JsonException)
            {
                return Fallo(status, "respuestaInvalida", "La respuesta del servicio no es válida.");
            }
        }
        catch (HttpRequestException)
        {
            return Fallo(null, "red", "No se pudo contactar con la API. Revise la conexión y reintente.");
        }
        catch (OperationCanceledException)
        {
            return Fallo(null, "tiempoAgotado", "Se agotó el tiempo de espera. Reintente.");
        }
        finally { Interlocked.Exchange(ref _ocupada, 0); }
    }

    private static bool TryBaseUrl(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value) || value.Contains('?') || value.Contains('#') || value.Contains('\\')
            || !Uri.TryCreate(value, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrEmpty(parsed.Host) || parsed.UserInfo.Length != 0) return false;
        uri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/");
        return true;
    }

    private static bool FormaSesion(JsonElement root, bool ingreso)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("identificadorUsuario", out var id) || id.ValueKind != JsonValueKind.String || !Opaco(id.GetString())
            || !root.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array) return false;
        foreach (var rol in roles.EnumerateArray())
            if (rol.ValueKind != JsonValueKind.String || !Enum.GetNames<RolIngreso>().Any(
                nombre => string.Equals(nombre, rol.GetString(), StringComparison.OrdinalIgnoreCase))) return false;
        return !ingreso || (root.TryGetProperty("token", out var token) && token.ValueKind == JsonValueKind.String
            && root.TryGetProperty("venceEnUtc", out var vence) && vence.ValueKind == JsonValueKind.String);
    }

    private static bool Opaco(string? value)
    {
        if (value == null || value.Length != 43 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) return false;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
            return bytes.Length == 32 && Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') == value;
        }
        catch (FormatException) { return false; }
    }

    private static string? ValidacionReconocida(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object) return null;
        var messages = new List<string>();
        foreach (var field in errors.EnumerateObject())
        {
            if (field.Name is not ("Correo" or "Contrasena" or "correo" or "contrasena") || field.Value.ValueKind != JsonValueKind.Array) return null;
            foreach (var item in field.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || !MensajesValidacion.Contains(item.GetString())) return null;
                messages.Add(item.GetString()!);
            }
        }
        return messages.Count == 0 ? null : string.Join(" ", messages.Distinct());
    }

    private static ResultadoAutenticacion Error(int? status, string codigo, string mensaje) => new(false, status, codigo, mensaje);
    private static JsonSerializerOptions CrearOpciones()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }
}
