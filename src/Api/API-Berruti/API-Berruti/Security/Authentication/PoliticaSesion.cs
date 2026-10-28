using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API_Berruti.DTOs.Authentication;

namespace API_Berruti.Security.Authentication;

public static class PoliticaSesion
{
    public static string NuevoToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool EsToken(string token)
    {
        if (token.Length != 43 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) return false;
        var bytes = Convert.FromBase64String(token.Replace('-', '+').Replace('_', '/') + "=");
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') == token;
    }

    public static string Huella(string token) => Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));

    // DATETIME(6) stores microseconds, not .NET's 100 ns ticks. Truncate before persisting/returning.
    public static DateTime AhoraUtc(TimeProvider reloj)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return new DateTime(ahora.Ticks - ahora.Ticks % 10, DateTimeKind.Utc);
    }

    public static bool EsVigente(DateTime vence, DateTime? revocada, bool activo, DateTime ahora) =>
        activo && revocada == null && ahora < vence;

    public static RolIngreso[] Roles(IEnumerable<string> nombres) => nombres.Select(nombre =>
    {
        if (!Enum.TryParse<RolIngreso>(nombre, out var rol) || !Enum.IsDefined(rol) || rol.ToString() != nombre)
            throw new InvalidOperationException("Rol de servidor inválido.");
        return rol;
    }).Distinct().ToArray();

    public static ClaimsPrincipal Identidad(string publico, IEnumerable<string> roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, publico) };
        claims.AddRange(Roles(roles).Select(r => new Claim(ClaimTypes.Role, r.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}
