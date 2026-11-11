namespace Web_App.Models.Location;

// Copia web de los DTOs de la API (DTOs/Location); no hay proyecto compartido.
public enum EstadoDemora
{
    SinDatos,
    EnHorario,
    ConDemora
}

public class ServicioUbicacionDto
{
    public string Id { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
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
    public DateTime? LlegadaAproximada { get; set; }
    public EstadoDemora EstadoDemora { get; set; }
    public string? Motivo { get; set; }
    public ParametrosUbicacionDto Parametros { get; set; } = new();
    public bool DatosFicticios { get; set; } = true;
}
