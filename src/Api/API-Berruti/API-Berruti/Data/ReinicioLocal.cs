using API_Berruti.DTOs.Authentication;
using API_Berruti.Models.Authentication;
using API_Berruti.Models.Location;
using API_Berruti.Repositories.Authentication;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Data;

// Recrea la base local descartable: borra sus tablas, aplica las migraciones y siembra
// cuentas ficticias. La conexión ya pasó por ValidarConexion (solo localhost y bases locales).
public static class ReinicioLocal
{
    public const string Argumento = "--reset-local";

    private static readonly (string Correo, RolIngreso Rol)[] CuentasDemo =
    {
        ("pasajero.demo@example.test", RolIngreso.Pasajero),
        ("cobrador.demo@example.test", RolIngreso.Cobrador),
        ("admin.demo@example.test", RolIngreso.Administrador),
    };

    public static async Task<int> EjecutarAsync(IServiceProvider servicios, IConfiguration configuracion, IHostEnvironment entorno)
    {
        var sembrar = entorno.IsDevelopment();
        var contrasena = configuracion["DemoSeed:Contrasena"];
        if (sembrar && (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length > 255))
        {
            Console.Error.WriteLine("Reinicio local: falta la contraseña demo DemoSeed__Contrasena (1 a 255 caracteres).");
            return 2;
        }

        using var alcance = servicios.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<BerrutiDbContext>();

        await BorrarTablasAsync(contexto);
        await contexto.Database.MigrateAsync();
        Console.WriteLine("Reinicio local: esquema recreado con las migraciones.");

        if (!sembrar) return 0;

        contexto.Contadores.AddRange(new ContadorEntidad { Prefijo = "US-" }, new ContadorEntidad { Prefijo = "RO-" });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var cuentas = alcance.ServiceProvider.GetRequiredService<ICuentasRepository>();
        var cobradores = new List<string>();
        foreach (var (correo, rolIngreso) in CuentasDemo)
        {
            var rol = await cuentas.CrearRolAsync(rolIngreso);
            var usuario = await cuentas.CrearUsuarioAsync(correo, contrasena!, rol.Identificador);
            if (rolIngreso == RolIngreso.Cobrador) cobradores.Add(usuario.Identificador);
            Console.WriteLine($"Reinicio local: cuenta {correo} ({rolIngreso}).");
        }

        // La fecha del servicio es mañana respecto del día del reinicio; las tarifas la cubren por un año.
        var fecha = DateTime.Today.AddDays(1);
        var escenario = EscenarioOperativoDemo.Construir(fecha);
        contexto.Paradas.AddRange(escenario.Paradas);
        contexto.Tramos.AddRange(escenario.Tramos);
        contexto.Servicios.AddRange(escenario.Servicios);
        contexto.Tarifas.AddRange(escenario.Tarifas);
        // MVP-009: el cobrador demo viaja en ambos servicios y publica su ubicación desde el celular.
        contexto.AsignacionesCobrador.AddRange(cobradores.SelectMany(usuarioId =>
            escenario.Servicios.Select(s => new AsignacionCobrador { UsuarioId = usuarioId, ServicioId = s.Id })));
        await contexto.SaveChangesAsync();
        Console.WriteLine($"Reinicio local: escenario operativo {fecha:yyyy-MM-dd} ({escenario.Paradas.Count} paradas, {escenario.Tramos.Count} tramos, {escenario.Servicios.Count} servicios, {escenario.Tarifas.Count} tarifas, {cobradores.Count * escenario.Servicios.Count} asignaciones de cobrador).");
        return 0;
    }

    private static async Task BorrarTablasAsync(BerrutiDbContext contexto)
    {
        var tablas = await contexto.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS Value FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE()")
            .ToListAsync();
        if (tablas.Count == 0) return;

        var conexion = contexto.Database.GetDbConnection();
        await conexion.OpenAsync();
        try
        {
            await using var comando = conexion.CreateCommand();
            // Los nombres vienen de information_schema de la base local validada, no del usuario.
            var borrar = string.Join(", ", tablas.Select(t => $"`{t.Replace("`", "``")}`"));
            comando.CommandText = $"SET FOREIGN_KEY_CHECKS = 0; DROP TABLE {borrar}; SET FOREIGN_KEY_CHECKS = 1;";
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await conexion.CloseAsync();
        }
        Console.WriteLine($"Reinicio local: {tablas.Count} tablas borradas.");
    }
}
