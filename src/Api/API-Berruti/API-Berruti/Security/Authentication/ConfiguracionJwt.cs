using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace API_Berruti.Security.Authentication;

// Parámetros JWT leídos de configuración (Jwt:Key por variable de entorno Jwt__Key; nunca versionada).
public static class ConfiguracionJwt
{
    public const int BytesMinimosClave = 32;
    public static readonly TimeSpan Vigencia = TimeSpan.FromDays(7);

    public sealed record Parametros(SymmetricSecurityKey Clave, string Emisor, string Audiencia);

    // Falla cerrado y sin incluir el valor de la clave en el mensaje.
    public static Parametros Leer(IConfiguration configuracion)
    {
        var texto = configuracion["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(texto))
            throw new InvalidOperationException("Falta la clave JWT: definí Jwt__Key (variable de entorno o .env local).");
        var bytes = Encoding.UTF8.GetBytes(texto);
        if (bytes.Length < BytesMinimosClave)
            throw new InvalidOperationException($"La clave JWT (Jwt__Key) debe tener al menos {BytesMinimosClave} bytes.");
        var emisor = configuracion["Jwt:Issuer"];
        var audiencia = configuracion["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(audiencia))
            throw new InvalidOperationException("Faltan Jwt:Issuer o Jwt:Audience en la configuración.");
        return new Parametros(new SymmetricSecurityKey(bytes), emisor, audiencia);
    }
}
