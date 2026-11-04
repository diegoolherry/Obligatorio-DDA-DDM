# Comprobar una sesión a través de dos reinicios de API

## Qué demuestra y qué no

Este **diagnóstico temporal, no UI de la aplicación**, prepara T12/S5–S6,
S9 y S15–S16. La ejecución local observada por el padre se registra abajo.
La autorización inicial manual fue reemplazada por «Sí, automatizá los clics y
los dos reinicios», limitada a esta API en localhost:7053; iPhone queda diferido.
No modifica la política nativa de siete días, aplicación, esquema ni configuración.

El resultado acredita únicamente esta secuencia condicional con el mismo token:

| Acción del operador autorizado | Solicitudes del panel | Comprobación |
| --- | --- | --- |
| Ingresar una vez | POST ingreso; GET prueba-pasajero | 200 JSON/no-store; misma identidad y roles |
| Pausa 1: detener y arrancar API; confirmar | GET prueba-pasajero; POST salida | 200 JSON/no-store con mismo token; 204 vacío/no-store |
| Pausa 2: detener y arrancar API; confirmar | GET prueba-pasajero | 401 vacío/no-store, challenge Bearer con mismo token anterior |

Todas las rutas llevan `/api/autenticacion/`. No hay solicitudes durante las
pausas, nuevo ingreso, matriz de otros roles, renovación ni reintentos.
**Pulsar continuar no demuestra un reinicio.** Sin evidencia humana externa de
ambas paradas/arranques (o evidencia de identidad de procesos del agente
expresamente autorizado), el éxito solo demuestra el comportamiento del mismo
token antes/después de salida. No prueba por sí solo reinicio de MySQL, seguridad de disco,
concurrencia, vencimiento exacto, invariancia de DB/contadores, otras sesiones,
Mobile/iPhone ni aprobación de Enzo. No completa el backlog.

## Preparar sin revelar credenciales

1. Coordinar con el parent la ejecución operativa separada. La preparación de
   estos archivos no ejecuta navegador, servidores, SQL, red ni procesos.
2. La persona mantiene su API local en `https://localhost:7053` con certificado
   confiable y configuración privada existente. No desactivar TLS, cambiar
   certificados, leer `.env`, ajustar MySQL ni mostrar logs privados.
3. La cuenta ficticia es `pasajero.demo@example.test`, fijada en el campo
   de solo lectura y en la solicitud de ingreso. Coincide con el bootstrap
   registrado en L45 de la [spec](../../odd/tasks/mvp-001-03-passenger-auth.md).
   No editar usuarios ni intentar sucesivos ingresos como diagnóstico.
4. En el entorno ES-module nodejs de ego-browser, ejecutar una sola vez el
   [entrypoint](../../odd/browser-checks/session-durability-20261028/install-panel.mjs).
   No es un script para ejecutar con Node CLI. Importa el
   [instalador autocontenido](../../odd/browser-checks/session-durability-20261028/session-durability-panel.mjs),
   crea/reanuda `taskSpace("ctc-session-durability-20261028")`, reutiliza `p1`,
   navega una vez a OpenAPI y entrega el control con `handOff()`.
   Solo imprime metadatos sanitizados. No ejecutar de nuevo sobre una sesión
   activa: la navegación perdería su acceso local sin confirmar revocación.
5. El instalador comprueba origen HTTPS exacto, ruta OpenAPI sin query/hash y
   documento JSON OpenAPI 3 con `paths` antes de mutar el DOM. Instalarlo no
   envía ninguna solicitud de autenticación. No persiste credenciales.

## Ejecutar con la persona presente

El modo manual sigue disponible. Solo con permiso explícito, el agente puede
pulsar los botones y efectuar los dos ciclos: verificar propietario de 7053,
nombre, ejecutable de este worktree y hora de inicio antes de cada parada;
confirmar salida/puerto cerrado y nuevo PID/arranque por el launcher existente.
No detener otros procesos ni alterar MySQL, grants, certificados o configuración.
Las etiquetas originales «manual» del panel no prueban quién operó ni requieren
cambiar el diagnóstico: registrar el actor y la evidencia externa por separado.

1. La persona escribe privadamente la contraseña en el único campo enmascarado
   y ella o el agente autorizado pulsa **Ingresar una sola vez**. No leer su valor, capturar red, cuerpos,
   headers o pantallas con secretos. El campo queda vacío y deshabilitado
   inmediatamente. Debe aparecer **PAUSA 1**, sin más solicitudes.
2. **No recargar/cerrar la pestaña.** El operador autorizado detiene realmente SOLO la API
   y vuelve a iniciarla. Registra evidencia externa de
   parada y nuevo arranque, orden y hora, sin secretos, para vincularla con
   PAUSA 1. La mera disponibilidad del puerto no prueba reinicio.
3. Solo después pulsa **Confirmo parada y arranque manual 1**. Se usa el mismo
   token, se comprueban identidad/roles y se intenta una sola salida. Debe
   aparecer **PAUSA 2**. El token anterior sigue exclusivamente en memoria.
4. El operador autorizado vuelve a detener y arrancar SOLO la API, registra la segunda
   confirmación externa y luego pulsa **Confirmo parada y arranque manual 2**.
   Solo un 401 vacío/no-store con challenge Bearer permite **ÉXITO CONDICIONAL**.
   Entonces se descartan token, identidad, roles y vencimiento locales.
5. Comunicar únicamente estados/booleanos sanitizados y las dos confirmaciones
   externas, nunca tokens, identificadores de usuario, vencimientos, contraseñas,
   respuestas o excepciones. Sin evidencia externa, informar la limitación.

