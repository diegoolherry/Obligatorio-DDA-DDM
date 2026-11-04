using API_Berruti.Data;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Authentication;
using API_Berruti.Services.Identifiers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace API_Berruti.Security.Authentication;

public static class ConfiguracionAutenticacion
{
    public static MySqlConnectionStringBuilder ValidarConexion(string? cadena, string entorno)
    {
        if (string.IsNullOrWhiteSpace(cadena)) throw new InvalidOperationException();
        var conexion = new MySqlConnectionStringBuilder(cadena);
        var local = conexion.Server is "localhost" or "127.0.0.1" or "::1";
        var pareja = entorno == "Development" && conexion.Database == "berrutilocal" && conexion.UserID == "berruti_app" ||
            entorno == "AuthenticationTests" && conexion.Database == "berrutiauthtests" && conexion.UserID == "berruti_test";
        if (!local || !pareja || conexion.Port != 3306 || string.IsNullOrWhiteSpace(conexion.Password))
            throw new InvalidOperationException();
        conexion.PersistSecurityInfo = false;
        conexion.AllowLoadLocalInfile = false;
        conexion.ConnectionTimeout = 3;
        conexion.DefaultCommandTimeout = 5;
        return conexion;
    }

    public static IServiceCollection AgregarAutenticacionLocal(this IServiceCollection servicios,
        IConfiguration configuracion, IHostEnvironment entorno)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddDbContext<BerrutiDbContext>(opciones =>
        {
            var conexion = ValidarConexion(configuracion.GetConnectionString("Berruti"), entorno.EnvironmentName);
            opciones.UseMySQL(conexion.ConnectionString);
            opciones.EnableSensitiveDataLogging(false);
        });
        servicios.AddScoped<ICorrelativo, Correlativo>();
        servicios.AddScoped<IContadoresRepository, ContadoresRepository>();
        servicios.AddScoped<ICuentasRepository, CuentasRepository>();
        servicios.AddScoped<ISesionesRepository, SesionesRepository>();
        servicios.AddScoped<IAutenticacionService, AutenticacionService>();
        servicios.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, SesionBearerHandler>("Bearer", _ => { });
        servicios.AddAuthorization();
        return servicios;
    }
}
