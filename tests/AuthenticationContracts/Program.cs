using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// Console checks: no server, credentials, database or additional test packages.
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var failures = 0;
var checks = 0;
void Check(bool condition, string name)
{
    checks++;
    if (condition) Console.WriteLine($"PASS: {name}");
    else { failures++; Console.Error.WriteLine($"FAIL: {name}"); }
}

var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
var apiProgram = File.ReadAllText(Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Program.cs"));
Check(apiProgram.Contains("new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)"),
    "API still registers the tested enum converter");
Check(!Directory.Exists(Path.Combine(root, "src/Shared")), "Shared directory removed");

var surfaces = new[]
{
    (Assembly: "API-Berruti", Namespace: "API_Berruti.DTOs.Authentication", Project: "src/Api/API-Berruti/API-Berruti/API-Berruti.csproj"),
    (Assembly: "Web-App", Namespace: "Web_App.Models.Authentication", Project: "src/Web/Web-App/Web-App/Web-App.csproj")
};
foreach (var surface in surfaces)
{
    var assembly = Assembly.Load(surface.Assembly);
    Check(!File.ReadAllText(Path.Combine(root, surface.Project)).Contains("ProjectReference"),
        $"{surface.Assembly}: no project dependency on Shared or the other application");
    Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "Berruti.Contracts"),
        $"{surface.Assembly}: no compiled Shared dependency");
    var request = assembly.GetType(surface.Namespace + ".SolicitudIngresoDto");
    var success = assembly.GetType(surface.Namespace + ".RespuestaIngresoDto");
    var rejection = assembly.GetType(surface.Namespace + ".RespuestaErrorIngresoDto");
    var roles = assembly.GetType(surface.Namespace + ".RolIngreso");
    Check(request != null && success != null && rejection != null && roles != null,
        $"{surface.Assembly}: owns all four authentication types");
    if (request == null || success == null || rejection == null || roles == null) continue;

    Check(new[] { request, success, rejection, roles }.All(t => t.Assembly == assembly),
        $"{surface.Assembly}: types belong to its own assembly");
    Check(Enum.GetNames(roles).SequenceEqual(new[] { "Pasajero", "Cobrador", "Administrador" }),
        $"{surface.Assembly}: role names preserved");

    var examples = new[]
    {
        (Type: request, Json: "{\"correo\":\"prueba@example.invalid\",\"contrasena\":\"x\"}"),
        (Type: success, Json: "{\"identificadorUsuario\":\"opaco-de-prueba\",\"roles\":[\"pasajero\",\"cobrador\",\"administrador\"]}"),
        (Type: rejection, Json: "{\"codigo\":\"ingresoRechazado\",\"mensaje\":\"No se pudo ingresar con las credenciales proporcionadas.\"}")
    };
    foreach (var example in examples)
    {
        var instance = JsonSerializer.Deserialize(example.Json, example.Type, options)!;
        var actual = JsonSerializer.Serialize(instance, example.Type, options);
        // JsonElement compares decoded values rather than escaping accents differently.
        var expectedFields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(example.Json)!;
        var actualFields = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(actual)!;
        Check(expectedFields.Count == actualFields.Count && expectedFields.All(pair =>
            actualFields.TryGetValue(pair.Key, out var value) && JsonElement.DeepEquals(pair.Value, value)),
            $"{surface.Assembly}: exact JSON fields, types and roundtrip for {example.Type.Name}");
    }
    Check(JsonSerializer.Serialize(Activator.CreateInstance(success), success, options) ==
        "{\"identificadorUsuario\":\"\",\"roles\":[]}", $"{surface.Assembly}: default success preserved");
    foreach (var invalid in new[] { "[0]", "[999]", "[\"0\"]", "[\"desconocido\"]" })
    {
        var rejected = false;
        try { JsonSerializer.Deserialize("{\"roles\":" + invalid + "}", success, options); }
        catch (JsonException) { rejected = true; }
        Check(rejected, $"{surface.Assembly}: rejects roles {invalid}");
    }
    var invalidEnumRejected = false;
    try { JsonSerializer.Serialize(Enum.ToObject(roles, 999), roles, options); }
    catch (JsonException) { invalidEnumRejected = true; }
    Check(invalidEnumRejected, $"{surface.Assembly}: rejects undefined output role");
    Check(JsonSerializer.Serialize(JsonSerializer.Deserialize("{\"roles\":[]}", success, options), success, options) ==
        "{\"identificadorUsuario\":\"\",\"roles\":[]}", $"{surface.Assembly}: empty roles roundtrip");

    var cases = new (string? Email, string? Password, string? Error)[]
    {
        (null, "x", "El correo es obligatorio."),
        ("", "x", "El correo es obligatorio."),
        ("   ", "x", "El correo es obligatorio."),
        ("no-es-correo", "x", "El correo no es válido."),
        ("prueba@example.invalid", null, "La contraseña es obligatoria."),
        ("prueba@example.invalid", "", "La contraseña es obligatoria."),
        ("prueba@example.invalid", "   ", "La contraseña es obligatoria."),
        ("prueba@example.invalid", "x", null)
    };
    foreach (var item in cases)
    {
        var instance = Activator.CreateInstance(request)!;
        request.GetProperty("Correo")!.SetValue(instance, item.Email);
        request.GetProperty("Contrasena")!.SetValue(instance, item.Password);
        var errors = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(instance, new ValidationContext(instance), errors, true);
        Check(item.Error == null ? valid && errors.Count == 0 :
            !valid && errors.Count == 1 && errors[0].ErrorMessage == item.Error,
            $"{surface.Assembly}: validation case {Array.IndexOf(cases, item) + 1}");
    }
    var blank = Activator.CreateInstance(request)!;
    var blankErrors = new List<ValidationResult>();
    Check(!Validator.TryValidateObject(blank, new ValidationContext(blank), blankErrors, true) &&
        blankErrors.Select(e => e.ErrorMessage).OrderBy(e => e).SequenceEqual(new[]
        { "El correo es obligatorio.", "La contraseña es obligatoria." }),
        $"{surface.Assembly}: defaults preserve both required messages");
}
Console.WriteLine($"{checks - failures}/{checks} checks passed");
return failures == 0 ? 0 : 1;
