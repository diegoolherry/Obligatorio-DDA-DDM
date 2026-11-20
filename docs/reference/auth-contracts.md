# Autenticación básica local y contratos iniciales de ingreso

## Dirección vigente: JWT sin estado (FIN-005)

Decisión del usuario: el proyecto se limita a una demostración local y aislada con cuentas totalmente ficticias. **No está autorizado desplegarlo en ninguna plataforma ni usar credenciales o usuarios reales** hasta una autorización humana explícita e independiente. Las contraseñas se guardan solo como hash (abajo); no devolverlas en respuestas ni registrarlas en logs.

### Decisiones de FIN-005: JWT y hash de contraseñas

Decisiones de Diego del 2026-11-18 en el chat de trabajo de FIN-005 (el [backlog](../tasks.json) conserva el estado de la tarea). Son decisiones, no resultados de prueba.

| Tema | Decisión |
| --- | --- |
| Mecanismo | **JWT adoptado**, sin estado: «Migramos a JWT», «JWT puro, sin estado». Reemplaza la sesión de servidor con token opaco y la tabla `sesion`. |
| OpenIddict | **No adoptado.** |
| Identity | **No adoptado.** Solo se usa `IPasswordHasher<Usuario>` del framework compartido, registrado sin el sistema de usuarios de Identity. |
| Contraseñas | Hash con sal mediante `IPasswordHasher<Usuario>`; se retira la excepción de texto plano. |
| Vigencia | **7 días**, «como hoy». Las diapositivas docentes recomiendan tokens cortos; ver riesgo aceptado. |
| Contrato de ingreso | «Mantener la actual»: `{identificadorUsuario, roles[], token, venceEnUtc}`; solo cambia el formato del token. |
| Logout | «Igual a la clase»: el cliente descarta el token y el servidor no revoca. |

**Riesgo aceptado (Diego).** Cerrar sesión solo descarta el token en el cliente. Un token robado, un cambio de rol o una cuenta desactivada **siguen surtiendo efecto hasta el vencimiento (7 días)**: no hay revocación, lista de bloqueo ni refresh token. Los roles viajan en el token y no se reconsultan en cada solicitud. Reducir la vigencia o agregar revocación requiere una nueva decisión del equipo.

### Mecanismo implementado en fuente

- **Ingreso.** `POST /api/autenticacion/ingreso` verifica correo y contraseña con `IPasswordHasher<Usuario>`. Usuario inexistente, inactivo, contraseña incorrecta o una fila heredada que no es un hash (texto plano anterior) reciben el mismo `401` y rechazo genérico. Si coincide, [`EmisorJwt`](../../src/Api/API-Berruti/API-Berruti/Security/Authentication/EmisorJwt.cs) firma un JWT HS256.
- **Claims.** `sub` = identificador público del usuario (no el ID correlativo `US-1` ni el correo), `jti` aleatorio y un claim de rol por cada rol, leídos de MySQL al ingresar, nunca del cliente. Sin correo ni datos personales: **un JWT está firmado, no cifrado**.
- **Vencimiento.** 7 días en segundos enteros; `exp` coincide con `venceEnUtc`. La API acepta hasta 5 minutos de tolerancia horaria después de `exp` (`ClockSkew` por defecto de JwtBearer, como en la clase).
- **Validación.** [`ConfiguracionAutenticacion`](../../src/Api/API-Berruti/API-Berruti/Security/Authentication/ConfiguracionAutenticacion.cs) usa `AddJwtBearer` y valida emisor, audiencia, clave de firma y vencimiento; acepta solo HS256 (rechaza `alg: none`). Token ausente, alterado o vencido recibe el mismo `401` con `WWW-Authenticate: Bearer`, sin revelar el motivo; con identidad válida pero sin el rol, `403`. Los rechazos no modifican datos protegidos y conocer un ID o declarar un rol desde el cliente no concede acceso.
- **Salida.** `POST /api/autenticacion/salida` exige un Bearer válido y responde `204` **sin estado en el servidor**; el token sigue siendo válido hasta `exp` si alguien lo conserva.
- **Clave y configuración.** [`ConfiguracionJwt`](../../src/Api/API-Berruti/API-Berruti/Security/Authentication/ConfiguracionJwt.cs): `Jwt__Key` de al menos 32 bytes, solo en el `.env` local (nunca versionado; ver [`api-local-config.example.txt`](../../src/Api/API-Berruti/API-Berruti/api-local-config.example.txt)). `Jwt:Issuer` y `Jwt:Audience` están en `appsettings.json`. Sin clave válida el arranque normal termina con código 2 y [`scripts/start-api-local.ps1`](../../scripts/start-api-local.ps1) rechaza el `.env`. La clave no viaja a Web ni a Mobile y no usa `EXPO_PUBLIC_*`.
- **Contraseñas y transición.** Las bases locales son descartables: con el cambio, recrearlas con `--reset-local` ([guía](../how-to/local-database-reset.md)). La migración `QuitarSesion` elimina la tabla `sesion` ([script generado](../../scripts/database/20261118162624_QuitarSesion.sql)); se eliminaron `seed-auth-demo-passenger.sql` y `review-demo-admin-creation.sql`, que escribían contraseñas legibles. Una base anterior con contraseñas en texto plano no autentica: se recrea, no se migra.