Ante cualquier fallo, timeout o respuesta inesperada: **INCOMPLETO**, sesión
incierta, detenerse. No hay logout de limpieza ni retry automático. **Abandonar**
borrará el acceso local y abortará la solicitud pendiente; no confirma revocación
ni deshace un ingreso/salida que el servidor ya haya procesado. Cerrar/recargar
hace perder acceso local, no elimina una posible sesión vigente en el servidor.
No prometer borrado físico de memoria del navegador ni protección frente a un
navegador/extensión comprometido. Cada solicitud tiene timeout de ocho segundos y
cuerpo limitado a 16 KiB durante la lectura; omite cookies, rechaza redirects y
usa no-store/same-origin/no-referrer. No hay bypass TLS ni almacenamiento local.

## Resultado local observado: dos reemplazos reales de proceso

Registro documental **2026-10-28T11:18:29-03:00**, no horas exactas de las
solicitudes. Procedencia: reporte operativo del padre en
[L65–L78](../../odd/tasks/mvp-001-03-passenger-auth.md), no verificación
independiente en vivo ni ejecución de este escritor. Diagnóstico local
`4d35b5898ee4927808fd0ebd59edd8b8a3175e71`, bookkeeping `318ea56`;
33/33 sintéticas independientes y 1/1 focalizada del padre son evidencia previa.

| Paso observado, en orden | Resultado del panel validado |
| --- | --- |
| POST ingreso, cuenta `pasajero.demo@example.test` | 200, JSON esperado/no-store |
| GET prueba-pasajero, antes del primer reinicio | 200, identidad/roles esperados, JSON/no-store |
| GET prueba-pasajero, después del primer reinicio, MISMO token | 200, misma identidad/roles, JSON/no-store |
| POST salida, una sola vez | 204, vacío/no-store |
| GET prueba-pasajero, después del segundo reinicio, MISMO token anterior | 401, vacío/no-store, challenge Bearer |

| Ciclo | Parada confirmada (salida y puerto cerrado) | Nuevo proceso del mismo ejecutable API-Berruti del worktree |
| --- | --- | --- |
| 1 | PID18564, inicio 2026-10-28T10:51:47; parada 2026-10-28T11:09:34.0203225-03:00 | PID20708, inicio 2026-10-28T11:09:42.7753433-03:00, launcher job-12 |
| 2 | PID20708 revalidado; parada 2026-10-28T11:13:16.7025366-03:00 | PID400, inicio 2026-10-28T11:13:23.8899390-03:00, launcher job-15; identidad observada a 11:15:27.9614649-03:00 |

El padre verificó identidad/ruta/inicio antes de detener cada propietario de
7053. Son reinicios reales realizados por el agente con permiso, no inferidos
de clics ni atribuidos a una persona. Job-11 llegó a pausa1, job-14 a pausa2 y
job-17 terminó exit0 en `terminal-conditional-success`, con
`allActionsDisabled=true` y `passwordDisabled=true`. La evidencia externa
resuelve la condición de reinicio del panel. Misma taskSpace1/p1, sin reinstalar,
recargar, nuevo ingreso, refresh, retry ni limpieza: exactamente cinco solicitudes
auth, un ingreso y una salida. Se aguardó `task.finish({keep:[]})` una sola vez;
el espacio quedó cerrado, sin operaciones posteriores. La API quedó restaurada
en job-15/PID400: **no efectuar una tercera parada**.

La pausa «no para te paso la contraseña y la podes usar vos» detuvo operaciones;
«listo ahi esta» reanudó tras la entrada privada. El agente nunca leyó contraseña,
token, payload ni respuesta cruda: solo estados/booleanos sanitizados del panel.
No se publica configuración privada.

Fallos auxiliares conservados: job-13 exit1/curl23 aunque HTTP200 por usar
`/dev/null` desde Python en Windows en vez de `os.devnull` (`nul`); no se repitió
esa solicitud. Job-16 observó curl0/HTTPS200 a
2026-10-28T11:13:30.798805-03:00, pero el helper terminó exit1 con SyntaxWarning
por escape de ruta Windows (`\\C`). PowerShell local aportó los metadatos
faltantes sin nuevo HTTP/auth. Job-12 exit127 corresponde a su parada intencional.
No son fallos funcionales de auth ni helpers íntegramente aprobados.

Se acredita supervivencia de esta sesión y revocación tras reinicios de API,
no reinicio de MySQL Server ni durabilidad ante fallo de disco. Siguen pendientes
SQL fresco normal/frontera/concurrencia/rollback, vencimiento y fallos/reintento
integrados, otras sesiones e invariancia de DB/contadores, Mobile/iPhone y Enzo.
No hay cierre académico ni certificación integral de seguridad; persiste la
excepción aprobada de contraseña ficticia local en texto plano.

## Verificación sintética, sin navegador ni MySQL

Desde la raíz del worktree de autenticación:

```sh
node --test tests/SessionDurabilityDiagnostic/*.test.mjs
node --check odd/browser-checks/session-durability-20261028/session-durability-panel.mjs
node --check odd/browser-checks/session-durability-20261028/install-panel.mjs
```

Node builtins, DOM/fetch mínimos ficticios y tokens sintéticos ejecutan el
instalador real serializado y los eventos públicos de sus botones. No hay hooks
para extraer el token del cierre. La prueba del entrypoint usa exclusivamente
APIs de navegador sintéticas. Estas pruebas no son evidencia de reinicios o
SQL real. La [guía de sesiones existente](api-session-checks.md) y la
[política autoritativa](../reference/auth-contracts.md) conservan sus límites;
el [informe](../project-report.md#durabilidad-local-observada-tras-dos-reinicios-de-api)
y el backlog distinguen este resultado de los pendientes, sin completar la tarea.
