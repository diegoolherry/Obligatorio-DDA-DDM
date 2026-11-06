using System.Collections.Immutable;
using DemoAdministratorPreparation;

const string uso = "Uso: DemoAdministratorPreparation --unit | --help\n";
if (args.Length == 1 && args[0] == "--help") { Console.Write(uso); return 0; }
if (args.Length != 1 || args[0] != "--unit") { Console.Error.Write(uso); return 1; }
var casos = new List<(string Nombre, Action<Falso> Cambiar, string Codigo)>();
void Caso(string nombre, Action<Falso> cambiar, string codigo = "rechazado") => casos.Add((nombre, cambiar, codigo));
Caso("reutiliza", _ => { }, "preparado");
Caso("crea rol", f => f.Estado = f.Estado with { MaxRO = 1, Roles = [new("RO-1", "Pasajero")] }, "preparado");
Caso("RO agotado reutilizable", f => f.Estado = f.Estado with { RO = 50000 }, "preparado");
Caso("limite US", f => f.Estado = f.Estado with { US = 49999 }, "preparado");
Caso("limite RO", f => f.Estado = f.Estado with { RO = 49999, MaxRO = 1, Roles = [new("RO-1", "Pasajero")] }, "preparado");
foreach (var destino in new[] { new Destino("remoto",3306,"berrutilocal","berruti_app"),
    new Destino("localhost",3307,"berrutilocal","berruti_app"), new Destino("localhost",3306,"otra","berruti_app"),
    new Destino("localhost",3306,"berrutilocal","root") })
    Caso("destino " + destino, f => f.Estado = f.Estado with { Destino = destino });
Caso("loopback IPv4", f => f.Estado = f.Estado with { Destino = f.Estado.Destino with { Host = "127.0.0.1" } }, "preparado");
Caso("loopback IPv6", f => f.Estado = f.Estado with { Destino = f.Estado.Destino with { Host = "::1" } }, "preparado");
Caso("correo incorrecto", f => f.Correo = "otro@example.test");
Caso("duplicado", f => f.Estado = f.Estado with { Usuarios = f.Estado.Usuarios.Add(new("US-2", "ADMIN.DEMO@EXAMPLE.TEST")), US = 2, MaxUS = 2 });
foreach (var clave in new[] { "", " ", new string('x',256) }) Caso("clave invalida " + clave.Length, f => f.Clave = clave);
Caso("clave maxima", f => f.Clave = new string('x',255), "preparado");
foreach (int? valor in new int?[] { null, -1, 50001, 0 })
{
    Caso("US invalido " + valor, f => f.Estado = f.Estado with { US = valor });
    Caso("RO invalido " + valor, f => f.Estado = f.Estado with { RO = valor });
}
Caso("US agotado", f => f.Estado = f.Estado with { US = 50000 });
Caso("RO agotado nuevo", f => f.Estado = f.Estado with { RO = 50000, MaxRO = 1, Roles = [new("RO-1", "Pasajero")] });
Caso("max US invalido", f => f.Estado = f.Estado with { MaxUS = -1 });
Caso("max RO invalido", f => f.Estado = f.Estado with { MaxRO = 50001 });
Caso("US atrasado", f => f.Estado = f.Estado with { MaxUS = 3 });
Caso("RO atrasado", f => f.Estado = f.Estado with { MaxRO = 3 });
foreach (var id in new[] { "US-2", "RO-0", "RO-50001", "RO-02", "RO-x" })
    Caso("rol invalido " + id, f => f.Estado = f.Estado with { Roles = [new("RO-1","Pasajero"),new(id,"Administrador")] });
