using System.ComponentModel.DataAnnotations;
using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Authentication;

public class CuentasRepository : ICuentasRepository
{
    private readonly BerrutiDbContext _contexto;
    private readonly IContadoresRepository _contadores;
    private readonly IPasswordHasher<Usuario> _hasher;
    public CuentasRepository(BerrutiDbContext contexto, IContadoresRepository contadores, IPasswordHasher<Usuario> hasher)
    {
        _contexto = contexto;
        _contadores = contadores;
        _hasher = hasher;
    }

    public async Task<Rol> CrearRolAsync(RolIngreso nombre)
    {
        if (!Enum.IsDefined(nombre)) throw new ArgumentException("Rol desconocido.", nameof(nombre));
        if (_contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("El contexto debe estar libre de cambios pendientes.");
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();
        var rol = new Rol { Nombre = nombre.ToString() };
        try
        {
            rol.Identificador = await _contadores.IncrementarAsync("RO-");
            _contexto.Roles.Add(rol);
            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return rol;
        }
        catch
        {
            await transaccion.RollbackAsync();
            _contexto.Entry(rol).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<Usuario> CrearUsuarioAsync(string correo, string contrasena, string rolId)
    {
        if (string.IsNullOrWhiteSpace(correo) || correo.Length > 254 || !new EmailAddressAttribute().IsValid(correo))
            throw new ArgumentException("El correo no es válido.", nameof(correo));
        if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length > 255)
            throw new ArgumentException("La contraseña es obligatoria y admite hasta 255 caracteres.", nameof(contrasena));
        if (string.IsNullOrWhiteSpace(rolId)) throw new ArgumentException("El rol es obligatorio.", nameof(rolId));
        if (_contexto.ChangeTracker.HasChanges())
            throw new InvalidOperationException("El contexto debe estar libre de cambios pendientes.");
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();
        var usuario = new Usuario { Correo = correo,
            IdentificadorPublico = API_Berruti.Security.Authentication.PoliticaSesion.NuevoToken() };
        // Único punto de escritura de la contraseña: se recibe en texto plano y solo se guarda el hash con sal.
        usuario.Contrasena = _hasher.HashPassword(usuario, contrasena);
        var membresia = new UsuarioRol { RolId = rolId };
        try
        {
            usuario.Identificador = await _contadores.IncrementarAsync("US-");
            membresia.UsuarioId = usuario.Identificador;
            _contexto.Usuarios.Add(usuario);
            _contexto.UsuariosRoles.Add(membresia);
            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
            return usuario;
        }
        catch
        {
            await transaccion.RollbackAsync();
            _contexto.Entry(membresia).State = EntityState.Detached;
            _contexto.Entry(usuario).State = EntityState.Detached;
            throw;
        }
    }

    // Devuelve entidades solo a servicios internos; no serializar ni registrar el hash de la contraseña.
    public Task<Usuario?> BuscarPorCorreoAsync(string correo) =>
        _contexto.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Correo == correo);

    public Task<Usuario?> BuscarPorIdAsync(string usuarioId) =>
        _contexto.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Identificador == usuarioId);

    public Task<string[]> ConsultarRolesAsync(string usuarioId) =>
        (from membresia in _contexto.UsuariosRoles.AsNoTracking()
         join rol in _contexto.Roles.AsNoTracking() on membresia.RolId equals rol.Identificador
         where membresia.UsuarioId == usuarioId
         select rol.Nombre).ToArrayAsync();
}
