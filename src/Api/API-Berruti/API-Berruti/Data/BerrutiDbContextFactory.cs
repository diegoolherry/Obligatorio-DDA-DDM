using API_Berruti.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace API_Berruti.Data;

// Solo para `dotnet ef`. `migrations add` y `migrations script` no se conectan a MySQL;
// `database update` usa ConnectionStrings__Berruti con el mismo guard local que la API.
public class BerrutiDbContextFactory : IDesignTimeDbContextFactory<BerrutiDbContext>
{
    private const string SinConexion = "server=localhost;port=3306;database=berrutilocal;user=berruti_app;password=sin-conexion";

    public BerrutiDbContext CreateDbContext(string[] args)
    {
        var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var cadena = Environment.GetEnvironmentVariable("ConnectionStrings__Berruti") ?? SinConexion;
        var conexion = ConfiguracionAutenticacion.ValidarConexion(cadena, entorno);
        var opciones = new DbContextOptionsBuilder<BerrutiDbContext>().UseMySQL(conexion.ConnectionString);
        return new BerrutiDbContext(opciones.Options);
    }
}
