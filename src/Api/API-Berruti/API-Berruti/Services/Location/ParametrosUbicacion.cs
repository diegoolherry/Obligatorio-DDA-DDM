using System.Globalization;

namespace API_Berruti.Services.Location;

public record Coordenada(double Latitud, double Longitud);

// Parámetros académicos de la sección "Ubicacion" de appsettings.json. No son valores operativos reales.
public class ParametrosUbicacion
{
    public int PeriodicidadSegundos { get; init; }
    public int MargenPeriodicidadSegundos { get; init; }
    public int UmbralDesactualizacionSegundos { get; init; }
    public double VelocidadPromedioKmH { get; init; }
    public int ToleranciaDemoraMinutos { get; init; }
    public TimeZoneInfo ZonaHoraria { get; init; } = TimeZoneInfo.Utc;
    // Clave: nombre de la parada de destino del tramo.
    public IReadOnlyDictionary<string, Coordenada> Destinos { get; init; } = new Dictionary<string, Coordenada>();

    public static ParametrosUbicacion Desde(IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection("Ubicacion");
        int Entero(string clave, int minimo)
        {
            if (!int.TryParse(seccion[clave], NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) || valor < minimo)
                throw new InvalidOperationException($"Ubicacion:{clave} debe ser un entero mayor o igual a {minimo}.");
            return valor;
        }
        double Real(IConfiguration origen, string clave, double minimo, double maximo)
        {
            if (!double.TryParse(origen[clave], NumberStyles.Float, CultureInfo.InvariantCulture, out var valor) || valor < minimo || valor > maximo)
                throw new InvalidOperationException($"Ubicacion: {clave} fuera de rango.");
            return valor;
        }

        var zona = seccion["ZonaHoraria"];
        if (string.IsNullOrWhiteSpace(zona)) throw new InvalidOperationException("Ubicacion:ZonaHoraria es obligatoria.");
        var destinos = new Dictionary<string, Coordenada>(StringComparer.Ordinal);
        foreach (var destino in seccion.GetSection("Destinos").GetChildren())
            destinos[destino.Key] = new Coordenada(Real(destino, "Latitud", -90, 90), Real(destino, "Longitud", -180, 180));

        var velocidad = Real(seccion, "VelocidadPromedioKmH", 1, 200);
        return new ParametrosUbicacion
        {
            PeriodicidadSegundos = Entero("PeriodicidadSegundos", 1),
            MargenPeriodicidadSegundos = Entero("MargenPeriodicidadSegundos", 0),
            UmbralDesactualizacionSegundos = Entero("UmbralDesactualizacionSegundos", 1),
            VelocidadPromedioKmH = velocidad,
            ToleranciaDemoraMinutos = Entero("ToleranciaDemoraMinutos", 0),
            ZonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(zona),
            Destinos = destinos
        };
    }
}