Caso("rol ambiguo", f => f.Estado = f.Estado with { Roles = f.Estado.Roles.Add(new("RO-3","Administrador")), RO = 3, MaxRO = 3 });
Caso("id rol compartido", f => f.Estado = f.Estado with { Roles = f.Estado.Roles.Add(new("RO-2","Cobrador")) });
Caso("rol utilizado", f => f.Estado = f.Estado with { Membresias = f.Estado.Membresias.Add(new("US-1","RO-2")) });
Caso("contadores cero", f => f.Estado = f.Estado with { US = 0, RO = 0, MaxUS = 0, MaxRO = 0, Roles = [], Usuarios = [], Membresias = [] }, "preparado");
foreach (var modo in new[] { "rol falla", "rol confirma y falla", "usuario falla", "usuario confirma y falla", "rol retorno", "usuario retorno",
    "contador US", "contador RO", "membresia", "pasajero", "rol post", "correo post", "activo post", "max post", "consulta falla" })
    Caso(modo, f => { f.Modo = modo; if (modo.StartsWith("rol ") || modo == "usuario falla")
        f.Estado = f.Estado with { MaxRO = 1, Roles = [new("RO-1","Pasajero")] }; }, "revision");
int aprobados = 0;
foreach (var caso in casos)
{
    try
    {
        var f = new Falso(); caso.Cambiar(f); var antes = f.Consultar();
        var nuevoRol = !antes.Roles.Any(r => r.Nombre == "Administrador");
        var resultado = await PreparacionAdministradorDemo.PrepararAsync(f, f.Correo, f.Clave);
        Igual(resultado, new Resultado(caso.Codigo, caso.Codigo switch {
            "preparado" => "Preparacion sintetica verificada; no autoriza alta real.",
            "revision" => "Revision requerida; resultado parcial o incierto. No reintentar.",
            _ => "Precondiciones rechazadas; sin intentos." }));
        var despues = f.Consultar();
        if (caso.Codigo == "rechazado") { Igual(f.RolesLlamadas,0); Igual(f.UsuariosLlamadas,0); Exigir(Mismo(antes,despues)); }
        else
        {
            Igual(f.RolesLlamadas,nuevoRol ? 1 : 0);
            Igual(f.UsuariosLlamadas, f.Modo is "rol falla" or "rol confirma y falla" or "rol retorno" ? 0 : 1);
            // Cada efecto público se compara con la transición sintética esperada completa, sin introspección.
            Exigir(Mismo(f.Esperado(antes),despues));
            if (caso.Codigo == "preparado")
            {
                var llamadas = (f.RolesLlamadas,f.UsuariosLlamadas);
                Igual(await PreparacionAdministradorDemo.PrepararAsync(f,f.Correo,f.Clave),
                    new Resultado("rechazado","Precondiciones rechazadas; sin intentos."));
                Igual(llamadas,(f.RolesLlamadas,f.UsuariosLlamadas)); Exigir(Mismo(despues,f.Consultar()));
            }
        }
        aprobados++;
    }
    catch { Console.WriteLine("FAIL: " + caso.Nombre); }
}
Console.WriteLine($"Sintetico: {aprobados}/{casos.Count} PASS");
return aprobados == casos.Count ? 0 : 1;
static void Exigir(bool condicion) { if (!condicion) throw new Exception("assert"); }
static void Igual<T>(T actual,T esperado) => Exigir(EqualityComparer<T>.Default.Equals(actual,esperado));
static bool Mismo(Inventario a,Inventario b) => a.Destino == b.Destino && a.US == b.US && a.RO == b.RO && a.MaxUS == b.MaxUS && a.MaxRO == b.MaxRO
    && a.Roles.SequenceEqual(b.Roles) && a.Usuarios.SequenceEqual(b.Usuarios) && a.Membresias.SequenceEqual(b.Membresias);

