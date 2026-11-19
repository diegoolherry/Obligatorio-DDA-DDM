# API implementada: scaffold y autenticación JWT local

La referencia del **esquema real** es el documento OpenAPI generado por la aplicación en **Development**, en `/openapi/v1.json`. No se mantiene una copia manual de ese esquema aquí. Para iniciar el servicio seguí el [tutorial](../tutorials/first-local-run.md) y para elegir host/puerto consultá [cliente/API](../how-to/client-api-configuration.md).

Ambos proyectos .NET declaran `net10.0`; la API utiliza `Microsoft.AspNetCore.OpenApi` `10.0.12`.

## Superficie actual

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /WeatherForecast` | Ejemplo de plantilla: devuelve cinco pronósticos con fechas de los próximos días, temperaturas y resúmenes aleatorios; no consulta un servicio meteorológico ni datos del transporte. |
| `GET /openapi/v1.json` | Documento generado; disponible solo en Development, por `MapOpenApi()` dentro de la condición de entorno. |
| `POST /api/autenticacion/ingreso` | HTTPS, cuerpo `SolicitudIngresoDto`: `200` con identificador público opaco, roles, `token` (JWT HS256, 7 días) y `venceEnUtc` (= `exp`); `400` DataAnnotations; `401` rechazo genérico y `WWW-Authenticate: Bearer`. |
| `POST /api/autenticacion/salida` | HTTPS y Bearer válido: `204` **sin estado en el servidor**; no revoca el token, que sigue válido hasta `exp`. El cliente lo descarta. |
| `GET /api/autenticacion/identidad` | HTTPS y Bearer válido: `200`, identificador público y roles actuales, sin token. |
| `GET /api/autenticacion/prueba-pasajero` | HTTPS y Bearer válido con rol `Pasajero`: `200`, identidad; prueba de permiso de solo lectura. |
| `GET /api/autenticacion/prueba-cobrador` | HTTPS y Bearer válido con rol `Cobrador`: `200`, identidad; prueba de permiso de solo lectura, no validación de tickets. Sin comprobación pública/MySQL todavía. |
| `GET /api/autenticacion/prueba-administrador` | HTTPS y Bearer válido con rol `Administrador`: `200`, identidad; pasajero sin ese rol recibe `403`. |

Las rutas protegidas sin JWT válido (ausente, alterado, firmado con otra clave, de otro emisor/audiencia o vencido) reciben `401` con `WWW-Authenticate: Bearer`; un JWT válido con rol insuficiente recibe `403`. Los roles salen del token emitido al ingresar: un cambio de rol o la desactivación de la cuenta rigen desde el próximo ingreso o el vencimiento. No mutan datos al denegar acceso. Todas las rutas de autenticación y solicitudes con esquema Bearer rechazan HTTP con `400` (`httpsRequerido`), sin redirigir ni procesar credenciales. Los fallos inesperados capturados por el límite de autenticación en estas solicitudes, incluidos los de configuración/proveedor, reciben `500` con el JSON sanitizado `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}`, sin detalles privados. Respuestas sensibles usan `Cache-Control: no-store`. Estos comportamientos están implementados en fuente; su comprobación pública es parcial: la [matriz local posterior del informe §7](../project-report.md#matriz-local-posterior-de-permisos-y-revocación-inmediata) registra **7/7 PASS** sobre HTTPS7053, pero se ejecutó contra el **mecanismo anterior** (token opaco con revocación) y **no** acredita el JWT; en particular, su rechazo del token tras la salida ya no existe. No hay todavía una matriz pública equivalente con JWT.

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
| `GET/POST/PUT/DELETE /api/administracion/servicios[/{id}]` | CRUD de `ServicioDto`: tramo, `salida` y `llegada` (fecha y hora local de Uruguay, sin conversión UTC; la llegada debe ser posterior a la salida y puede caer al día siguiente), capacidad y política `SinAsiento` o `AsignacionAutomatica`; `201/200/204`, validación `400`, ausencia `404`. `GET` acepta el parámetro opcional `fecha=yyyy-MM-dd` (día local de la `salida`): con él devuelve solo los servicios de ese día, ordenados por `salida`, y `400` si el formato no es válido; sin él devuelve todos, como antes. |
| `GET/POST/PUT/DELETE /api/administracion/tarifas[/{id}]` | CRUD de `TarifaDto`: tramo, importe y vigencia; `201/200/204`, validación `400`, ausencia `404`. |

Si otra sesión MySQL retiene la fila, las escrituras `PUT`/`DELETE` de paradas, tramos y servicios (y la generación de Servicios) responden `409` en unos 3 s en vez de quedar sin respuesta; ver [problemas conocidos](../how-to/troubleshooting.md#api-una-escritura-queda-sin-respuesta-cuando-otra-sesión-retiene-filas) (FIN-012).

Los datos operativos de demostración no se cargan por la API: según el código de [`ReinicioLocal`](../../src/Api/API-Berruti/API-Berruti/Data/ReinicioLocal.cs) y [`EscenarioOperativoDemo`](../../src/Api/API-Berruti/API-Berruti/Data/EscenarioOperativoDemo.cs), recrear la base de Development con [`scripts/reset-local-db.ps1`](../../scripts/reset-local-db.ps1) siembra 3 paradas, 2 tramos, 2 servicios y 2 tarifas para el día siguiente al reinicio. Esto describe el código; no es evidencia de una ejecución contra MySQL.

### Comprobación local de autorización operativa

El script fuente [`scripts/check-operational-authorization.ps1`](../../scripts/check-operational-authorization.ps1) prepara una matriz HTTP `GET` para las cuatro listas. Para ejecutarlo, la API debe estar disponible en HTTPS de loopback y el entorno local debe estar autorizado y configurado; el script solicita la contraseña demo con entrada segura, no la guarda, no relaja la validación TLS y no imprime credenciales, tokens ni cuerpos de error.

```powershell
.\scripts\check-operational-authorization.ps1 -BaseUrl https://localhost:7053
```

Se esperan `401` anónimo, `403` para Pasajero/Cobrador y `200` para Administrador en cada lista. Solo emite tokens de ingreso y el script llama a `salida` (`204`, sin estado) en `finally`; **no demuestra** CRUD, persistencia MySQL ni ausencia de fallos fuera de esta matriz. El script no fue ejecutado; ninguna de estas respuestas se presenta como resultado observado.

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

## Intentos de compra (corte HTTP)

Los tres endpoints requieren Bearer válido con rol `Pasajero`. El pasajero se obtiene de la identidad autenticada del servidor; el cliente no envía ni elige el usuario propietario.

| Método y ruta | Contrato basado en el controller |
| --- | --- |
| `POST /api/intentos-compra` | Cuerpo `IniciarIntentoCompraRequestDto` (`tramos`: selecciones ordenadas con `orden` y `servicioId`). `201` con `IniciarIntentoCompraResponseDto` y `Location` al recurso; `400` por solicitud inválida; `401` sin identidad; `404` si falta algún servicio; `409` cuando no se pudo reservar el asiento solicitado; `500` genérico ante error inesperado. |
| `GET /api/intentos-compra/{intentoId}/cotizacion` | Recalcula la tarifa vigente para la secuencia exacta del intento del pasajero autenticado. `200` devuelve `CotizarCompraResponseDto` con cotización original/actual, `cambioPrecio`, `huellaCotizacion` y vencimiento original; `400` entrada inválida; `401` sin identidad; `404` intento ausente/ajeno; `409` hold vencido o selección no disponible; `500` error genérico. La huella SHA-256 determinista liga intento, vencimiento y precios actuales para comparar una futura aceptación; no es firma ni autorización de pago, y el endpoint no recibe importes del cliente.
| `GET /api/intentos-compra/{intentoId}` | Devuelve `IniciarIntentoCompraResponseDto` solo cuando el servicio encuentra el intento bajo la identidad del pasajero actual. `200`, `400` por identificador inválido, `401` sin identidad, `404` si no existe o pertenece a otra identidad, y `500` genérico ante error inesperado. |

El `201` incluye `intentoId`, `itinerario` y `holdVenceEn` UTC, según el DTO vigente. El límite HTTP propaga la cancelación y traduce errores esperados sin devolver detalles de excepciones internas. Esta referencia describe el código y el harness determinista llama directamente al controller: todavía no se ejecutó una solicitud HTTP integrada, no se inició la API y no se consultó MySQL; los resultados `401/403`, encabezados Bearer, serialización y `Location` a nivel de servidor no se presentan como comprobados en runtime.

El cliente debe consultar la cotización y enviar únicamente su huella tras la aceptación explícita del pasajero. Una respuesta `409` por cambio requiere volver a consultar la cotización vigente y solicitar una nueva aceptación; no se amplía el hold. Los resultados de los controladores se prueban con harness determinista, no mediante una solicitud HTTP integrada ni API iniciada.

### Inicio y consulta de pagos

Los endpoints de intentos y pagos requieren Bearer válido con rol `Pasajero`. La identidad del pasajero se obtiene del servidor; el cliente no envía ni elige al propietario.

| Método y ruta | Contrato basado en el controller |
| --- | --- |
| `POST /api/intentos-compra/{intentoId}/pagos` | Cuerpo `IniciarPagoRequestDto` con solo `huellaCotizacionAceptada`; usa cotización y precio del servidor. `200` devuelve pago existente o resultado conocido; `202` indica resultado `Desconocido` que requiere reconciliación; `400` solicitud inválida; `401` sin identidad; `404` intento ausente/ajeno; `409` cotización cambió (devuelve la actual para aceptación explícita) o hold vencido/no disponible; `500` genérico. No confirma compra ni emite tickets/QR. |
| `GET /api/intentos-compra/{intentoId}/pagos/actual` | `200` devuelve `PagoResponseDto` del intento autenticado; `400` identificador inválido; `401` sin identidad; `404` intento ausente/ajeno o sin pago; `500` genérico. La URL de checkout solo se expone si el pago está pendiente y el hold original sigue vigente. |
| `POST /api/intentos-compra/{intentoId}/pagos/sincronizar` | Sin cuerpo: consulta la orden de proveedor guardada; si falta el ID porque se perdió la respuesta a la creación, busca por la referencia externa PG persistida desde la creación hasta UTC actual y acepta únicamente un resultado completo, único y coincidente. `200` devuelve el pago y solo incluye compra/tickets cuando la confirmación se persistió; `202` indica estado desconocido, búsqueda vacía o ambigua; `400` solicitud inválida; `401` sin identidad; `404` intento ausente/ajeno/sin pago; `500` genérico. Nunca vuelve a crear la orden ni acepta estado del cliente; la búsqueda no inventa una URL de checkout. Una aprobación tardía o sin disponibilidad solicita devolución completa idempotente y verifica después el estado; si queda pendiente, el pago sigue en seguimiento y no se emiten tickets. |


Una respuesta `Desconocido` no confirma el pago. La sincronización consulta la orden y solo puede confirmar la compra si valida pago aprobado por el proveedor, importe completo, hold original vigente y disponibilidad de asientos bajo la transacción de confirmación; después persiste compra, tramos y tickets juntos. El cliente Web abre exclusivamente la `checkoutUrl` HTTPS devuelta por la API y, al regresar manualmente a la misma pantalla, sincroniza el mismo intento; no crea otro hold/pago automáticamente ni confía en el retorno del navegador como prueba de aprobación. La pantalla de éxito muestra compra y tickets por tramo; consultar o mostrar los QR y validarlos pertenecen a MVP-005. Una aprobación tardía o sin disponibilidad solicita devolución completa idempotente y verifica después el estado; si queda pendiente, el pago sigue en seguimiento y no se emiten tickets. Los harnesses deterministas usan dobles de persistencia/proveedor y no acreditan middleware HTTP, API iniciada, MySQL, ejecución transaccional ni proveedor real.

## Catálogo de horarios (FIN-001)

Transcripción del horario publicado en <http://www.berruttiturismo.com.uy/horarios.html> («Horarios de invierno 2026»), versionada en [`horarios-invierno-2026.json`](../../src/Api/API-Berruti/API-Berruti/Data/Catalogo/horarios-invierno-2026.json) y sembrada por el reinicio local. Documentación basada en fuente y en `tests/RouteCatalog` (en memoria), no en una prueba HTTP contra MySQL.

| Método y ruta | Acceso | Comportamiento establecido en fuente |
| --- | --- | --- |
| `GET /api/catalogo` | Anónimo | `200` con bloques (vigencia, empresa, `fuenteUrl`), rutas dirigidas con paradas ordenadas y salidas por tipo de día, más feriados y excepciones. |
| `GET /api/catalogo/salidas?fecha=yyyy-MM-dd` | Anónimo | `200` con las salidas de ese día por bloque y su `estado` (`opera`, `fueraDeVigencia`, `calendarioNoCargado`, `sinServicio`, `consultarAgencia`); `400` si falta la fecha o no tiene ese formato. |
| `POST /api/administracion/catalogo/servicios` | `Administrador` | Genera `Servicio` fechados. Cuerpo: `desde`, `hasta` (hasta 31 días), `capacidad` (≥ 1) y `politicaAsiento`, todos obligatorios. `200` con `serviciosCreados`, `serviciosExistentes` y `tramosCreados`; `400` con el motivo; `409` si otra generación guardó lo mismo a la vez. |

Reglas de calendario, en este orden: fuera de la vigencia publicada del bloque no hay salidas; en un año sin feriados cargados no hay salidas y el motivo lo dice («No está cargado el calendario de feriados de 2027.», decisión de Diego); una excepción «sin servicio» (1/1, 1/5, 25/12) gana sobre «consultar agencia» (Semana de Turismo, 30/3–5/4/2026); domingo o feriado usa «Domingos y feriados»; sábado usa «Sábados» y «Lunes a sábado»; lunes a viernes usa «Lunes a viernes», «Lunes a sábado» y, lunes y viernes, «Lunes y viernes». La vigencia 02/03/26–20/12/26 es solo del bloque Nueva Palmira / Colonia. Feriados: tabla por año con los no laborables de 2026.

La generación crea un `Servicio` por cada par de paradas con hora publicada en ambas (por ejemplo, la salida de 05:20 de Nueva Palmira genera Nueva Palmira→Carmelo, →Radial y →Colonia, y así con cada parada siguiente). Las paradas sin hora (Polancos, los alimentadores de Ombúes y Conchillas, Nueva Palmira / Carmelo y Agraciada) no generan servicios. Reutiliza el `Tramo` existente de cada par o lo crea. No crea tarifas: un servicio generado aparece en la búsqueda recién cuando su tramo tiene una tarifa vigente. Repetir la generación no duplica: lo impide el índice único `(salida_catalogo_id, tramo_id, salida)`.

## Administración del catálogo (FIN-008)

Todas las rutas exigen el rol `Administrador`: `401` sin sesión y `403` con otro rol. Documentación basada en fuente y en `tests/RouteCatalog` (en memoria).

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /api/administracion/catalogo` | `200` con los bloques activos y sus `id`, rutas activas (paradas ordenadas) y salidas activas. Cada salida informa `horas` (`orden`, `hora`) y `serviciosGenerados`. |
| `POST /api/administracion/catalogo/bloques` · `PUT …/bloques/{id}` | Cuerpo: `clave` (hasta 16), `nombre`, `empresa`, `fuenteUrl` opcional, `vigenteDesde` y `vigenteHasta` opcionales. `201` o `200` con el bloque; `400` con el motivo; `409` si la clave ya existe. |
| `POST /api/administracion/catalogo/rutas` · `PUT …/rutas/{id}` | Cuerpo: `bloqueId` (activo), `clave`, `familia`, `nombre` y `paradaIds` en orden de recorrido. Necesita al menos dos paradas existentes, sin repetir la misma de forma consecutiva; el orden se guarda como 1..n. `409` si la clave ya existe, o si cambian las paradas mientras la ruta tiene salidas activas. |
| `POST /api/administracion/catalogo/salidas` · `PUT …/salidas/{id}` | Cuerpo: `rutaId` (activa), `dias`, `codigos` y `nota` opcionales, y `horas` (`orden`, `hora`). Necesita al menos dos horas; la parada de orden 1 debe tener hora, cada orden corresponde a una parada de la ruta y aparece una vez, y las horas son estrictamente crecientes. Una salida no cambia de ruta. |
| `DELETE …/bloques/{id}` · `…/rutas/{id}` · `…/salidas/{id}` | `204`: baja lógica (`activo = 0`). `404` si no existe o ya fue dado de baja. |
| `POST /api/administracion/catalogo/feriados` · `PUT …/feriados/{yyyy-MM-dd}` · `DELETE …/feriados/{yyyy-MM-dd}` | Cuerpo: `fecha` (solo en `POST`; en `PUT` va en la ruta y no cambia) y `nombre`. `201`, `200` o `204`; `400` con el motivo; `404` si no existe; `409` si ya hay un feriado esa fecha. |
| `POST /api/administracion/catalogo/excepciones` · `PUT …/excepciones/{id}` · `DELETE …/excepciones/{id}` | Cuerpo: `bloqueId` (activo, o `null` para todos los bloques), `desde`, `hasta` (no anterior a `desde`), `tipo` (`sinServicio` o `consultarAgencia`) y `motivo`. `201`, `200` o `204`; `400` con el motivo; `404` si no existe o es de un bloque dado de baja. |

