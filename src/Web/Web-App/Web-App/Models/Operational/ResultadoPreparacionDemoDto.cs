namespace Web_App.Models.Operational;

public class ResultadoPreparacionDemoDto
{
    public bool Exito { get; set; }
    public bool Conflicto { get; set; }
    public bool SinCambios { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public IReadOnlyList<string> Conflictos { get; set; } = [];
    public int ParadasCreadas { get; set; }
    public int TramosCreados { get; set; }
    public int ServiciosCreados { get; set; }
    public int TarifasCreadas { get; set; }
}