### Reglas que se conservan

- Mantener Controller → Service → Repository → DbContext. Por exigencia explícita del usuario, **cada Service y Repository de C# tiene su propia interfaz**, y los consumidores y la DI dependen de ella. No se introduce un repositorio genérico. La aprobación del usuario no se presenta como evidencia docente.
- Los DTOs son contratos separados de las entidades: clases propias en API y Web y tipos equivalentes en Mobile, sin Shared.
- Los clientes conservan el token **solo en memoria**, sin almacenamiento persistente ni «recordarme». Reabrir la app o recargar la Web requiere ingresar de nuevo. Los clientes validan solo la forma del token (tres segmentos base64url) y no decodifican su contenido; los roles de la respuesta sirven para adaptar la UI, no para autorizar.
- Sin sesión válida, `401`; con sesión válida pero sin permiso, `403`. Si falla la salida por red, el cliente informa que **no se confirmó el cierre** y no muestra un éxito ficticio.
- HTTPS sin desactivar la validación de certificados. El token es una credencial: no va en URLs ni logs. El usuario confirmó que su dispositivo Expo es un **iPhone**.
- Los IDs string prefijados de entidades no convierten credenciales ni referencias externas en correlativos.

### Estado de verificación

| Afirmación | Categoría | Fuente |
| --- | --- | --- |
| JWT, sin OpenIddict ni Identity, 7 días, contrato mantenido, logout sin estado, riesgo aceptado | Decisión | Diego, 2026-11-18, FIN-005 |
| Emisión, validación, hash y salida `204` sin estado | Implementado en fuente | Archivos enlazados arriba |
| Clientes: forma del token y textos de cierre sin afirmar revocación | Observado por quien redactó esta documentación | `dotnet run --project tests/WebAuthentication/WebAuthentication.csproj --no-launch-profile` → 177/177; `node --test tests/MobileAuthentication/*.test.mjs` → 26/26; `npm run typecheck` en `src/Mobile` → sin errores |
| Pruebas API (`tests/AuthenticationSessions` 64/64, `tests/DemoAdministratorPreparation` 55/55, `tests/ApiLocalEnv` 32/32) | Reportado durante FIN-005; no repetido al redactar esto | PR de FIN-005 |
| Recorrido real HTTPS/MySQL contra la API con JWT; `--mysql` de `tests/MySqlPersistence` | **Pendiente**: no ejecutado | — |

