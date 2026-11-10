namespace Web_App.Security;

public class LimiteIngresoHttps
{
    private readonly RequestDelegate _next;

    public LimiteIngresoHttps(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        var ingreso = path.StartsWithSegments("/ingreso", StringComparison.OrdinalIgnoreCase);
        var circuito = path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase);
        if (!context.Request.IsHttps && (ingreso || circuito))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync("Se requiere HTTPS para ingresar. Abrí la dirección HTTPS configurada para esta aplicación.");
            return;
        }

        await _next(context);
    }
}
