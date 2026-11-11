namespace API_Berruti.Models.Catalogo;

// Feriado cargado por año. En esos días corren las salidas de «Domingos y feriados».
public class Feriado
{
    public DateTime Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