Las pruebas automatizadas protegen los invariantes de autorización; este texto no las sustituye. Ver [rutas](api.md) y [comprobaciones](../how-to/api-session-checks.md).

## Estado del código existente

 Contratos iniciales de ingreso

**Implementado:** clases propias de API y Web, DataAnnotations y roles enum con el mismo contrato JSON de MVP-001-02; ya no existe el proyecto Shared. La verificación independiente y aprobación de Enzo reportada para MVP-001-02 pertenecen al corte anterior. **Separación cerrada:** el usuario confirmó que Enzo aprobó todos los criterios de `MVP-001-06`. **En fuente:** endpoints API de sesión y autorización; **pendiente:** prueba pública HTTPS/MySQL con JWT. El [backlog](../tasks.json) conserva la autoridad del cierre; ver [API](api.md).

## Aprobación y límites

El usuario detuvo el candidato inglés por sintaxis no discutida y aprobó explícitamente en el chat clases públicas españolas con `get; set;`, cadenas `string.Empty` y DataAnnotations con los mensajes de abajo. En ese corte confirmó bibliotecas de clases y referencias como enseñadas, por lo que Shared permaneció. Luego pidió eliminar el proyecto compartido y mantener clases propias en API y Web; esta separación no reabre el cierre histórico de MVP-001-02. Los extractos docentes reportan DataAnnotations aplicadas; DTO es un requisito autorizado, no cobertura docente demostrada.

Tras explicar enum y conversor, el usuario autorizó explícitamente `RolIngreso`, `RolIngreso[]` y `JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)` en las opciones MVC de la API. Para Web pidió solo un comentario sobre un futuro consumidor HTTP; no se agrega configuración dormida, DI, factory ni cliente ficticio. La aprobación se atribuye al chat, no a Enzo ni a evidencia docente del conversor. Los nombres físicos C# permanecen; no se reintroducen `sealed`, modificador `required`, `init` ni `JsonPropertyName`.

El corte histórico de contratos no agregaba endpoints, sesiones, tokens, UI, persistencia ni permisos. T2 agrega solo el comportamiento API descrito arriba, sin UI ni dependencias nuevas. Los atributos validan datos cuando se invoca validación; no autentican ni prueban existencia del correo. No se exige fuerza, longitud o composición de contraseña, ni se normalizan credenciales. Los roles describen identidad, no autorizan acciones.

## Clases propias por aplicación

[API](../../src/Api/API-Berruti/API-Berruti/API-Berruti.csproj) define DTOs en `API_Berruti.DTOs.Authentication`; [Web](../../src/Web/Web-App/Web-App/Web-App.csproj) define modelos propios en `Web_App.Models.Authentication`. Ambos proyectos siguen siendo `net10.0`, sin referencia entre sí ni a una biblioteca de contratos compartida. No se comparten entidades de persistencia ni se agregan paquetes. La compatibilidad se mantiene mediante el contrato JSON y sus pruebas, no mediante identidad de clases o assemblies.

| Clase pública C# | DTO de API | Modelo de Web | Tipo equivalente Mobile |
| --- | --- | --- | --- |
| `SolicitudIngresoDto` | [AuthenticationRequest.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationRequest.cs) | [AuthenticationRequest.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationRequest.cs) | `SolicitudIngresoDto` |
| `RespuestaIngresoDto` | [AuthenticationSuccessResponse.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationSuccessResponse.cs) | [AuthenticationSuccessResponse.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationSuccessResponse.cs) | `RespuestaIngresoDto` |
| `RespuestaErrorIngresoDto` | [AuthenticationRejectionResponse.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationRejectionResponse.cs) | [AuthenticationRejectionResponse.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationRejectionResponse.cs) | `RespuestaErrorIngresoDto` |

Cada aplicación mantiene también su propio `RolIngreso`. Cambiar un lado requiere ejecutar las comprobaciones de compatibilidad; no se agrega un cliente HTTP ni se configura JSON en Web antes de tener un consumidor.

