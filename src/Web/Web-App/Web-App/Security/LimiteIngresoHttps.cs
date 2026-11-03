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
        if (!context.Request.IsHttps
            && context.Request.Path.StartsWithSegments("/ingreso", StringComparison.OrdinalIgnoreCase))
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
