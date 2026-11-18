using System.ComponentModel.DataAnnotations;
using Web_App.Models.Catalogo;

namespace Web_App.Components.Admin.Catalogo;

// FIN-008-03: modelos tipados de los formularios del catálogo. Los límites copian los de la API; la API sigue validando todo.
public sealed class FormularioBloque
{
    public string? Id { get; set; }
    [Required(ErrorMessage = "Ingresá la clave."), StringLength(16, ErrorMessage = "La clave tiene hasta 16 caracteres.")]
    public string Clave { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el nombre."), StringLength(100, ErrorMessage = "El nombre tiene hasta 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá la empresa."), StringLength(100, ErrorMessage = "La empresa tiene hasta 100 caracteres.")]
    public string Empresa { get; set; } = string.Empty;
    public DateOnly? VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    [StringLength(200, ErrorMessage = "La URL de la fuente tiene hasta 200 caracteres.")]
    public string? FuenteUrl { get; set; }
}

public sealed class FormularioRuta
{
    public string? Id { get; set; }
    [Required(ErrorMessage = "Elegí el bloque.")] public string BloqueId { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá la clave."), StringLength(16, ErrorMessage = "La clave tiene hasta 16 caracteres.")]
    public string Clave { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá la familia."), StringLength(100, ErrorMessage = "La familia tiene hasta 100 caracteres.")]
    public string Familia { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá el nombre."), StringLength(100, ErrorMessage = "El nombre tiene hasta 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    // Paradas en orden de recorrido; se copia la lista al editar para no alterar el catálogo cargado.
    public List<string> ParadaIds { get; set; } = [];
}

public sealed class FormularioSalida
{
    public string? Id { get; set; }
    [Required(ErrorMessage = "Elegí la ruta.")] public string RutaId { get; set; } = string.Empty;
    public DiasOperacion Dias { get; set; }
    [StringLength(16, ErrorMessage = "Los códigos tienen hasta 16 caracteres.")] public string? Codigos { get; set; }
    [StringLength(200, ErrorMessage = "La nota tiene hasta 200 caracteres.")] public string? Nota { get; set; }
    // Hora «HH:mm» por orden de parada; vacía = la parada no tiene hora publicada.
    public Dictionary<int, string?> Horas { get; set; } = [];
}

public sealed class FormularioFeriado
{
    // Fecha original al editar: va en la ruta y no se puede cambiar.
    public DateOnly? FechaOriginal { get; set; }
    [Required(ErrorMessage = "Ingresá la fecha.")] public DateOnly? Fecha { get; set; }
    [Required(ErrorMessage = "Ingresá el nombre."), StringLength(100, ErrorMessage = "El nombre tiene hasta 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class FormularioExcepcion
{
    public string? Id { get; set; }
    // Vacío: aplica a todos los bloques.
    public string BloqueId { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ingresá la fecha desde.")] public DateOnly? Desde { get; set; }
    [Required(ErrorMessage = "Ingresá la fecha hasta.")] public DateOnly? Hasta { get; set; }
    public TipoExcepcion Tipo { get; set; }
    [Required(ErrorMessage = "Ingresá el motivo."), StringLength(200, ErrorMessage = "El motivo tiene hasta 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

// Baja pendiente de confirmar. Logica: baja lógica (bloque, ruta, salida) frente a eliminación definitiva.
public sealed record BajaCatalogo(string Tipo, string Id, string Descripcion, bool Logica);