[authentication.ts](../../src/Mobile/src/contracts/authentication.ts) conserva la unión `RolIngreso = 'pasajero' | 'cobrador' | 'administrador'` y agrega únicamente los dos campos pasivos de sesión. No existe un consumidor de ingreso.

## Campos y validación

| DTO | Propiedad C# / tipo | JSON MVC / TypeScript | Significado y reglas |
| --- | --- | --- | --- |
| Solicitud | `Correo: string` | `correo: string` | `Required`: `El correo es obligatorio.`; `EmailAddress`: `El correo no es válido.` |
| Solicitud | `Contrasena: string` | `contrasena: string` | `Required`: `La contraseña es obligatoria.`; no devolver ni registrar la credencial. |
| Éxito | `IdentificadorUsuario: string` | `identificadorUsuario: string` | Identificador público opaco; nunca visible al usuario. No expone PK. `Usuario.IdentificadorPublico` se genera aleatoriamente una sola vez al alta (32 bytes Base64url), se persiste con índice único y es el claim `sub` del JWT; por sí solo no autentica solicitudes. |
| Éxito | `Roles: RolIngreso[]` | `roles: RolIngreso[]` | Array de textos españoles por el conversor MVC. Inicializador `new RolIngreso[0]`; sin cardinalidad mínima, orden obligatorio ni rol principal. |
| Éxito | `Token: string` | `token: string` | JWT HS256 compacto (`header.payload.signature`, tres segmentos base64url sin padding; los clientes aceptan hasta 4096 caracteres); solo respuesta de ingreso. Default vacío; no guardar/loguear ni enviar por URL. |
| Éxito | `VenceEnUtc: string` | `venceEnUtc: string` | UTC ISO 8601 `yyyy-MM-ddTHH:mm:ss.ffffffZ`, precisión microsegundos (en segundos enteros); siete días absolutos y coincide con el `exp` del JWT; default vacío. |
| Rechazo | `Codigo: string` | `codigo: string` | Código genérico elegido: `ingresoRechazado`. |
| Rechazo | `Mensaje: string` | `mensaje: string` | `No se pudo ingresar con las credenciales proporcionadas.` |

Éxito y rechazo siguen separados, sin envelope. Solo el éxito de ingreso contiene el token. Email desconocido, contraseña incorrecta y cuenta inactiva reciben el mismo `401` y rechazo genérico. DataAnnotations conserva los `400` y mensajes originales; no normaliza credenciales. Los fallos inesperados capturados por el límite de autenticación, incluidos los de proveedor/configuración, devuelven `500` sanitizado, no rechazo de credenciales: `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}`, con `Cache-Control: no-store`. Identidad y pruebas de rol devuelven `RespuestaSesionDto` (`identificadorUsuario`, `roles`), nunca token, PK ni contraseña.

Las cadenas comienzan vacías, no validadas por el inicializador. `Required` rechaza null/vacío/espacios cuando se invoca validación. Las propiedades siguen siendo mutables; no garantizan nulabilidad runtime. TypeScript no ejecuta DataAnnotations ni valida JSON recibido.

## Roles y serialización

`RolIngreso` se define independientemente en [API](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationRoles.cs) y [Web](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationRoles.cs); conserva los valores del enum que reemplazó las constantes string del candidato anterior:

| Miembro enum C# | Texto JSON de salida / unión Mobile | Dominio / significado anterior |
| --- | --- | --- |
| `Pasajero` | `pasajero` | Pasajero / passenger |
| `Cobrador` | `cobrador` | Cobrador / fare collector; conductor/cobrador de MVP-001-04 es este mismo rol. |
| `Administrador` | `administrador` | Administrador / administrator |

