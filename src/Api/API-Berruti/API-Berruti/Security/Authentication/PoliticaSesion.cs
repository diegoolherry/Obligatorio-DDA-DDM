using System.Security.Cryptography;
using API_Berruti.DTOs.Authentication;

namespace API_Berruti.Security.Authentication;

public static class PoliticaSesion
{
    // Valor aleatorio de 32 bytes en Base64url: identificador público del usuario (claim `sub`), no un secreto.
    public static string NuevoToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // DATETIME(6) stores microseconds, not .NET's 100 ns ticks. Truncate before persisting/returning.
    public static DateTime AhoraUtc(TimeProvider reloj)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        return new DateTime(ahora.Ticks - ahora.Ticks % 10, DateTimeKind.Utc);
    }

    public static RolIngreso[] Roles(IEnumerable<string> nombres) => nombres.Select(nombre =>
    {
        if (!Enum.TryParse<RolIngreso>(nombre, out var rol) || !Enum.IsDefined(rol) || rol.ToString() != nombre)
            throw new InvalidOperationException("Rol de servidor inválido.");
        return rol;
    }).Distinct().ToArray();
}
