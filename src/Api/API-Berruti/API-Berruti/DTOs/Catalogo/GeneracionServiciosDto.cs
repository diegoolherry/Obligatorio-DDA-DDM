using API_Berruti.Models.Operational;

namespace API_Berruti.DTOs.Catalogo;

// Capacidad y política no tienen valor por defecto: el horario no informa la flota ni sus asientos.
public class GenerarServiciosDto
{
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int? Capacidad { get; set; }
    public PoliticaAsiento? PoliticaAsiento { get; set; }
}

public class ResultadoGeneracionDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int ServiciosCreados { get; set; }
    public int ServiciosExistentes { get; set; }
    public int TramosCreados { get; set; }
}