[Program.cs de API](../../src/Api/API-Berruti/API-Berruti/Program.cs) registra el conversor en `AddControllers().AddJsonOptions(...)`, con los using de `System.Text.Json` y `System.Text.Json.Serialization`. Las opciones MVC conservan camelCase web para propiedades; el conversor produce textos camelCase para enums y no admite enteros. Con estas opciones, roles numéricos (definidos o no), texto numérico y texto desconocido probados generan `JsonException`; serializar un enum no definido también falla. No es un validador adicional de dominio ni establece cardinalidad o permisos. Un cast C# todavía puede construir valores enum no definidos: el rechazo comprobado pertenece al conversor.

Un DTO no produce este wire por sí solo. `JsonSerializer.Serialize(dto)` independiente predeterminado conserva PascalCase y enums numéricos. Para reproducir el contrato fuera de MVC hay que usar `new JsonSerializerOptions(JsonSerializerDefaults.Web)` **y agregar el mismo conversor**, tanto al serializar como al deserializar. Solo opciones Web no convierten enums a texto. El conversor no exige que todo texto de entrada tenga exactamente las mayúsculas de la salida; no se agrega una política de casing estricta.

[Program.cs de Web](../../src/Web/Web-App/Web-App/Program.cs) contiene únicamente el recordatorio en español para un futuro cliente HTTP de API: deberá utilizar ese conversor para leer `RolIngreso`. No registra opciones HTTP JSON, DI ni cliente; no acredita integración. Mobile conserva textos españoles sin conversor runtime; solo cambia el contrato pasivo con los dos campos nuevos.

## Evidencia histórica por candidato

