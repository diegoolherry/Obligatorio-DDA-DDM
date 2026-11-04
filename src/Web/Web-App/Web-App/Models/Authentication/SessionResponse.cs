namespace Web_App.Models.Authentication;

public class RespuestaSesionDto
{
    public string IdentificadorUsuario { get; set; } = string.Empty;
    public RolIngreso[] Roles { get; set; } = new RolIngreso[0];
}
