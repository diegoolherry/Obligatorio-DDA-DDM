namespace API_Berruti.Models.Authentication;

// Entidades internas; nunca son respuestas HTTP ni credenciales reales.
public class Usuario
{
    public string Identificador { get; set; } = string.Empty;
    public string IdentificadorPublico { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class Rol
{
    public string Identificador { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioRol
{
    public string UsuarioId { get; set; } = string.Empty;
    public string RolId { get; set; } = string.Empty;
}

public class ContadorEntidad
{
    public string Prefijo { get; set; } = string.Empty;
    public int Ultimo { get; set; }
}
