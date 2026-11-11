# Ingreso móvil compartido de pasajero y cobrador

## Flujo mínimo (antes de implementar)

Anónimo → enviar correo/contraseña ficticios → ocupado → sesión confirmada por respuesta válida del servidor, o validación/rechazo/red/servidor/configuración → reintentar. Una acción dominante, sin envíos duplicados; etiquetas persistentes y correo conservado tras errores. La contraseña se borra después del envío; nunca se muestra sin máscara, ni se renderizan token o identificador público.

Sesión → cerrar sesión → ocupado → anónimo con revocación confirmada (`204`), o revocación no confirmada y reintento seguro. Mientras la revocación esté pendiente no se permiten operaciones protegidas. Descartar memoria no confirma revocación remota. Una respuesta tardía no restaura una sesión descartada.

## Configuración y límites

`App.tsx` consume `process.env.EXPO_PUBLIC_API_URL` y la pasa al servicio por instancia. Es una URL pública HTTPS de la raíz de API, no un secreto; no incluir usuario/contraseña, query ni fragmento. Configuración ausente o inválida muestra un error local accionable sin enviar solicitudes. No se escriben `.env`, configuración Expo ni almacenamiento persistente. No se normalizan credenciales ni se inventa política de contraseña.

En iPhone, `localhost` es el teléfono, no la computadora. La futura comprobación requiere una dirección HTTPS alcanzable y confianza válida configuradas por el usuario; el túnel Metro no publica la API. No hay bypass TLS ni cambios de certificados, firewall o LAN en este corte.

El token queda privado en memoria del servicio por aplicación; cada instancia/proceso nuevo es anónimo. No se promete detectar cada cierre/background del sistema. Fetch usa `credentials: 'omit'`, `redirect: 'error'` y Bearer por solicitud individual, nunca encabezados globales. Las solicitudes tienen cancelación y límite de tiempo. No hay router, store, cookies ni librerías nuevas; se mantienen componentes nativos, StyleSheet y áreas seguras.

## Responsabilidades y evidencia docente

La raíz crea una instancia por aplicación y la inyecta por props como `IAuthenticationService`; la pantalla solo recoge entradas, representa estados y despacha acciones. El servicio implementa esa interfaz y custodia la credencial. Los tipos de resultado/sesión no son entidades persistidas. La pantalla compartida conserva el mismo formulario y expone `checkPassenger()` y `checkCollector()` como comprobaciones explícitas de solo lectura. Consumen `GET /api/autenticacion/prueba-pasajero` y `GET /api/autenticacion/prueba-cobrador` con Bearer; no seleccionan ni conceden roles. La API decide: `200` confirma el permiso, `403` lo deniega sin borrar la sesión y `401` invalida la sesión local. Los errores de red/servidor permiten reintentar sin declarar éxito. No incorpora funciones de transporte ni cuentas de demostración nuevas.

Estado local, props, componentes nativos y StyleSheet corresponden a los conceptos reportados en los extractos docentes. HTTP práctico, autenticación e interfaz explícita no se presentan como implementación docente demostrada: este corte se apoya en las aprobaciones S5–S7/S10 del [Spec](../../odd/tasks/mvp-001-03-passenger-auth.md). Se sigue el mecanismo mínimo autorizado, sin router, almacenamiento ni paquetes adicionales.

## Comprobaciones offline

Desde la raíz, en primer plano:

```sh
node --test tests/MobileAuthentication/*.test.mjs
cd src/Mobile && npm run typecheck
git diff --check
```

Resultado histórico del corte pasajero: pruebas de servicio `15/15` PASS (exit 0), typecheck `tsc --noEmit` exit 0. Node informa `ExperimentalWarning` por `stripTypeScriptTypes`. RED previo de comportamiento: `10` fallos y `1` PASS con el servicio todavía sin implementar; un error posterior de sintaxis en una expresión regular del test se corrigió antes del GREEN final. No se instaló ni cambió ninguna dependencia.

Ampliación cobrador: `17/17` PASS (exit 0), incluidos los 15 casos previos, una secuencia de servicio cobrador (GET/Bearer, denegación, servidor/red, reintento, logout y aislamiento) y una comprobación **estructural**, no renderizada, del título/acciones de la pantalla compartida. Typecheck `tsc --noEmit` exit 0. [Seguimiento y límites del corte](../../odd/tasks/mvp-001-04-collector-auth.md).

