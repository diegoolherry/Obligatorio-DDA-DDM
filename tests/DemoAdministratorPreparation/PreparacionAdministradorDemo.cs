using System.Collections.Immutable;

namespace DemoAdministratorPreparation;

public sealed record Destino(string Host, int Puerto, string Base, string Usuario);
public sealed record Rol(string Id, string Nombre);
public sealed record Usuario(string Id, string Correo, bool Activo = true);
public sealed record Membresia(string UsuarioId, string RolId);
public sealed record Inventario(Destino Destino, int? US, int? RO, int MaxUS, int MaxRO,
    ImmutableArray<Rol> Roles, ImmutableArray<Usuario> Usuarios, ImmutableArray<Membresia> Membresias);
public sealed record Resultado(string Codigo, string Mensaje);
// Puerto local reducido: mismas operaciones de creación que ICuentasRepository, sin entidades secretas.
public interface IContextoSintetico
{
    Inventario Consultar();
    Task<Rol> CrearRolAsync(string nombre);
    Task<Usuario> CrearUsuarioAsync(string correo, string contrasena, string rolId);
}
public static class PreparacionAdministradorDemo
{
    public const string Correo = "admin.demo@example.test";
    public const string Administrador = "Administrador";
    public static async Task<Resultado> PrepararAsync(IContextoSintetico contexto, string correo, string contrasena)
    {
        try
        {
            var antes = contexto.Consultar();
            if (correo != Correo || string.IsNullOrWhiteSpace(contrasena) || contrasena.Length > 255 || !Valido(antes))
                return new("rechazado", "Precondiciones rechazadas; sin intentos.");
            var candidatos = antes.Roles.Where(r => r.Nombre == Administrador).ToArray();
            var esperado = antes;
            Rol rol;
            if (candidatos.Length == 0)
            {
                rol = new Rol($"RO-{antes.RO + 1}", Administrador);
                esperado = antes with { RO = antes.RO + 1, MaxRO = antes.RO!.Value + 1, Roles = antes.Roles.Add(rol) };
                if (await contexto.CrearRolAsync(Administrador) != rol || !Mismo(esperado, contexto.Consultar()))
                    return Revision();
            }
            else rol = candidatos[0];
            var usuario = new Usuario($"US-{antes.US + 1}", Correo);
            esperado = esperado with { US = antes.US + 1, MaxUS = antes.US!.Value + 1,
                Usuarios = antes.Usuarios.Add(usuario), Membresias = antes.Membresias.Add(new(usuario.Id, rol.Id)) };
            if (await contexto.CrearUsuarioAsync(Correo, contrasena, rol.Id) != usuario || !Mismo(esperado, contexto.Consultar()))
                return Revision();
            return new("preparado", "Preparacion sintetica verificada; no autoriza alta real.");
        }
        catch { return Revision(); } // Nunca propagar mensajes potencialmente sensibles; ninguna compensación/reintento.
    }
    private static Resultado Revision() => new("revision", "Revision requerida; resultado parcial o incierto. No reintentar.");
    private static bool Valido(Inventario i)
    {
        var d = i.Destino;
        if (!(d.Host is "localhost" or "127.0.0.1" or "::1") || d.Puerto != 3306 || d.Base != "berrutilocal" || d.Usuario != "berruti_app"
            || i.US is null or < 0 or >= 50000 || i.RO is null or < 0 or > 50000
            || i.MaxUS < 0 || i.MaxRO < 0 || i.MaxUS > i.US || i.MaxRO > i.RO
            || i.Usuarios.Any(u => string.Equals(u.Correo, Correo, StringComparison.OrdinalIgnoreCase))) return false;
        if (i.Roles.Any(r => Numero(r.Id,"RO-") == 0) || i.Usuarios.Any(u => Numero(u.Id,"US-") == 0)
            || i.Roles.Select(r => r.Id).Distinct().Count() != i.Roles.Length
            || i.Usuarios.Select(u => u.Id).Distinct().Count() != i.Usuarios.Length
            || i.MaxRO != i.Roles.Select(r => Numero(r.Id,"RO-")).DefaultIfEmpty(0).Max()
            || i.MaxUS != i.Usuarios.Select(u => Numero(u.Id,"US-")).DefaultIfEmpty(0).Max()) return false;
        var roles = i.Roles.Where(r => string.Equals(r.Nombre, Administrador, StringComparison.OrdinalIgnoreCase)).ToArray();
        return roles.Length == 0 ? i.RO < 50000 : roles.Length == 1 && roles[0].Nombre == Administrador
            && !i.Membresias.Any(m => m.RolId == roles[0].Id);
    }
    private static int Numero(string id, string prefijo)
    {
        if (!id.StartsWith(prefijo, StringComparison.Ordinal) || !int.TryParse(id.AsSpan(prefijo.Length), out var n)
            || n < 1 || n > 50000 || id != prefijo + n.ToString(System.Globalization.CultureInfo.InvariantCulture)) return 0;
        return n;
    }
    private static bool Mismo(Inventario a, Inventario b) => a.Destino == b.Destino && a.US == b.US && a.RO == b.RO
        && a.MaxUS == b.MaxUS && a.MaxRO == b.MaxRO && a.Roles.SequenceEqual(b.Roles)
        && a.Usuarios.SequenceEqual(b.Usuarios) && a.Membresias.SequenceEqual(b.Membresias);
}
