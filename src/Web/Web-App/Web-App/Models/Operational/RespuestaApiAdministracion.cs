namespace Web_App.Models.Operational;

public record RespuestaApiAdministracion(bool Exito, int? EstadoHttp, string? Contenido, string Mensaje);
