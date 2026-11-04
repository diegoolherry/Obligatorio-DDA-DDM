using System.Globalization;
using System.Security.Claims;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Security.Authentication;

namespace API_Berruti.Services.Authentication;

public class AutenticacionService : IAutenticacionService
{
    private readonly ICuentasRepository _cuentas;
    private readonly ISesionesRepository _sesiones;
    private readonly TimeProvider _reloj;

    public AutenticacionService(ICuentasRepository cuentas, ISesionesRepository sesiones, TimeProvider reloj)
    {
        _cuentas = cuentas;
        _sesiones = sesiones;
        _reloj = reloj;
    }

    public async Task<RespuestaIngresoDto?> IngresarAsync(SolicitudIngresoDto solicitud)
    {
        var usuario = await _cuentas.BuscarPorCorreoAsync(solicitud.Correo);
        // Exact application-level comparison: no trimming, case folding or password policy.
        if (usuario == null || !usuario.Activo || usuario.Correo != solicitud.Correo || usuario.Contrasena != solicitud.Contrasena)
            return null;
        if (string.IsNullOrWhiteSpace(usuario.IdentificadorPublico)) throw new InvalidOperationException();
        var roles = PoliticaSesion.Roles(await _cuentas.ConsultarRolesAsync(usuario.Identificador));
        var token = PoliticaSesion.NuevoToken();
        var ahora = PoliticaSesion.AhoraUtc(_reloj);
        var vence = ahora.AddDays(7);
        await _sesiones.CrearAsync(new Sesion { Huella = PoliticaSesion.Huella(token),
            UsuarioId = usuario.Identificador, CreadaUtc = ahora, VenceUtc = vence });
        return new RespuestaIngresoDto { IdentificadorUsuario = usuario.IdentificadorPublico, Roles = roles,
            Token = token, VenceEnUtc = vence.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture) };
    }

    public async Task<ClaimsPrincipal?> AutenticarAsync(string token)
    {
        if (!PoliticaSesion.EsToken(token)) return null;
        var sesion = await _sesiones.BuscarAsync(PoliticaSesion.Huella(token));
        if (sesion == null) return null;
        var usuario = await _cuentas.BuscarPorIdAsync(sesion.UsuarioId);
        if (usuario == null || !PoliticaSesion.EsVigente(sesion.VenceUtc, sesion.RevocadaUtc,
            usuario.Activo, _reloj.GetUtcNow().UtcDateTime)) return null;
        if (string.IsNullOrWhiteSpace(usuario.IdentificadorPublico)) throw new InvalidOperationException();
        // Reload memberships on every request; never accept role or identity values from the client.
        return PoliticaSesion.Identidad(usuario.IdentificadorPublico,
            await _cuentas.ConsultarRolesAsync(usuario.Identificador));
    }

    public async Task SalirAsync(string token)
    {
        if (!PoliticaSesion.EsToken(token)) throw new InvalidOperationException();
        await _sesiones.RevocarAsync(PoliticaSesion.Huella(token), PoliticaSesion.AhoraUtc(_reloj));
    }
}