El listado de administración incluye `feriados` y `excepciones` (con `id`). Feriados y excepciones no tienen servicios que dependan de ellos: se eliminan de verdad, y el cambio se ve en las salidas del día y en la próxima generación.

Política de baja (decisión de Diego, 2026-11-17): el elemento dado de baja deja de aparecer en `GET /api/catalogo`, en las salidas del día y en la administración, y no genera más `Servicio`. Los servicios ya generados se conservan. Un bloque dado de baja oculta sus rutas y excepciones; una ruta, sus salidas. No hay reactivación. Editar las paradas o las horas no cambia los servicios ya generados: aplica a las generaciones siguientes. Cambiar las paradas de una ruta exige dar de baja antes sus salidas activas, porque las horas se guardan por orden de parada. Las validaciones rechazan sin guardar nada. Migración `20261117193916_BajaLogicaCatalogo` ([SQL](../../scripts/database/20261117193916_BajaLogicaCatalogo.sql)).

Página Web (FIN-008-03): `/admin/catalogo` (enlace «Catálogo de rutas» en el menú, solo con sesión) usa estos endpoints y `GET /api/administracion/paradas`. Antes de mostrarse comprueba el rol `Administrador` con la API. Gestiona bloques, rutas (paradas ordenadas: agregar, quitar, subir y bajar; bloqueadas si la ruta tiene salidas activas), salidas (hora por parada; la primera es obligatoria), feriados y excepciones. Las bajas piden confirmación: bloques, rutas y salidas son baja lógica y feriados y excepciones se eliminan de verdad. Editar una ruta o una salida con Servicios generados advierte que el cambio aplica solo a futuras generaciones. Los `400` y `409` muestran el motivo que devuelve la API (solo cuerpos de texto, hasta 300 caracteres); el `404` muestra «El elemento no existe o ya fue dado de baja». Comprobado con pruebas del cliente en `tests/WebAuthentication` y por inspección de la fuente; el recorrido en navegador queda descrito en el PR.

