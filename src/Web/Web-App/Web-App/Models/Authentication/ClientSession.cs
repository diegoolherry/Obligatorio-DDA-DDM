namespace Web_App.Models.Authentication;

// No credenciales ni identificadores en el estado que consume la vista.
public record EstadoSesion(bool Autenticada, bool Ocupada);

public record ResultadoAutenticacion(bool Exito, int? EstadoHttp, string Codigo, string Mensaje);
