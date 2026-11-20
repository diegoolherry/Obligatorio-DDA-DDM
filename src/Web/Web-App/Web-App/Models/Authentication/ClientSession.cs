namespace Web_App.Models.Authentication;

// No credenciales ni identificadores en el estado que consume la vista.
public record EstadoSesion(bool Autenticada, bool Ocupada, IReadOnlyList<RolIngreso> Roles)
{
    // Solo para mostrar u ocultar enlaces: la API comprueba cada permiso con el token.
    public bool EsPasajero => Autenticada && Roles.Contains(RolIngreso.Pasajero);
    public bool EsAdministrador => Autenticada && Roles.Contains(RolIngreso.Administrador);
}

public record ResultadoAutenticacion(bool Exito, int? EstadoHttp, string Codigo, string Mensaje);
