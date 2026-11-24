using System.ComponentModel.DataAnnotations;

namespace API_Berruti.DTOs.Operational;

public sealed class ValidarTicketRequestDto
{
    // Contenido del QR tal como lo leyó la cámara. Sin MaxLength: un QR ajeno y largo debe responder «código inexistente», no 400.
    [Required(ErrorMessage = "El código es obligatorio.")]
    public string QrOpaco { get; set; } = string.Empty;

    // Servicio del tramo que el cobrador eligió (los de GET /api/ubicacion/cobrador/servicios).
    [Required(ErrorMessage = "El tramo es obligatorio.")]
    [StringLength(36, ErrorMessage = "El tramo no es válido.")]
    public string ServicioId { get; set; } = string.Empty;
}

// valido | yaUtilizado | vencido | tramoIncorrecto | codigoInexistente (camelCase en JSON).
public enum ResultadoValidacion
{
    Valido,
    YaUtilizado,
    Vencido,
    TramoIncorrecto,
    CodigoInexistente
}

// Datos del tramo del ticket para mostrar al cobrador; sin QR, pasajero ni IDs de usuario.
public sealed class TicketValidadoDto
{
    public string TicketId { get; set; } = string.Empty;
    public string ServicioId { get; set; } = string.Empty;
    public string TramoId { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string DestinoNombre { get; set; } = string.Empty;
    public DateTime Salida { get; set; }
    public DateTime Llegada { get; set; }
}

public sealed class ResultadoValidacionTicketDto
{
    public ResultadoValidacion Resultado { get; set; }
    // Nulo solo con «codigoInexistente».
    public TicketValidadoDto? Ticket { get; set; }
    // «valido»: hora UTC en que se consumió. «yaUtilizado»: hora UTC del primer uso. Nulo en los demás.
    public DateTime? UsadoUtc { get; set; }
}
