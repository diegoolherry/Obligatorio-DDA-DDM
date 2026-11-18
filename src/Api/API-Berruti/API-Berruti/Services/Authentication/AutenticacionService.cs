using System.Globalization;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Security.Authentication;
using Microsoft.AspNetCore.Identity;

namespace API_Berruti.Services.Authentication;

public class AutenticacionService : IAutenticacionService
{
    private readonly ICuentasRepository _cuentas;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly EmisorJwt _emisor;
    private readonly TimeProvider _reloj;

    public AutenticacionService(ICuentasRepository cuentas, IPasswordHasher<Usuario> hasher,
        EmisorJwt emisor, TimeProvider reloj)
    {
        _cuentas = cuentas;
        _hasher = hasher;
        _emisor = emisor;
        _reloj = reloj;
    }

    public async Task<RespuestaIngresoDto?> IngresarAsync(SolicitudIngresoDto solicitud)
    {
        var usuario = await _cuentas.BuscarPorCorreoAsync(solicitud.Correo);
        // Correo exacto (sin recortar ni normalizar mayúsculas). Usuario inexistente, inactivo o con
        // contraseña incorrecta producen el mismo rechazo. SuccessRehashNeeded también es éxito.
        if (usuario == null || !usuario.Activo || usuario.Correo != solicitud.Correo ||
            !ContrasenaValida(usuario, solicitud.Contrasena))
            return null;
        if (string.IsNullOrWhiteSpace(usuario.IdentificadorPublico)) throw new InvalidOperationException();
        // El rol sale de la base de datos del servidor, nunca de la solicitud.
        var roles = PoliticaSesion.Roles(await _cuentas.ConsultarRolesAsync(usuario.Identificador));
        var (token, vence) = _emisor.Emitir(usuario.IdentificadorPublico, roles, PoliticaSesion.AhoraUtc(_reloj));
        return new RespuestaIngresoDto { IdentificadorUsuario = usuario.IdentificadorPublico, Roles = roles,
            Token = token, VenceEnUtc = vence.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture) };
    }

    private bool ContrasenaValida(Usuario usuario, string contrasena)
    {
        try { return _hasher.VerifyHashedPassword(usuario, usuario.Contrasena, contrasena) != PasswordVerificationResult.Failed; }
        // Un valor guardado que no es un hash (p. ej. una contraseña legible anterior) no autentica nunca.
        catch (FormatException) { return false; }
    }
}
