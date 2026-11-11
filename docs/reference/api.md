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
| `GET /api/autenticacion/prueba-cobrador` | HTTPS y Bearer válido con rol `Cobrador`: `200`, identidad; prueba de permiso de solo lectura, no validación de tickets. Sin comprobación pública/MySQL todavía. |
| `GET /api/autenticacion/prueba-administrador` | HTTPS y Bearer válido con rol `Administrador`: `200`, identidad; pasajero sin ese rol recibe `403`. |

Las rutas protegidas sin sesión válida reciben `401` con `WWW-Authenticate: Bearer`; una sesión válida con rol insuficiente recibe `403`. No mutan datos al denegar acceso. Todas las rutas de autenticación y solicitudes con esquema Bearer rechazan HTTP con `400` (`httpsRequerido`), sin redirigir ni procesar credenciales. Los fallos inesperados capturados por el límite de autenticación en estas solicitudes, incluidos los de configuración/proveedor, reciben `500` con el JSON sanitizado `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}`, sin detalles privados. Respuestas sensibles usan `Cache-Control: no-store`. Estos comportamientos están implementados en fuente; su comprobación pública es parcial: la [matriz local posterior del informe §7](../project-report.md#matriz-local-posterior-de-permisos-y-revocación-inmediata) registra **7/7 PASS** sobre HTTPS7053, con ingreso válido, identidad/permiso de pasajero, denegación de administrador, salida y rechazo inmediato del mismo token anterior, incluidos JSON/cuerpos vacíos, no-store y challenges Bearer esperados. No acredita todos los comportamientos de esta referencia.

Fuentes: [WeatherForecastController.cs](../../src/Api/API-Berruti/API-Berruti/Controllers/WeatherForecastController.cs), [WeatherForecast.cs](../../src/Api/API-Berruti/API-Berruti/WeatherForecast.cs) y [Program.cs](../../src/Api/API-Berruti/API-Berruti/Program.cs). Consultá el JSON servido para campos, tipos y respuestas; la variación aleatoria impide esperar valores exactos.

## Administración operativa

Las rutas operativas están protegidas con `[Authorize(Roles = "Administrador")]` y usan DTOs tipados. Esta tabla resume los controllers y contratos actuales; es documentación basada en fuente, no evidencia de una prueba HTTP integrada ni de persistencia MySQL.

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /api/administracion/paradas` | Lista `ParadaDto` para Administrador. |
| `POST /api/administracion/paradas` | Crea desde `ParadaDto`: `201` o `400` por validación. |
| `PUT /api/administracion/paradas/{id}` | Actualiza: `200`, `400` para datos esperados inválidos o `404` si falta. |
| `DELETE /api/administracion/paradas/{id}` | Elimina: `204`, `404` si falta o `409` cuando la parada está referenciada por un tramo. |
| `GET/POST/PUT/DELETE /api/administracion/tramos[/{id}]` | CRUD de `TramoDto`, con origen/destino ligados a paradas; `201/200/204`, validación `400`, ausencia `404` y restricción referencial `409`. |
| `GET/POST/PUT/DELETE /api/administracion/servicios[/{id}]` | CRUD de `ServicioDto`: tramo, `salida` y `llegada` (fecha y hora local de Uruguay, sin conversión UTC; la llegada debe ser posterior a la salida y puede caer al día siguiente), capacidad y política `SinAsiento` o `AsignacionAutomatica`; `201/200/204`, validación `400`, ausencia `404`. |
| `GET/POST/PUT/DELETE /api/administracion/tarifas[/{id}]` | CRUD de `TarifaDto`: tramo, importe y vigencia; `201/200/204`, validación `400`, ausencia `404`. |

Los datos operativos de demostración no se cargan por la API: según el código de [`ReinicioLocal`](../../src/Api/API-Berruti/API-Berruti/Data/ReinicioLocal.cs) y [`EscenarioOperativoDemo`](../../src/Api/API-Berruti/API-Berruti/Data/EscenarioOperativoDemo.cs), recrear la base de Development con [`scripts/reset-local-db.ps1`](../../scripts/reset-local-db.ps1) siembra 3 paradas, 2 tramos, 2 servicios y 2 tarifas para el día siguiente al reinicio. Esto describe el código; no es evidencia de una ejecución contra MySQL.

### Comprobación local de autorización operativa

El script fuente [`scripts/check-operational-authorization.ps1`](../../scripts/check-operational-authorization.ps1) prepara una matriz HTTP `GET` para las cuatro listas. Para ejecutarlo, la API debe estar disponible en HTTPS de loopback y el entorno local debe estar autorizado y configurado; el script solicita la contraseña demo con entrada segura, no la guarda, no relaja la validación TLS y no imprime credenciales, tokens ni cuerpos de error.

```powershell
.\scripts\check-operational-authorization.ps1 -BaseUrl https://localhost:7053
```

Se esperan `401` anónimo, `403` para Pasajero/Cobrador y `200` para Administrador en cada lista. Solo se crean sesiones de autenticación y el script intenta revocarlas siempre en `finally`; **no demuestra** CRUD, persistencia MySQL ni ausencia de fallos fuera de esta matriz. El script no fue ejecutado; ninguna de estas respuestas se presenta como resultado observado.

## Consulta pública de itinerarios

Estas rutas son anónimas (`[AllowAnonymous]`) y de solo lectura: el pasajero busca sin sesión; la compra exigirá autenticación. Es documentación basada en fuente y en las comprobaciones ejecutables de `tests/OperationalAdministration`, no evidencia de una prueba HTTP contra MySQL.

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /api/paradas` | Lista `ParadaDto` (`id`, `nombre`) para elegir origen y destino. |
| `GET /api/itinerarios?origenId={id}&destinoId={id}&fecha=yyyy-MM-dd` | `200` con una lista de `ItinerarioDto`, vacía si no hay coincidencias; `400` si falta un parámetro, la fecha no tiene el formato `yyyy-MM-dd` o las paradas son iguales; `404` si una parada no existe. |

Reglas de la búsqueda (decisiones de producto de MVP-003):

- Devuelve todas las combinaciones directas (un tramo) y de dos tramos, ordenadas por salida y después por llegada.
- Ambos servicios salen en la fecha buscada, en hora local de Uruguay; la llegada puede caer al día siguiente.
- El transbordo se hace en la misma parada, con una espera de 5 a 30 minutos, ambos inclusive. Las excepciones operativas, como accidentes o demoras, quedan fuera de alcance.
- Cada tramo usa la tarifa vigente en la fecha (`vigenteDesde ≤ fecha ≤ vigenteHasta`); con varias vigentes usa la de `vigenteDesde` más reciente. Un itinerario con un tramo sin tarifa vigente no se ofrece.

`ItinerarioDto` contiene `salida`, `llegada`, `precioTotal`, `transbordo` (`parada`, `minutos`; `null` en un directo) y `tramos`. Cada tramo informa `orden`, `servicioId`, `tramoId`, `origen` y `destino` (`ParadaDto`), `salida`, `llegada`, `importe` y `politicaAsiento` (`sinAsiento` o `asignacionAutomatica`). Con el escenario demo, Ombúes→Colonia devuelve un itinerario: 08:00–08:30 y 08:45–10:00, con un transbordo de 15 minutos en Radial de Conchillas y 100 + 250 = 350.

## Límites y evidencia

Las sesiones API usan Controller → interfaz Service → interfaces Repository → DbContext; autenticación Bearer opaca y autorización del framework compartido, sin JWT ni paquetes nuevos. Roles se consultan en MySQL en cada solicitud; token solo se entrega al ingresar, huella SHA-256 única en DB, vencimiento UTC absoluto de siete días y revocación por sesión. La administración Web consume las rutas operativas documentadas arriba; estas rutas aún no tienen verificación HTTP/MySQL ejecutada. Compra, reserva, pagos y QR no forman parte de este corte. La matriz pública local `200/204/401/403` y el rechazo inmediato del token anterior tras logout sí tienen evidencia independiente enlazada arriba; persistencia ante reinicio, revocación durable, límite de vencimiento, otras sesiones, concurrencia e invariancia de BD/contadores siguen sin prueba funcional. Los fallos de servidor/proveedor y reintento (incluido el escenario 503, fuera de esta matriz) permanecen pendientes; no se acredita un ensayo de fallos por el resultado positivo local. Ver [contratos](auth-contracts.md) y [checks pendientes](../how-to/api-session-checks.md). Los [requisitos](requirements.md) describen lo exigido/propuesto, **no esta superficie disponible**.

La verificación previa observó HTTP 200 y cinco registros en `/weatherforecast`, y HTTP 200 con OpenAPI `3.1.1` en `/openapi/v1.json`, bajo Development. Son resultados históricos atribuidos al verificador en el [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración), no pruebas nuevas ni una promesa de idéntica salida en otro entorno. En aquella verificación histórica no se probó TLS ni autenticación; no debe confundirse con la evidencia HTTPS/autenticación local posterior enlazada arriba. Mobile/iPhone y revisión de Enzo siguen pendientes; no hay cierre del backlog ni autorización de producción.