- **Candidato español anterior con constantes string:** el escritor observó RED de validación (ocho fallos esperados), GREEN nueve PASS y JSON doce PASS. La verificación independiente confirmó GREEN, JSON, builds/typecheck/convenciones/backlog/whitespace; no reprodujo RED. Su arnés fue eliminado y se confirmó su ausencia. Es evidencia histórica, no verificación del nuevo enum.
- **Candidato enum actual, observación del escritor:** antes del conversor, RED JSON exit 1, cinco fallos de veinte aserciones: salida numérica, roundtrip de textos, aceptación de números definidos/no definidos y serialización de enum no definido. Después del conversor: GREEN exit 0, veinte PASS. Validación sin cambios: nueve PASS. Builds API/Web, typecheck Mobile, convenciones y `git diff --check` pasan. Comprobación Python antes/después: JSON válido y huella idéntica del backlog excluyendo solo evidence de MVP-001-02, preservando otros 18 registros y todos los campos de estado/metadatos; huella de Mobile idéntica. Comandos de ejecución en [informe §7](../project-report.md#contratos-pasivos-mvp-001-02).
- **Verificación independiente:** JSON20 y validación9 PASS; builds API/Web, typecheck, convenciones, whitespace y backlog exit 0. RED5 permanece como evidencia del escritor. Revisión de Enzo pendiente. No se comprobó login, HTTP, UI, sesión o autorización.

## Comprobación vigente de separación y regresión

Desde la raíz del worktree:

```sh
dotnet run --project tests/AuthenticationContracts/AuthenticationContracts.csproj --no-launch-profile
```

El proyecto consola usa los proyectos reales API/Web y no agrega paquetes de pruebas. Comprueba la adaptación deliberada a cuatro campos de éxito (los anteriores más dos strings), defaults vacíos de los nuevos campos, clases públicas propias, ausencia de Shared y de dependencias entre aplicaciones, nombres de roles, campos y tipos JSON exactos, roundtrip, defaults, rechazos de enums y validaciones con sus mensajes originales en ambas aplicaciones. Compara con las opciones web y el conversor cuyo registro coteja en el código de API; no ejecuta su entrypoint, HTTP, UI ni autenticación. Devuelve exit 1 y mensajes `FAIL` por stderr si falla alguna comprobación; exit 0 si pasan todas. No requiere cuentas, base de datos ni servidores.

## Receta histórica de la suite aislada

Esta receta documenta las pruebas del corte anterior con Shared. No es un comando vigente para el árbol actual: la suite externa fue eliminada y su referencia al proyecto compartido ya no existe. Usar la comprobación anterior para este corte, sin recrear recursos externos.

Se recreó `<carpeta-externa>/` solo tras comprobar que no existía. Inventario propio: `Suite.csproj`, `Program.cs`, outputs `bin/` y `obj/`; se eliminaron únicamente esos archivos generados tras verificar el inventario y la ausencia de enlaces simbólicos; confirmó que la carpeta ya no existe. La receta requiere recrearla para repetir las pruebas. Es consola `net10.0` sin paquetes, con `FrameworkReference` a `Microsoft.AspNetCore.App` y `ProjectReference` a Shared. No inicia un servidor.

```sh
dotnet run --project <carpeta-externa>/Suite.csproj -- json
dotnet run --project <carpeta-externa>/Suite.csproj -- validacion
```

**Opciones sometidas a prueba:** crear `ServiceCollection`, agregar logging y registrar `AddControllers().AddJsonOptions(...)` con el mismo cuerpo que la API. Resolver `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>.Value.JsonSerializerOptions` del proveedor. Cotejar el bloque completo del registro con la fuente API, normalizando solo CRLF/LF. La suite reproduce el registro y resuelve opciones MVC reales en memoria; no carga el entrypoint API ni ejecuta HTTP. Antes del conversor se cotejó y utilizó `AddControllers()` solo.

**Veinte aserciones JSON completas:** cada aserción debe acumular PASS/FAIL real y terminar con exit 1 si alguna falla, no solo imprimir etiquetas.

1. Registro MVC cotejado con fuente API; enum contiene exactamente `Pasajero`, `Cobrador`, `Administrador`; unión Mobile conserva exactamente los tres textos (tres aserciones).
2. Serializar solicitud ficticia (`prueba@example.invalid`, contraseña `x`), éxito (`opaco-de-prueba`, tres roles) y rechazo documentado. Para cada DTO, exigir exactamente las dos claves de la tabla y sus tipos: string salvo `roles` array (seis aserciones). Exigir claves exactas excluye credenciales en respuestas.
3. Exigir array de tres strings `pasajero/cobrador/administrador`, no números; deserializar JSON explícito con esos textos y comprobar identificador, todos los enums y reserialización idéntica (dos aserciones).
4. Roundtrip de todas las propiedades de solicitud y rechazo (dos aserciones).
5. Exigir inicializador vacío, salida array vacío y deserialización de `{"roles":[]}` con longitud cero (una aserción).
6. Deserializar individualmente `{"roles":[0]}`, `{"roles":[999]}`, `{"roles":["0"]}` y `{"roles":["desconocido"]}`: exigir `JsonException` en cada caso (cuatro aserciones).
7. Serializar respuesta con `(RolIngreso)999`: exigir `JsonException`; serializar una respuesta válida sin opciones y exigir propiedad `Roles` y primer elemento numérico (dos aserciones).

**Nueve aserciones de validación sin cambios:** crear una solicitud por caso y lista nueva de `ValidationResult`; invocar `Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true)`. En cada inválido individual exigir false, un error y mensaje exacto; en válido exigir true y cero errores. Valores ficticios, no credenciales reales.

| Correo | Contraseña | Resultado esperado |
| --- | --- | --- |
| null; `""`; `"   "` (tres casos) | `"x"` | false; `El correo es obligatorio.` |
| `"no-es-correo"` | `"x"` | false; `El correo no es válido.` |
| `"prueba@example.invalid"` | null; `""`; `"   "` (tres casos) | false; `La contraseña es obligatoria.` |
| `"prueba@example.invalid"` | `"x"` | true; cero errores, sin fuerza adicional. |
| Nueva instancia sin asignaciones | Nueva instancia | false; exactamente dos errores. |
