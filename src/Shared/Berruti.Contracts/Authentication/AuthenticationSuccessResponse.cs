namespace Berruti.Contracts.Authentication
{
    public class RespuestaIngresoDto
    {
        public string IdentificadorUsuario { get; set; } = string.Empty;

        public RolIngreso[] Roles { get; set; } = new RolIngreso[0];
    }
}