## Ubicación de demostración (MVP-009)

El dispositivo embarcado es el celular del cobrador: con su sesión `Cobrador` publica la posición del servicio en el que viaja, según la asignación `cobrador_servicio` que siembra el reinicio local (el cobrador demo queda asignado a los dos servicios demo). Los parámetros son académicos y ficticios, en la sección `Ubicacion` de [`appsettings.json`](../../src/Api/API-Berruti/API-Berruti/appsettings.json): `PeriodicidadSegundos`, `MargenPeriodicidadSegundos`, `UmbralDesactualizacionSegundos`, `VelocidadPromedioKmH`, `ToleranciaDemoraMinutos`, `ZonaHoraria` y `Destinos` (coordenadas por nombre de parada de destino). Si algún valor es inválido, la API no arranca.

| Método y ruta | Comportamiento establecido en fuente |
| --- | --- |
| `GET /api/ubicacion/cobrador/servicios` | Rol `Cobrador`: lista `ServicioUbicacionDto` (origen, destino, salida y llegada locales) de los servicios asignados a ese usuario. |
| `POST /api/ubicacion/servicios/{servicioId}/posiciones` | Rol `Cobrador`, cuerpo `PublicarPosicionRequest` (`latitud` y `longitud`, hasta 6 decimales): `201` con `PublicacionPosicionResponse` (recepción UTC del servidor, `intervaloSegundos` desde la anterior y `cumplePeriodicidad` contra periodicidad + margen; ambos `null` en la primera); `403` si el cobrador no está asignado; `404` si el servicio no existe; `400` si falta o está fuera de rango una coordenada. Un rechazo no guarda la posición ni reemplaza la última. |
| `GET /api/ubicacion/servicios` | Rol `Pasajero`: lista los servicios de demostración. |
| `GET /api/ubicacion/servicios/{servicioId}` | Rol `Pasajero`: `200` con `ConsultaUbicacionResponse`: última posición y su antigüedad, `desactualizada` si supera el umbral, `llegadaAproximada` (hora local) y `estadoDemora` (`enHorario` o `conDemora`) solo con posición vigente y destino configurado; si no, `sinDatos` con `motivo`. Incluye los parámetros usados y `datosFicticios: true`. `404` si el servicio no existe. |

