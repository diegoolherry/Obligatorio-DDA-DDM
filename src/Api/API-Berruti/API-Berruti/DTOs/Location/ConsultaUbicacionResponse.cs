namespace API_Berruti.DTOs.Location;

public enum EstadoDemora
{
    SinDatos,
    EnHorario,
    ConDemora
}

public class UltimaPosicionDto
{
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public DateTime RecibidaUtc { get; set; }
    public double AntiguedadSegundos { get; set; }
}

public class ParametrosUbicacionDto
{
    public int PeriodicidadSegundos { get; set; }
    public int UmbralDesactualizacionSegundos { get; set; }
    public double VelocidadPromedioKmH { get; set; }
    public int ToleranciaDemoraMinutos { get; set; }
}

public class ConsultaUbicacionResponse
{
    public ServicioUbicacionDto Servicio { get; set; } = new();
    public UltimaPosicionDto? UltimaPosicion { get; set; }
    public bool Desactualizada { get; set; }
    // Hora local de Uruguay; solo con posición vigente y destino configurado.
    public DateTime? LlegadaAproximada { get; set; }
    public EstadoDemora EstadoDemora { get; set; }
    public string? Motivo { get; set; }
    public ParametrosUbicacionDto Parametros { get; set; } = new();
    // Los datos y parámetros son académicos: no son telemetría ni horarios reales.
    public bool DatosFicticios { get; set; } = true;
}
