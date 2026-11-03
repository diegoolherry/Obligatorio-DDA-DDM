# Ingreso Web de demostración

## Flujo mínimo

Anónimo → enviar correo y contraseña ficticios → ocupado (sin envíos duplicados) → sesión confirmada por respuesta válida, o validación/rechazo/red/servidor/configuración → reintentar conservando el correo. La contraseña nunca se muestra como texto ni se devuelve en mensajes; se borra después del envío.

Sesión → cerrar sesión → ocupado → anónimo solo tras `204`; si falla, indicar **revocación no confirmada** y conservar la credencial solo en memoria para reintentar. Un `401` autenticado elimina la sesión local inválida, pero no acredita revocación. Descartar memoria no equivale a cerrar en el servidor. Respuestas tardías no recuperan sesiones descartadas.

Una acción principal por estado; etiquetas persistentes y estados accesibles. Solo la API autoriza: la visibilidad local no acredita permisos. Sin pantallas de negocio ni nueva identidad visual.

## Configuración y alcance

El navegador debe abrir `/ingreso` por **HTTPS de Web**: el perfil existente `https` declara `https://localhost:7054` (además de HTTP en 5025). No alcanza con configurar HTTPS para la API. El guard previo a la redirección rechaza HTTP en `/ingreso`, sin distinguir mayúsculas e incluyendo segmentos posteriores, con `400`, texto plano y `Cache-Control: no-store`, sin redirigir ni procesar/renderizar el formulario. Esto también falla cerrado cuando no se conoce un puerto HTTPS; el perfil `http` no habilita el ingreso. No se cambian perfiles, puertos ni certificados.

Home, Counter, Weather y el hub compartido `/_blazor` conservan su comportamiento previo: no hay bloqueo HTTP global ni cambio de transporte SignalR. Cargar el formulario por HTTPS debe usar el transporte seguro normal de InteractiveServer; su comportamiento real en navegador/circuito y la confianza TLS siguen pendientes. No desactivar validación TLS.

El cliente consume `Api:BaseUrl` (variable pública `Api__BaseUrl`). Debe ser una URL HTTPS absoluta sin credenciales, query ni fragmento. Ausencia o valor inválido produce un error local accionable, sin enviar solicitudes. Ejemplo configurable del perfil existente: `https://localhost:7053/`; no demuestra certificado confiable ni API disponible. No colocar tokens, contraseñas ni datos de DB en esa opción.

El servicio es scoped por circuito InteractiveServer; una instancia/circuito nuevos son anónimos. Una reconexión del mismo circuito puede conservar memoria. No usa cookies ni almacenamiento persistente, encabezados Authorization globales ni redirecciones automáticas. El token viaja únicamente en cada solicitud protegida. La pérdida del circuito no confirma revocación remota.

`/ingreso` usa Bootstrap del scaffold y declara InteractiveServer solo por página. Home, Counter y Weather se conservan. Usar únicamente cuentas ficticias locales; no es un servicio oficial.

## Verificación desde la raíz

```sh
dotnet run --project tests/WebAuthentication/WebAuthentication.csproj --no-launch-profile
dotnet build src/Web/Web-App/Web-App.slnx --no-restore --nologo
dotnet run --project tests/AuthenticationContracts/AuthenticationContracts.csproj --no-launch-profile
git diff --check
```

Las pruebas deterministas utilizan HttpMessageHandler ficticio, DTOs propios y DI real sin red ni DB. W1 compone el guard con HttpsRedirectionMiddleware real, configuración vacía y direcciones HTTP-only en memoria: reproduce el paso al siguiente middleware sin puerto HTTPS, comprueba rechazo exacto de ingreso HTTP y preservación de las rutas hermanas. El orden en Program.cs se comprueba estructuralmente, no mediante red. No prueban TLS, MySQL, navegador ni circuito interactivo real. La prueba manual de ingreso, permisos, cierre y reconexión en navegador, y la integración HTTPS/API, siguen pendientes de verificación separada.
