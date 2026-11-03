# Comprobar una sesión a través de dos reinicios manuales

## Qué demuestra y qué no

Este **diagnóstico temporal, no UI de la aplicación**, prepara T12/S5–S6,
S9 y S15–S16. Su ejecución real sigue pendiente. El usuario autorizó el
instrumento y **dos reinicios manuales de la API**; iPhone queda diferido.
No modifica la política nativa de siete días, aplicación, esquema ni configuración.

El resultado acredita únicamente esta secuencia condicional con el mismo token:

| Acción humana | Solicitudes del panel | Comprobación |
| --- | --- | --- |
| Ingresar una vez | POST ingreso; GET prueba-pasajero | 200 JSON/no-store; misma identidad y roles |
| Pausa 1: detener y arrancar API; confirmar | GET prueba-pasajero; POST salida | 200 JSON/no-store con mismo token; 204 vacío/no-store |
| Pausa 2: detener y arrancar API; confirmar | GET prueba-pasajero | 401 vacío/no-store, challenge Bearer con mismo token anterior |

Todas las rutas llevan `/api/autenticacion/`. No hay solicitudes durante las
pausas, nuevo ingreso, matriz de otros roles, renovación ni reintentos.
**Pulsar continuar no demuestra un reinicio.** Sin evidencia humana externa de
ambas paradas/arranques, el éxito solo demuestra el comportamiento del mismo
token antes/después de salida. No prueba por sí solo MySQL, durabilidad real,
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

1. La persona escribe privadamente la contraseña en el único campo enmascarado
   y pulsa **Ingresar una sola vez**. No leer su valor, capturar red, cuerpos,
   headers o pantallas con secretos. El campo queda vacío y deshabilitado
   inmediatamente. Debe aparecer **PAUSA 1**, sin más solicitudes.
2. **No recargar/cerrar la pestaña.** La persona detiene realmente SOLO la API
   desde su terminal y vuelve a iniciarla. Registra confirmación humana de
   parada y nuevo arranque, orden y hora, sin secretos, para vincularla con
   PAUSA 1. La mera disponibilidad del puerto no prueba reinicio.
3. Solo después pulsa **Confirmo parada y arranque manual 1**. Se usa el mismo
   token, se comprueban identidad/roles y se intenta una sola salida. Debe
   aparecer **PAUSA 2**. El token anterior sigue exclusivamente en memoria.
4. La persona vuelve a detener y arrancar SOLO la API, registra la segunda
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
el parent reconciliará enlaces entrantes, informe y estados por separado.