Las pruebas usan el servicio real y transporte falso con datos ficticios; no prueban TLS, MySQL, API pública ni dispositivo. Pendientes: revisión visual/táctil/accesibilidad con teclado, flujo vivo de ingreso/permiso/salida, iPhone y comportamiento nativo iOS de TLS, redirects y cookies. La API autoriza roles; la visibilidad local no autoriza operaciones.

## Ubicación de demostración (MVP-009)

Dos pestañas nuevas usan la sesión del ingreso: **Compartir ubicación** (rol cobrador) y **Ubicación del servicio** (rol pasajero). Consumen las rutas de [Ubicación de demostración](../reference/api.md#ubicación-de-demostración-mvp-009) con el Bearer que añade la sesión por solicitud (`authorizedRequest`); las pantallas nunca ven el token. La API sigue decidiendo los permisos: `403` si la cuenta no tiene el rol o el servicio no está asignado, `401` descarta la sesión local. Los datos son ficticios.

### Cómo probarlo en el iPhone con Expo

1. Reiniciá la base local con la asignación sembrada (cobrador ↔ servicios demo) y levantá la API detrás de una URL HTTPS alcanzable desde el iPhone ([configuración](client-api-configuration.md)). El túnel de Metro no publica la API.
2. En `src/Mobile`, con `EXPO_PUBLIC_API_URL` apuntando a esa raíz HTTPS: `npx expo start` y abrí el QR con Expo Go.
3. **Cobrador:** pestaña «Ingreso» con la cuenta de cobrador de demostración → «Compartir ubicación» → elegí un servicio → «Permitir ubicación» (solo entonces aparece el diálogo del sistema) → «Compartir». La pantalla muestra la hora de la última publicación aceptada (hora de Uruguay), la precisión en metros y si cumple la periodicidad. Se envía como máximo una vez por la periodicidad que informa la API (30 s hasta la primera respuesta).
4. **Pasajero:** cerrá sesión, ingresá con la cuenta de pasajero → «Ubicación del servicio» → elegí el servicio. Ves la última posición, su hora y antigüedad, la llegada aproximada y si va en horario o con demora; «Actualizar ubicación» vuelve a consultar. Pasado el umbral sin publicaciones aparece «Desactualizada» y la llegada pasa a «no disponible» con su motivo.

### Permiso de ubicación

- Solo ubicación en primer plano («mientras se usa la app»). No hay ubicación en segundo plano ni `UIBackgroundModes`; el plugin `expo-location` de `app.json` define el texto de propósito en español y desactiva explícitamente los modos de segundo plano. Expo Go puede mostrar su propio texto: el de `app.json` rige en un build de desarrollo o propio.
- El permiso no se pide al abrir la app: primero se explica para qué se usa y se pide al tocar «Permitir ubicación».
- Si se deniega y el sistema no vuelve a preguntar, aparece «Abrir Ajustes» (`Linking.openSettings()`). Al volver a la app se vuelve a leer el permiso, y también justo antes de «Compartir».
- La lectura sigue aunque el cobrador cambie de pestaña mientras la app está abierta. Se detiene con «Detener», al cerrar la sesión de cobrador, si el permiso deja de estar concedido y cuando la app pasa a segundo plano; hay que volver a tocar «Compartir». El envío en segundo plano queda para FIN-007.

### Comprobaciones

```sh
node --test tests/MobileAuthentication/*.test.mjs tests/MobileLocation/*.test.mjs
cd src/Mobile && npm run typecheck
```

Las pruebas cubren la sesión (solicitud autorizada, 401/403, sin filtrar el token) y el servicio de ubicación (resultados tipados por estado, redondeo a 6 decimales, hora de Uruguay). Las pantallas solo tienen comprobaciones de fuente (sesión entre pestañas, envío que sigue con la pantalla oculta); el diálogo de permiso y el GPS no tienen pruebas automáticas: se verifican con el recorrido manual anterior, que queda pendiente de ejecutar en el iPhone.