La llegada aproximada es la distancia en línea recta (haversine) al destino dividida por la velocidad promedio; hay demora si supera la llegada programada más la tolerancia. No es una predicción operativa. Las pruebas `dotnet run --project tests/Geolocation/Geolocation.csproj --no-launch-profile` cubren la publicación, los rechazos sin escrituras, la consulta y los roles exigidos con un repositorio en memoria; no ejercitan HTTP ni MySQL.

Para simular el celular del cobrador sin la app, [`scripts/publish-demo-position.ps1`](../../scripts/publish-demo-position.ps1) pide la contraseña demo con entrada segura, ingresa como cobrador, lista sus servicios y publica `-Cantidad` posiciones hacia el `-Destino` cada `-Segundos`, y luego llama a `salida` (`204`; el JWT no se revoca en el servidor). No imprime credenciales ni tokens. Requiere la base reiniciada con la asignación sembrada:

```powershell
.\scripts\publish-demo-position.ps1 -Destino Colonia -Latitud -34.1928 -Longitud -57.8400 -Cantidad 3 -Segundos 30
```

## Límites y evidencia

La autenticación API usa Controller → interfaz Service → interfaces Repository → DbContext y JWT Bearer sin estado (FIN-005, decisión de Diego del 2026-11-18; detalle y riesgo aceptado en [contratos](auth-contracts.md#decisiones-de-fin-005-jwt-y-hash-de-contraseñas)): HS256, emisor, audiencia, clave (`Jwt__Key` local, nunca versionada) y vencimiento de siete días validados; contraseñas solo como hash de `IPasswordHasher<Usuario>`. El rol se lee de MySQL al ingresar y viaja en el token; **cerrar sesión no revoca nada en el servidor**. La Web consume las rutas operativas documentadas arriba e incorpora el flujo de compra descrito: la capa de compra reutiliza el JWT del circuito autenticado; sus harnesses son deterministas, no verificación HTTP/MySQL integrada. El corte API incorpora intentos y holds, consulta de cotización, inicio de pago con una orden Checkout Pro, consulta/sincronización del pago y confirmación condicionada de compra con emisión de tickets. La matriz pública local `200/204/401/403` enlazada arriba corresponde al mecanismo anterior; con JWT, el recorrido HTTPS/MySQL real sigue pendiente, igual que el límite de vencimiento, la concurrencia y la aplicación efectiva de invariantes de BD/contadores. Los contratos deterministas cubren estados y errores con dobles; no son evidencia de llamadas reales al proveedor ni de comportamiento de MySQL. Ver [contratos](auth-contracts.md) y [checks pendientes](../how-to/api-session-checks.md). Los [requisitos](requirements.md) describen lo exigido/propuesto, **no esta superficie disponible**.

La verificación previa observó HTTP 200 y cinco registros en `/weatherforecast`, y HTTP 200 con OpenAPI `3.1.1` en `/openapi/v1.json`, bajo Development. Son resultados históricos atribuidos al verificador en el [informe §7](../project-report.md#7-calidad-pruebas-y-gestión-de-configuración), no pruebas nuevas ni una promesa de idéntica salida en otro entorno. En aquella verificación histórica no se probó TLS ni autenticación; no debe confundirse con la evidencia HTTPS/autenticación local posterior enlazada arriba. Mobile/iPhone y revisión de Enzo siguen pendientes; no hay cierre del backlog ni autorización de producción.
