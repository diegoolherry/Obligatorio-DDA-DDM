using System.Text.Json;
using System.Text.Json.Serialization;
using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Identifiers;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

ICorrelativo correlativo = new Correlativo();
ICuentasRepository Cuentas(BerrutiDbContext contexto) =>
    new CuentasRepository(contexto, new ContadoresRepository(contexto, correlativo));
var fallos = 0;
void Comprobar(bool condicion, string nombre)
{
    if (condicion) Console.WriteLine($"PASS: {nombre}");
    else { fallos++; Console.Error.WriteLine($"FAIL: {nombre}"); }
}
void Rechaza<T>(Action accion, string nombre) where T : Exception
{
    try { accion(); Comprobar(false, nombre); }
    catch (T) { Comprobar(true, nombre); }
}
if (args.SequenceEqual(new[] { "--unit" }))
{
    Comprobar(correlativo.Siguiente("US-", 0) == "US-1", "inicio inclusivo US-1");
    Comprobar(correlativo.Siguiente("RO-", 0) == "RO-1", "inicio independiente RO-1");
    Comprobar(correlativo.Siguiente("US-", 49999) == "US-50000", "límite inclusivo 50000");
    Comprobar(correlativo.Siguiente("US-", 9) == "US-10", "sin relleno");
    Rechaza<InvalidOperationException>(() => correlativo.Siguiente("US-", 50000), "agotamiento");
    Rechaza<ArgumentOutOfRangeException>(() => correlativo.Siguiente("US-", -1), "estado negativo");
    Rechaza<ArgumentOutOfRangeException>(() => correlativo.Siguiente("RO-", 50001), "estado fuera de rango");
    Rechaza<ArgumentException>(() => correlativo.Siguiente("XX-", 0), "prefijo desconocido");
    var valida = new ConfiguracionMySql { Server = "localhost", Port = 3306,
        Database = "berrutiauthtests", User = "berruti_test", Password = "ficticia-solo-unit" };
    Comprobar(valida.EsValida(), "configuración aislada válida (sin conexión)");
    foreach (var host in new[] { "localhost", "127.0.0.1", "::1" })
    {
        valida.Server = host;
        Comprobar(valida.EsValida(), "host loopback permitido");
    }
    valida.Server = "otro-host";
    Comprobar(!valida.EsValida(), "rechaza host remoto");
    valida.Server = "localhost";
    valida.Database = "berrutilocal";
    Comprobar(!valida.EsValida(), "rechaza demo");
    valida.Database = "berrutiauthtests";
    valida.User = "root";
    Comprobar(!valida.EsValida(), "rechaza root");
    valida.User = "berruti_test";
    valida.Password = "";
    Comprobar(!valida.EsValida(), "rechaza contraseña vacía");
    valida.Password = "ficticia-solo-unit";
    valida.Port = 0;
    Comprobar(!valida.EsValida(), "rechaza puerto incorrecto");
    Console.WriteLine("Unit no abre MySQL ni demuestra persistencia/atomicidad.");
    return fallos == 0 ? 0 : 1;
}
var frontera = args.SequenceEqual(new[] { "--mysql-boundary" });
if (!frontera && !args.SequenceEqual(new[] { "--mysql" }))
{
    Console.Error.WriteLine("Uso: --unit | --mysql | --mysql-boundary. Los modos MySQL requieren configuración humana y fixture fresca normal (0) o frontera (49998); no ejecutan DDL ni reinician datos.");
    return 1;
}

