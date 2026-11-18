# Seguridad en APIs móviles: material docente y límites

## Fuente y alcance

`Seguridad_en_APIs_Móviles.pdf`, 14 diapositivas generadas con Gemini Notebook, aportadas por el usuario. El PDF solo contiene imágenes: se renderizaron sus páginas y se transcribieron leyéndolas directamente (no NotebookLM). Los ejemplos usan un caso «SIGMA» con React Native y Node/Express (`/auth/login`, `/tareas/...`, `requireAuth`, `requireRole`); no son la API .NET de este proyecto. Para el lado servidor .NET usá [JWT, roles y Postman](../../ctc-dda-blazor/references/jwt-roles.md).

## Enseñado en la presentación

- **El backend decide (p01, p07, p12, p13).** Ocultar botones o pantallas en la app es experiencia de usuario, no seguridad: un atacante puede llamar directo a la API. La API valida token y rol en cada ruta protegida.
- **Identificación, autenticación y autorización (p02–p04).** Factores de autenticación (conocimiento, posesión, inherencia; MFA solo definido). Modelos de autorización: RBAC y propiedad del recurso («un técnico solo edita lo que creó o tiene asignado»).
- **HTTPS (p05, p13).** Siempre `https://` en producción; nunca aceptar certificados inválidos.
- **Token de acceso (p06, p09, p10).** Login por `POST` con correo y contraseña; la API valida el hash y devuelve un token de vencimiento corto; las llamadas protegidas envían `Authorization: Bearer <token>` y nunca reenvían la contraseña.
- **Almacenamiento (p08, p12, p13).**
  - No guardar contraseñas: viajan una sola vez, en el login.
  - No guardar tokens en AsyncStorage ni en `.env`: las variables de entorno quedan públicas en la app compilada y AsyncStorage puede leerse desde copias de seguridad u otras apps.
  - Si se guarda el token, usar Expo SecureStore o Keychain/Keystore.
- **Errores (p11, p12).**
  - 401: borrar la sesión y volver al login.
  - 403: mantener la sesión y avisar que no tiene permisos.
  - 500: mostrar un error genérico, sin stack trace ni detalles internos.
- **Errores frecuentes (p12).** Confiar en la UI, guardar tokens en AsyncStorage, usar tokens sin vencimiento y exponer errores internos.

## Lo que la presentación no cubre

Refresh tokens y cuánto debe durar un token «corto». Tampoco cubre la revocación ni el endpoint de logout, si el cliente debe vigilar el vencimiento o solo reaccionar al 401, el formato JWT y sus claims, el pinning de certificados ni la biometría para desbloquear el token. La frase «gestionar el cierre de sesión» no define ningún mecanismo.

## Uso en este proyecto

- **Token solo en memoria.** Es una decisión aprobada de Berruti: reabrir la app exige volver a ingresar. Es más restrictiva que guardar en SecureStore y no contradice la presentación, que solo dice dónde guardarlo si se persiste. Persistir la sesión es una decisión de producto aparte; si se aprueba, solo puede usar SecureStore, nunca AsyncStorage ni `.env`.
- **Vigencia del JWT.** El equipo eligió 7 días sin revocación en el servidor (FIN-005). La presentación recomienda tokens cortos: tenelo presente como riesgo aceptado y no lo cambies sin una decisión del equipo.
- **Sin secretos en la app.** No pongas la clave JWT, contraseñas ni otros secretos en la app ni en variables `EXPO_PUBLIC_*`: todo lo que viaja en el bundle es público.
- **Roles en el cliente.** Los roles de la respuesta de ingreso sirven solo para adaptar la UI. La autorización real está en `[Authorize(Roles = ...)]` de la API, que se prueba allí.
- **Decodificar el JWT.** El cliente no decodifica ni confía en el payload; solo valida la forma del token y lo envía.