sealed class Falso : IContextoSintetico
{
    public Inventario Estado { get; set; } = new(new("localhost",3306,"berrutilocal","berruti_app"),1,2,1,2,
        [new("RO-1","Pasajero"),new("RO-2","Administrador")],[new("US-1","pasajero@example.test")],[new("US-1","RO-1")]);
    public string Correo { get; set; } = PreparacionAdministradorDemo.Correo;
    public string Clave { get; set; } = "MARCADOR-PRIVADO";
    public string Modo { get; set; } = "";
    public int RolesLlamadas { get; private set; }
    public int UsuariosLlamadas { get; private set; }
    public Inventario Consultar()
    {
        if (Modo == "consulta falla" && UsuariosLlamadas > 0 && !_falloConsulta) { _falloConsulta = true; throw new Exception("MARCADOR-EXCEPCION"); }
        return Estado with { Roles = Estado.Roles.ToImmutableArray(), Usuarios = Estado.Usuarios.ToImmutableArray(), Membresias = Estado.Membresias.ToImmutableArray() };
    }
    private bool _falloConsulta;
    public Task<Rol> CrearRolAsync(string nombre)
    {
        RolesLlamadas++; if (nombre != "Administrador") throw new Exception("contrato");
        if (Modo == "rol falla") throw new Exception("MARCADOR-EXCEPCION");
        var rol = new Rol($"RO-{Estado.RO + 1}",nombre);
        Estado = Estado with { RO = Estado.RO + 1, MaxRO = Estado.RO!.Value + 1, Roles = Estado.Roles.Add(rol) };
        if (Modo == "rol confirma y falla") throw new Exception("MARCADOR-EXCEPCION");
        return Task.FromResult(Modo == "rol retorno" ? rol with { Nombre = "Cobrador" } : rol);
    }
    public Task<Usuario> CrearUsuarioAsync(string correo,string contrasena,string rolId)
    {
        UsuariosLlamadas++;
        if (correo != Correo || contrasena != Clave || !Estado.Roles.Any(r => r.Id == rolId && r.Nombre == "Administrador")) throw new Exception("contrato");
        if (Modo == "usuario falla") throw new Exception("MARCADOR-EXCEPCION");
        Estado = TransicionUsuario(Estado,rolId);
        if (Modo == "usuario confirma y falla") throw new Exception("MARCADOR-EXCEPCION");
        var usuario = Estado.Usuarios.Last();
        return Task.FromResult(Modo == "usuario retorno" ? usuario with { Id = "US-50000" } : usuario);
    }
    public Inventario Esperado(Inventario antes)
    {
        if (RolesLlamadas > 0 && Modo != "rol falla") antes = antes with { RO = antes.RO + 1, MaxRO = antes.RO!.Value + 1,
            Roles = antes.Roles.Add(new($"RO-{antes.RO + 1}","Administrador")) };
        return UsuariosLlamadas > 0 && Modo != "usuario falla" ? TransicionUsuario(antes,antes.Roles.Single(r => r.Nombre == "Administrador").Id) : antes;
    }
    private Inventario TransicionUsuario(Inventario s,string rolId)
    {
        var usuario = new Usuario($"US-{s.US + 1}",Correo);
        var t = s with { US = s.US + 1, MaxUS = s.US!.Value + 1, Usuarios = s.Usuarios.Add(usuario), Membresias = s.Membresias.Add(new(usuario.Id,rolId)) };
        return Modo switch {
            "contador US" => t with { US = s.US }, "contador RO" => t with { RO = s.RO + 1 },
            "membresia" => t with { Membresias = t.Membresias.Add(new(usuario.Id,rolId)) },
            "pasajero" => t with { Usuarios = t.Usuarios.SetItem(0,t.Usuarios[0] with { Activo = false }) },
            "rol post" => t with { Roles = [] }, "correo post" => t with { Usuarios = t.Usuarios.SetItem(t.Usuarios.Length-1,usuario with { Correo = "otro@example.test" }) },
            "activo post" => t with { Usuarios = t.Usuarios.SetItem(t.Usuarios.Length-1,usuario with { Activo = false }) },
            "max post" => t with { MaxUS = 0 }, _ => t };
    }
}
