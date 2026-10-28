using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;

namespace API_Berruti.Repositories.Authentication;

public interface ICuentasRepository
{
    Task<Rol> CrearRolAsync(RolIngreso nombre);
    Task<Usuario> CrearUsuarioAsync(string correo, string contrasena, string rolId);
    Task<Usuario?> BuscarPorCorreoAsync(string correo);
    Task<Usuario?> BuscarPorIdAsync(string usuarioId);
    Task<string[]> ConsultarRolesAsync(string usuarioId);
}
