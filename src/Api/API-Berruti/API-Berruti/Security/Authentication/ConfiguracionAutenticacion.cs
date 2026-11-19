using API_Berruti.Data;
using API_Berruti.Models.Authentication;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Services.Authentication;
using API_Berruti.Services.Identifiers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            entorno == "AuthenticationTests" && conexion.Database == "berrutiauthtests" && conexion.UserID == "berruti_test" ||
            entorno == "PurchaseTests" && conexion.Database == "berruticomprastests" && conexion.UserID == "berruti_purchase_test";
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
            opciones.AddInterceptors(new EsperaBloqueoInterceptor());
            opciones.EnableSensitiveDataLogging(false);
        });
        servicios.AddScoped<ICorrelativo, Correlativo>();
        servicios.AddScoped<IContadoresRepository, ContadoresRepository>();
        servicios.AddScoped<ICuentasRepository, CuentasRepository>();
        // Hash con sal y costo de ASP.NET Core Identity; solo se registra el hasher, no el sistema de usuarios de Identity.
        servicios.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
        servicios.AddSingleton(new EmisorJwt(configuracion));
        servicios.AddScoped<IAutenticacionService, AutenticacionService>();
        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                // Se lee al construir las opciones (primer uso), no al registrar: construir DI no exige configuración.
                var jwt = ConfiguracionJwt.Leer(configuracion);
                opciones.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = jwt.Emisor,
                    ValidateAudience = true, ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.Clave,
                    ValidateLifetime = true,
                    // Solo HS256 con la clave local; rechaza `alg: none` y otros algoritmos.
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
                };
                opciones.Events = new JwtBearerEvents
                {
                    // Mismo 401 que antes para ausente, inválido o vencido, sin revelar el motivo.
                    OnChallenge = contexto =>
                    {
                        contexto.HandleResponse();
                        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        contexto.Response.Headers.WWWAuthenticate = "Bearer";
                        return Task.CompletedTask;
                    }
                };
            });
        servicios.AddAuthorization();
        return servicios;
    }
}
