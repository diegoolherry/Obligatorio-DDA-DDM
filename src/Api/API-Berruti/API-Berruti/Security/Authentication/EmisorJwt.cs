using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API_Berruti.DTOs.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace API_Berruti.Security.Authentication;

// Crea el JWT del ingreso. La configuración se lee al emitir para que construir DI no dependa de ella.
public class EmisorJwt
{
    private readonly IConfiguration _configuracion;
    public EmisorJwt(IConfiguration configuracion) { _configuracion = configuracion; }

    // El vencimiento se trunca a segundos enteros para que `exp` y venceEnUtc coincidan exactamente.
    public (string Token, DateTime Vence) Emitir(string identificadorPublico, IEnumerable<RolIngreso> roles, DateTime ahoraUtc)
    {
        var parametros = ConfiguracionJwt.Leer(_configuracion);
        var vence = ahoraUtc.Add(ConfiguracionJwt.Vigencia);
        vence = new DateTime(vence.Ticks - vence.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        // Sin correo ni datos personales: el token solo es legible, no cifrado.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, identificadorPublico),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol.ToString())));
        var credenciales = new SigningCredentials(parametros.Clave, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(parametros.Emisor, parametros.Audiencia, claims, null, vence, credenciales);
        return (new JwtSecurityTokenHandler().WriteToken(token), vence);
    }
}