var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var ruta = Path.Combine(raiz, ".local", "mysql-tests.json");
if (!File.Exists(ruta))
{
    Console.Error.WriteLine("Falta .local/mysql-tests.json en la raíz. Crearlo personalmente según docs/how-to/mysql-persistence-checks.md. Pruebas MySQL NO ejecutadas.");
    return 1;
}
try
{
    // Única lectura privada: runtime local. Nunca imprimir contenido/excepciones.
    var configuracion = JsonSerializer.Deserialize<ConfiguracionMySql>(File.ReadAllText(ruta),
        new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow });
    if (configuracion == null || !configuracion.EsValida())
    {
        Console.Error.WriteLine("Configuración rechazada: requiere loopback, puerto 3306, base berrutiauthtests, usuario berruti_test y contraseña no vacía. No se abrió conexión.");
        return 1;
    }
    var cadena = new MySqlConnectionStringBuilder
    {
        Server = configuracion.Server, Port = (uint)configuracion.Port,
        Database = configuracion.Database, UserID = configuracion.User, Password = configuracion.Password,
        ConnectionTimeout = 3, DefaultCommandTimeout = 5, PersistSecurityInfo = false
    }.ConnectionString;
    BerrutiDbContext Contexto() => new(new DbContextOptionsBuilder<BerrutiDbContext>().UseMySQL(cadena).Options);
    await using var contexto = Contexto();
    await contexto.Database.OpenConnectionAsync();
    await using (var identidad = contexto.Database.GetDbConnection().CreateCommand())
    {
        identidad.CommandText = "SELECT CURRENT_USER()";
        if ((string?)await identidad.ExecuteScalarAsync() != "berruti_test@localhost")
            throw new InvalidOperationException();
        identidad.CommandText = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'berrutiauthtests' AND TABLE_NAME IN ('usuario','rol','usuario_rol','contador_entidad') AND ENGINE = 'InnoDB'";
        if (Convert.ToInt32(await identidad.ExecuteScalarAsync()) != 4)
            throw new InvalidOperationException();
    }
    async Task<(int Usuarios, int Membresias, int Ultimo, int Rol)> Estado() =>
        (await contexto.Usuarios.CountAsync(), await contexto.UsuariosRoles.CountAsync(),
         await contexto.Contadores.Where(c => c.Prefijo == "US-").Select(c => c.Ultimo).SingleAsync(),
         await contexto.Contadores.Where(c => c.Prefijo == "RO-").Select(c => c.Ultimo).SingleAsync());
    // Puerta de solo lectura antes de cualquier DML: también rechaza fixtures ya usadas.
    var contadores = await contexto.Contadores.AsNoTracking().ToArrayAsync();
    var esperado = frontera ? 49998 : 0;
    if (contadores.Length != 2 || !contadores.Any(c => c.Prefijo == "US-" && c.Ultimo == esperado) ||
        !contadores.Any(c => c.Prefijo == "RO-" && c.Ultimo == esperado) ||
        await contexto.Usuarios.AnyAsync() || await contexto.Roles.AnyAsync() || await contexto.UsuariosRoles.AnyAsync())
    {
        Console.Error.WriteLine($"Fixture rechazada antes de DML: se requieren tablas de cuentas vacías y exactamente US-/RO- en {esperado}. No reutilizar ni reiniciar una fixture usada.");
        return 1;
    }
    var repositorio = Cuentas(contexto);
    if (frontera)
    {
        // Snapshot completo, solo en memoria; nunca imprimir datos ni contraseñas.
        async Task<string> Instantanea() => JsonSerializer.Serialize(new
        {
            Usuarios = await contexto.Usuarios.AsNoTracking().OrderBy(u => u.Identificador)
                .Select(u => new { u.Identificador, u.IdentificadorPublico, u.Correo, u.Contrasena, u.Activo }).ToArrayAsync(),
            Roles = await contexto.Roles.AsNoTracking().OrderBy(r => r.Identificador)
                .Select(r => new { r.Identificador, r.Nombre }).ToArrayAsync(),
            Membresias = await contexto.UsuariosRoles.AsNoTracking().OrderBy(m => m.UsuarioId).ThenBy(m => m.RolId)
                .Select(m => new { m.UsuarioId, m.RolId }).ToArrayAsync(),
            Contadores = await contexto.Contadores.AsNoTracking().OrderBy(c => c.Prefijo)
                .Select(c => new { c.Prefijo, c.Ultimo }).ToArrayAsync()
        });
        var rol49999 = await repositorio.CrearRolAsync(RolIngreso.Pasajero);
        Comprobar(rol49999.Identificador == "RO-49999", "RO-49999 por alta pública");
        var estado = await Estado();
        Comprobar(estado.Rol == 49999 && estado.Ultimo == 49998, "RO-49999 persistido sin incrementar US-");
        var rol50000 = await repositorio.CrearRolAsync(RolIngreso.Cobrador);
        Comprobar(rol50000.Identificador == "RO-50000", "RO-50000 inclusivo por alta pública");
        Comprobar(await contexto.Roles.AsNoTracking().CountAsync(r =>
            (r.Identificador == "RO-49999" && r.Nombre == "Pasajero") ||
            (r.Identificador == "RO-50000" && r.Nombre == "Cobrador")) == 2 &&
            (await Estado()).Rol == 50000 && (await Estado()).Ultimo == 49998, "ambos roles y frontera RO- persistidos independientemente");
        var antesRechazo = await Instantanea();
        try
        {
            await repositorio.CrearRolAsync(RolIngreso.Administrador);
            Comprobar(false, "RO- rechaza agotamiento exacto");
        }
        catch (InvalidOperationException error) when (error.Message == "Se agotó el rango de identificadores de la entidad.")
        { Comprobar(true, "RO- rechaza agotamiento exacto"); }
        Comprobar(await Instantanea() == antesRechazo, "rechazo RO- conserva todos los registros, membresías y ambos contadores");

        var marcaFrontera = Guid.NewGuid().ToString("N");
        foreach (var numero in new[] { 49999, 50000 })
        {
            var correoFrontera = $"t1-{marcaFrontera}-{numero}@example.invalid";
            var rolUsuario = numero == 49999 ? rol49999 : rol50000;
            var usuarioFrontera = await repositorio.CrearUsuarioAsync(correoFrontera, "ficticia-local", rolUsuario.Identificador);
            Comprobar(usuarioFrontera.Identificador == $"US-{numero}", $"US-{numero} por alta pública");
            var leido = await repositorio.BuscarPorCorreoAsync(correoFrontera);
            Comprobar(leido?.Identificador == $"US-{numero}" && leido.Activo &&
                (await repositorio.ConsultarRolesAsync(usuarioFrontera.Identificador)).SequenceEqual(new[] { rolUsuario.Nombre }),
                $"lectura pública y rol de US-{numero}");
            estado = await Estado();
            Comprobar(estado.Ultimo == numero && estado.Rol == 50000 &&
                estado.Usuarios == numero - 49998 && estado.Membresias == numero - 49998,
                $"US-{numero} y membresía persistidos sin incrementar RO-");
        }
        antesRechazo = await Instantanea();
        try
        {
            await repositorio.CrearUsuarioAsync($"t1-{marcaFrontera}-agotado@example.invalid", "ficticia-local", rol50000.Identificador);
            Comprobar(false, "US- rechaza agotamiento exacto");
        }
        catch (InvalidOperationException error) when (error.Message == "Se agotó el rango de identificadores de la entidad.")
        { Comprobar(true, "US- rechaza agotamiento exacto"); }
        Comprobar(await Instantanea() == antesRechazo, "rechazo US- conserva todos los registros, membresías y ambos contadores");
        Console.WriteLine("Escenario frontera ejecutado; no ejecuta escenario normal/concurrencia. Fixture consumida, sin reinicio ni limpieza.");
        return fallos == 0 ? 0 : 1;
    }
    var inicio = await Estado();
    var rol = await contexto.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Nombre == "Pasajero");
    if (rol == null)
    {
        rol = await repositorio.CrearRolAsync(RolIngreso.Pasajero);
        Comprobar(rol.Identificador == correlativo.Siguiente("RO-", inicio.Rol), "alta rol por repositorio");
        Comprobar((await Estado()).Ultimo == inicio.Ultimo, "contador rol no incrementa usuario");
    }
    var previo = await Estado();
    var marca = Guid.NewGuid().ToString("N");
    var correo = $"t1-{marca}@example.invalid";
    var usuario = await repositorio.CrearUsuarioAsync(correo, "ficticia-local", rol.Identificador);
    Comprobar(usuario.Identificador == correlativo.Siguiente("US-", previo.Ultimo), "alta correlativa pública");
    Comprobar((await Estado()).Rol == previo.Rol, "contador usuario no incrementa rol");
    Comprobar((await repositorio.BuscarPorCorreoAsync(correo))?.Identificador == usuario.Identificador &&
        (await repositorio.ConsultarRolesAsync(usuario.Identificador)).SequenceEqual(new[] { "Pasajero" }), "lectura cuenta y membresía");
    previo = await Estado();
    try { await repositorio.CrearUsuarioAsync(correo, "ficticia-local", rol.Identificador); Comprobar(false, "unicidad rechaza duplicado"); }
    catch (DbUpdateException) { Comprobar(true, "unicidad rechaza duplicado"); }
    Comprobar(await Estado() == previo, "fallo SQL revierte contador, cuenta y membresía");
    var rolesExistentes = await contexto.Roles.Select(r => r.Identificador).ToArrayAsync();
    var rolAusente = Enumerable.Range(1, 50000).Select(n => $"RO-{n}").First(id => !rolesExistentes.Contains(id));
    try { await repositorio.CrearUsuarioAsync($"t1-{marca}-fk@example.invalid", "ficticia-local", rolAusente); Comprobar(false, "FK rechaza membresía inexistente"); }
    catch (DbUpdateException) { Comprobar(true, "FK rechaza membresía inexistente"); }
    Comprobar(await Estado() == previo, "fallo de membresía revierte también usuario insertado y contador");
    try { await repositorio.CrearUsuarioAsync("no-es-correo", "x", rol.Identificador); Comprobar(false, "entrada inválida"); }
    catch (ArgumentException) { Comprobar(true, "entrada inválida"); }
    Comprobar(await Estado() == previo, "entrada inválida no modifica estado");

    // Borra exclusivamente la fixture recién creada, nunca tablas ni contadores.
    await contexto.UsuariosRoles.Where(m => m.UsuarioId == usuario.Identificador).ExecuteDeleteAsync();
    await contexto.Usuarios.Where(u => u.Identificador == usuario.Identificador).ExecuteDeleteAsync();
    var siguiente = await repositorio.CrearUsuarioAsync($"t1-{marca}-nuevo@example.invalid", "ficticia-local", rol.Identificador);
    Comprobar(siguiente.Identificador == correlativo.Siguiente("US-", previo.Ultimo), "no reutiliza identidad borrada");
    previo = await Estado();
    var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var altas = Enumerable.Range(0, 4).Select(async i =>
    {
        await using var paralelo = Contexto();
        await salida.Task;
        return await Cuentas(paralelo).CrearUsuarioAsync($"t1-{marca}-{i}@example.invalid", "ficticia-local", rol.Identificador);
    }).ToArray();
    salida.SetResult();
    var creados = await Task.WhenAll(altas);
    Comprobar(creados.Select(u => u.Identificador).Distinct().Count() == 4 &&
        creados.Select(u => int.Parse(u.Identificador[3..])).Order().SequenceEqual(Enumerable.Range(previo.Ultimo + 1, 4)), "cuatro altas simultáneas únicas y consecutivas");
    var despues = await Estado();
    Comprobar(despues.Ultimo == previo.Ultimo + 4 && despues.Usuarios == previo.Usuarios + 4 &&
        despues.Membresias == previo.Membresias + 4 && despues.Rol == previo.Rol, "concurrencia persiste cuentas, membresías y contador independiente");
    Console.WriteLine("Escenario normal ejecutado; no ejecuta frontera MySQL. Fixture consumida, sin reinicio ni limpieza global.");
    return fallos == 0 ? 0 : 1;
}
catch (JsonException)
{
    Console.Error.WriteLine("JSON privado mal formado o claves desconocidas. No se abrió conexión.");
    return 1;
}
catch
{
    Console.Error.WriteLine("Prueba MySQL fallida. Revisar localmente esquema, cuenta/permisos y conectividad con administrador. Detalles privados omitidos; no implica PASS ni limpieza automática.");
    return 1;
}

class ConfiguracionMySql
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EsValida() => (Server == "localhost" || Server == "127.0.0.1" || Server == "::1") &&
        Port == 3306 && Database == "berrutiauthtests" && User == "berruti_test" && !string.IsNullOrWhiteSpace(Password);
}
