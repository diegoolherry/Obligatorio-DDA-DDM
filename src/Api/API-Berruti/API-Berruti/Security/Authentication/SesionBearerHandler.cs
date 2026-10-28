using System.Text.Encodings.Web;
using API_Berruti.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace API_Berruti.Security.Authentication;

public class SesionBearerHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SesionBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory logger, UrlEncoder encoder) : base(opciones, logger, encoder) { }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps) return AuthenticateResult.NoResult();
        var token = EncabezadoBearer.Token(Request.Headers.Authorization.ToString());
        if (token == null) return AuthenticateResult.NoResult();
        // Lazy resolution keeps unrelated scaffold requests independent of MySQL/configuration.
        var servicio = Context.RequestServices.GetRequiredService<IAutenticacionService>();
        var identidad = await servicio.AutenticarAsync(token);
        return identidad == null ? AuthenticateResult.Fail("Sesión no válida.") :
            AuthenticateResult.Success(new AuthenticationTicket(identidad, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        return Task.CompletedTask;
    }
}
