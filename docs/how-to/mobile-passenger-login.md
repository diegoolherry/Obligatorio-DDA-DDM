# Ingreso móvil de pasajero

## Flujo mínimo (antes de implementar)

Anónimo → enviar correo/contraseña ficticios → ocupado → sesión confirmada por respuesta válida del servidor, o validación/rechazo/red/servidor/configuración → reintentar. Una acción dominante, sin envíos duplicados; etiquetas persistentes y correo conservado tras errores. La contraseña se borra después del envío; nunca se muestra sin máscara, ni se renderizan token o identificador público.

Sesión → cerrar sesión → ocupado → anónimo con revocación confirmada (`204`), o revocación no confirmada y reintento seguro. Mientras la revocación esté pendiente no se permiten operaciones protegidas. Descartar memoria no confirma revocación remota. Una respuesta tardía no restaura una sesión descartada.

## Configuración y límites

`App.tsx` consume `process.env.EXPO_PUBLIC_API_URL` y la pasa al servicio por instancia. Es una URL pública HTTPS de la raíz de API, no un secreto; no incluir usuario/contraseña, query ni fragmento. Configuración ausente o inválida muestra un error local accionable sin enviar solicitudes. No se escriben `.env`, configuración Expo ni almacenamiento persistente. No se normalizan credenciales ni se inventa política de contraseña.

En iPhone, `localhost` es el teléfono, no la computadora. La futura comprobación requiere una dirección HTTPS alcanzable y confianza válida configuradas por el usuario; el túnel Metro no publica la API. No hay bypass TLS ni cambios de certificados, firewall o LAN en este corte.

El token queda privado en memoria del servicio por aplicación; cada instancia/proceso nuevo es anónimo. No se promete detectar cada cierre/background del sistema. Fetch usa `credentials: 'omit'`, `redirect: 'error'` y Bearer por solicitud individual, nunca encabezados globales. Las solicitudes tienen cancelación y límite de tiempo. No hay router, store, cookies ni librerías nuevas; se mantienen componentes nativos, StyleSheet y áreas seguras.

## Responsabilidades y evidencia docente

La raíz crea una instancia por aplicación y la inyecta por props como `IAuthenticationService`; la pantalla solo recoge entradas, representa estados y despacha acciones. El servicio implementa esa interfaz y custodia la credencial. Los tipos de resultado/sesión no son entidades persistidas. `checkPassenger()` consume únicamente la prueba de lectura ya existente; no incorpora pantallas de otros roles ni funciones de transporte.

Estado local, props, componentes nativos y StyleSheet corresponden a los conceptos reportados en los extractos docentes. HTTP práctico, autenticación e interfaz explícita no se presentan como implementación docente demostrada: este corte se apoya en las aprobaciones S5–S7/S10 del [Spec](../../odd/tasks/mvp-001-03-passenger-auth.md). Se sigue el mecanismo mínimo autorizado, sin router, almacenamiento ni paquetes adicionales.

## Comprobaciones offline

Desde la raíz, en primer plano:

```sh
node --test tests/MobileAuthentication/*.test.mjs
cd src/Mobile && npm run typecheck
git diff --check
```

Resultado observado en este corte: pruebas de servicio `15/15` PASS (exit 0), typecheck `tsc --noEmit` exit 0. Node informa `ExperimentalWarning` por `stripTypeScriptTypes`. RED previo de comportamiento: `10` fallos y `1` PASS con el servicio todavía sin implementar; un error posterior de sintaxis en una expresión regular del test se corrigió antes del GREEN final. No se instaló ni cambió ninguna dependencia.

Las pruebas usan el servicio real y transporte falso con datos ficticios; no prueban TLS, MySQL, API pública ni dispositivo. Pendientes: revisión visual/táctil/accesibilidad con teclado, flujo vivo de ingreso/permiso/salida, iPhone y comportamiento nativo iOS de TLS, redirects y cookies. La API autoriza roles; la visibilidad local no autoriza operaciones.
