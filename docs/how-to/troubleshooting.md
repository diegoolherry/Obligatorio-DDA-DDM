# Resolver problemas del arranque local

Aplicá solo el caso que coincida con tu error. Para el camino normal seguí el [tutorial](../tutorials/first-local-run.md); para puertos y alcance de red consultá [cliente/API](client-api-configuration.md).

## Windows: Expo pide ngrok aunque está instalado globalmente

**Síntoma observado:** al iniciar el túnel apareció `CommandError: Install @expo/ngrok@^4.1.0 and try again.`

En esta sesión Windows, el paquete global `@expo/ngrok@4.1.3` podía cargarse, pero el resolver exacto de Expo daba `MODULE_NOT_FOUND`. La consulta del prefijo global mediante `npm.cmd` fallaba con `spawn EINVAL` y omitía la raíz global real. Es evidencia de este entorno (Node `24.11.1`, npm `11.19.0`, Expo `57.0.27`), **no un diagnóstico universal de cualquier error ngrok** ni evidencia de incompatibilidad de todas esas versiones.

Si el paquete global ya está disponible y coincide el problema de resolución, este workaround **temporal fue verificado en la sesión**. En PowerShell, desde la raíz:

```powershell
cd src/Mobile
$previousNodePath = $env:NODE_PATH
try {
    $env:NODE_PATH = (npm root -g).Trim()
    npx expo start --tunnel
} finally {
    $env:NODE_PATH = $previousNodePath
}
```

Esperá el QR del túnel, conectá el cliente Expo compatible y al terminar presioná Ctrl+C; al salir del bloque se restaura el valor anterior de `NODE_PATH`. No modifica settings permanentes ni archivos del proyecto. Si `npm root -g` falla o falta el paquete, este caso no queda resuelto por el workaround: conservá el error para diagnosticarlo con el equipo, sin reinstalaciones especulativas.

El usuario escaneó el QR y reportó «perfecto, anda». Eso acredita un **arranque en dispositivo reportado por la persona**, no una observación del agente en celular, una revisión visual de insets ni integración con la API. Los intentos independientes anteriores de Metro que agotaron el tiempo siguen siendo historia válida; ver [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración).

## .NET: SDK o solución no reconocidos

Comprobá `dotnet --list-sdks` y que exista un SDK .NET 10 compatible con `net10.0` y `.slnx`. Ejecutá desde las rutas del [tutorial](../tutorials/first-local-run.md); `API-Berruti` y `Web-App` no son proyectos en la raíz. No cambies frameworks ni recrees soluciones para ocultar un error de entorno.

## .NET: advertencia de redirección HTTPS en arranque HTTP

`Failed to determine the https port for redirect` se observó en la verificación HTTP con overrides y sin perfil de lanzamiento. Ambos `Program.cs` conservan redirección HTTPS. En esa ejecución, las respuestas HTTP comprobadas igualmente fueron 200; eso no prueba TLS. Revisá el perfil y las URLs de escucha con [cliente/API](client-api-configuration.md), sin eliminar middleware ni confiar certificados como solución automática. El perfil HTTPS y sus certificados requieren una comprobación aparte.

## Mobile: auditoría o Metro sin confirmación

La auditoría histórica pendiente registró 22 paquetes afectados (15 altos, 7 moderados), con tres avisos distintos, descritos en el [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración); no es una auditoría nueva. No se aplicó `npm audit fix`; un aviso no demuestra por sí solo explotabilidad ni explica el timeout histórico de Metro. Conservá logs, entorno y resultado observado; no declares readiness por logs sin verificar conexión. Detené solo los servidores que iniciaste y evitá cambios de paquetes o settings hasta tener un diagnóstico acotado.
