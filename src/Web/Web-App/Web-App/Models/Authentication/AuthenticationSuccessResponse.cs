namespace Web_App.Models.Authentication
{
    public class RespuestaIngresoDto
    {
        public string IdentificadorUsuario { get; set; } = string.Empty;

        public RolIngreso[] Roles { get; set; } = new RolIngreso[0];

        public string Token { get; set; } = string.Empty;

        public string VenceEnUtc { get; set; } = string.Empty;
    }
}
