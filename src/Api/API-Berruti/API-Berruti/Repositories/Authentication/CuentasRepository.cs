using System.ComponentModel.DataAnnotations;
using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Repositories.Authentication;

public class CuentasRepository : ICuentasRepository
{
    private readonly BerrutiDbContext _contexto;
    private readonly IContadoresRepository _contadores;
    public CuentasRepository(BerrutiDbContext contexto, IContadoresRepository contadores)
    {
        _contexto = contexto;
        _contadores = contadores;
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
        var usuario = new Usuario { Correo = correo, Contrasena = contrasena,
            IdentificadorPublico = API_Berruti.Security.Authentication.PoliticaSesion.NuevoToken() };
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

    // Devuelve entidades solo a servicios internos; no serializar ni registrar contraseñas.
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
