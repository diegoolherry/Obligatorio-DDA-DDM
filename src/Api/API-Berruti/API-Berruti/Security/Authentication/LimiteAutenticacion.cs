namespace API_Berruti.Security.Authentication;

public class LimiteAutenticacion
{
    private readonly RequestDelegate _siguiente;
    public LimiteAutenticacion(RequestDelegate siguiente) { _siguiente = siguiente; }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var sensible = contexto.Request.Path.StartsWithSegments("/api/autenticacion", StringComparison.OrdinalIgnoreCase) ||
            EncabezadoBearer.EsBearer(contexto.Request.Headers.Authorization.ToString());
        if (!sensible) { await _siguiente(contexto); return; }
        contexto.Response.Headers.CacheControl = "no-store";
        if (!contexto.Request.IsHttps)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new { codigo = "httpsRequerido", mensaje = "Se requiere HTTPS." });
            return;
        }
        try { await _siguiente(contexto); }
        catch
        {
            // Do not expose/log provider exceptions, credentials, tokens or debug pages.
            if (contexto.Response.HasStarted) { contexto.Abort(); return; }
            contexto.Response.Clear();
            contexto.Response.Headers.CacheControl = "no-store";
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await contexto.Response.WriteAsJsonAsync(new { codigo = "autenticacionNoDisponible",
                mensaje = "La autenticación no está disponible. Intentá nuevamente." });
        }
    }
}
