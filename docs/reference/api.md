# API implementada: scaffold y sesiones locales

La referencia del **esquema real** es el documento OpenAPI generado por la aplicación en **Development**, en `/openapi/v1.json`. No se mantiene una copia manual de ese esquema aquí. Para iniciar el servicio seguí el [tutorial](../tutorials/first-local-run.md) y para elegir host/puerto consultá [cliente/API](../how-to/client-api-configuration.md).

Ambos proyectos .NET declaran `net10.0`; la API utiliza `Microsoft.AspNetCore.OpenApi` `10.0.12`.

## Superficie actual

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /WeatherForecast` | Ejemplo de plantilla: devuelve cinco pronósticos con fechas de los próximos días, temperaturas y resúmenes aleatorios; no consulta un servicio meteorológico ni datos del transporte. |
| `GET /openapi/v1.json` | Documento generado; disponible solo en Development, por `MapOpenApi()` dentro de la condición de entorno. |
| `POST /api/autenticacion/ingreso` | HTTPS, cuerpo `SolicitudIngresoDto`: `200` con identificador público opaco, roles, `token` y `venceEnUtc`; `400` DataAnnotations; `401` rechazo genérico y `WWW-Authenticate: Bearer`. |
| `POST /api/autenticacion/salida` | HTTPS y Bearer válido: `204`, revoca exclusivamente esa sesión. |
| `GET /api/autenticacion/identidad` | HTTPS y Bearer válido: `200`, identificador público y roles actuales, sin token. |
| `GET /api/autenticacion/prueba-pasajero` | HTTPS y Bearer válido con rol `Pasajero`: `200`, identidad; prueba de permiso de solo lectura. |
| `GET /api/autenticacion/prueba-administrador` | HTTPS y Bearer válido con rol `Administrador`: `200`, identidad; pasajero sin ese rol recibe `403`. |

Las rutas protegidas sin sesión válida reciben `401` con `WWW-Authenticate: Bearer`; una sesión válida con rol insuficiente recibe `403`. No mutan datos al denegar acceso. Todas las rutas de autenticación y solicitudes con esquema Bearer rechazan HTTP con `400` (`httpsRequerido`), sin redirigir ni procesar credenciales. Los fallos inesperados capturados por el límite de autenticación en estas solicitudes, incluidos los de configuración/proveedor, reciben `500` con el JSON sanitizado `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}`, sin detalles privados. Respuestas sensibles usan `Cache-Control: no-store`. Estos comportamientos están implementados en fuente; su comprobación pública es parcial: la [matriz local posterior del informe §7](../project-report.md#matriz-local-posterior-de-permisos-y-revocación-inmediata) registra **7/7 PASS** sobre HTTPS7053, con ingreso válido, identidad/permiso de pasajero, denegación de administrador, salida y rechazo inmediato del mismo token anterior, incluidos JSON/cuerpos vacíos, no-store y challenges Bearer esperados. No acredita todos los comportamientos de esta referencia.

Fuentes: [WeatherForecastController.cs](../../src/Api/API-Berruti/API-Berruti/Controllers/WeatherForecastController.cs), [WeatherForecast.cs](../../src/Api/API-Berruti/API-Berruti/WeatherForecast.cs) y [Program.cs](../../src/Api/API-Berruti/API-Berruti/Program.cs). Consultá el JSON servido para campos, tipos y respuestas; la variación aleatoria impide esperar valores exactos.

## Límites y evidencia

Las sesiones API usan Controller → interfaz Service → interfaces Repository → DbContext; autenticación Bearer opaca y autorización del framework compartido, sin JWT ni paquetes nuevos. Roles se consultan en MySQL en cada solicitud; token solo se entrega al ingresar, huella SHA-256 única en DB, vencimiento UTC absoluto de siete días y revocación por sesión. No hay rutas reales de negocio, pagos, QR ni consumidores cliente. La matriz pública local `200/204/401/403` y el rechazo inmediato del token anterior tras logout sí tienen evidencia independiente enlazada arriba; persistencia ante reinicio, revocación durable, límite de vencimiento, otras sesiones, concurrencia e invariancia de BD/contadores siguen sin prueba funcional. Los fallos de servidor/proveedor y reintento (incluido el escenario 503, fuera de esta matriz) permanecen pendientes; no se acredita un ensayo de fallos por el resultado positivo local. Ver [contratos](auth-contracts.md) y [checks pendientes](../how-to/api-session-checks.md). Los [requisitos](requirements.md) describen lo exigido/propuesto, **no esta superficie disponible**.

La verificación previa observó HTTP 200 y cinco registros en `/weatherforecast`, y HTTP 200 con OpenAPI `3.1.1` en `/openapi/v1.json`, bajo Development. Son resultados históricos atribuidos al verificador en el [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración), no pruebas nuevas ni una promesa de idéntica salida en otro entorno. En aquella verificación histórica no se probó TLS ni autenticación; no debe confundirse con la evidencia HTTPS/autenticación local posterior enlazada arriba. Mobile/iPhone y revisión de Enzo siguen pendientes; no hay cierre del backlog ni autorización de producción.
