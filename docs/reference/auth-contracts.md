# Autenticación básica local y contratos iniciales de ingreso

## Dirección vigente, todavía no implementada

Decisión del usuario: el proyecto se limita a una demostración local y aislada con cuentas totalmente ficticias. **No está autorizado desplegarlo en ninguna plataforma ni usar credenciales o usuarios reales.** Las contraseñas ficticias podrán almacenarse temporalmente en texto plano hasta revisar en clase el mecanismo esperado; esto no constituye almacenamiento seguro. No devolverlas en respuestas ni registrarlas en logs.

La implementación actual prevista no usará Identity. OpenIddict y las mejoras del login se aplazan a `FIN-005` del [backlog](../tasks.json); no se promete incorporar Identity en el futuro. Esta decisión reemplaza la dirección previa Identity/OpenIddict para este corte, sin eliminar los requisitos de autenticación y autorización del MVP.

- Mantener Controller → Service → Repository → DbContext para las operaciones de cuentas y negocio.
- Comprobar correo y contraseña en la API y crear una sesión de servidor vinculada al usuario. La credencial de sesión será opaca, aleatoria e impredecible, no un ID correlativo como `US-1` ni una contraseña; no se ha seleccionado JWT.
- Validar la sesión en cada operación protegida y obtener el rol desde datos autoritativos del servidor, nunca desde un rol declarado por el cliente. Sin sesión válida, responder `401`; con sesión válida pero sin permiso, `403`, sin modificar datos protegidos. Los servicios conservan las comprobaciones específicas de negocio, como pertenencia de una compra.
- Invalidar la sesión correspondiente al cerrar sesión; las peticiones posteriores con esa credencial deben rechazarse. Mantener el rechazo genérico de credenciales incorrectas y distinguir errores de disponibilidad.
- API y Web ya tienen sus propias clases de contrato, compatibles en JSON, sin biblioteca Shared. La separación está implementada y `MVP-001-06` está cerrada con aprobación de Enzo reportada por el usuario; la adaptación de los contratos al futuro mecanismo de sesión sigue pendiente.

**Detalles pendientes antes de implementar:** esquema y almacenamiento de sesiones, generación y transporte de la credencial, custodia por cliente, caducidad, comportamiento ante reinicios y fallos, y pruebas aisladas. La dirección anterior contemplaba un límite absoluto de siete días desde el login; simplificar la implementación no autoriza por sí solo cambiar ese requisito. Hay que confirmar su aplicación al nuevo mecanismo antes de escribirlo. Los IDs string prefijados de entidades no convierten credenciales, QR ni referencias externas en correlativos.

No hay login, sesión, logout ni permisos ejecutables acreditados. `MVP-001-03`, `MVP-001-04` y `MVP-001-05` conservan sus criterios, estados y dependencias; `FIN-005` es trabajo futuro y no bloquea su cierre cuando se demuestre el corte básico local.

## Estado del código existente

 Contratos iniciales de ingreso

**Implementado:** clases propias de API y Web, DataAnnotations y roles enum con el mismo contrato JSON de MVP-001-02; ya no existe el proyecto Shared. La verificación independiente y aprobación de Enzo reportada para MVP-001-02 pertenecen al corte anterior. **Separación cerrada:** el usuario confirmó que Enzo aprobó todos los criterios de `MVP-001-06`. **Pendiente:** autenticación ejecutable. No hay endpoints nuevos. El [backlog](../tasks.json) conserva la autoridad del cierre; el [scaffold API](api.md) sigue sin ingreso implementado.

## Aprobación y límites

El usuario detuvo el candidato inglés por sintaxis no discutida y aprobó explícitamente en el chat clases públicas españolas con `get; set;`, cadenas `string.Empty` y DataAnnotations con los mensajes de abajo. En ese corte confirmó bibliotecas de clases y referencias como enseñadas, por lo que Shared permaneció. Luego pidió eliminar el proyecto compartido y mantener clases propias en API y Web; esta separación no reabre el cierre histórico de MVP-001-02. Los extractos docentes reportan DataAnnotations aplicadas; DTO es un requisito autorizado, no cobertura docente demostrada.

Tras explicar enum y conversor, el usuario autorizó explícitamente `RolIngreso`, `RolIngreso[]` y `JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)` en las opciones MVC de la API. Para Web pidió solo un comentario sobre un futuro consumidor HTTP; no se agrega configuración dormida, DI, factory ni cliente ficticio. La aprobación se atribuye al chat, no a Enzo ni a evidencia docente del conversor. Los nombres físicos C# permanecen; no se reintroducen `sealed`, modificador `required`, `init` ni `JsonPropertyName`.

No se agregan endpoints, sesiones, tokens, bibliotecas de autenticación, UI, persistencia ni permisos. Los atributos validan datos cuando se invoca validación; no autentican ni prueban existencia del correo. No se exige fuerza, longitud o composición de contraseña, ni se normalizan credenciales. Los roles describen identidad, no autorizan acciones.

## Clases propias por aplicación

[API](../../src/Api/API-Berruti/API-Berruti/API-Berruti.csproj) define DTOs en `API_Berruti.DTOs.Authentication`; [Web](../../src/Web/Web-App/Web-App/Web-App.csproj) define modelos propios en `Web_App.Models.Authentication`. Ambos proyectos siguen siendo `net10.0`, sin referencia entre sí ni a una biblioteca de contratos compartida. No se comparten entidades de persistencia ni se agregan paquetes. La compatibilidad se mantiene mediante el contrato JSON y sus pruebas, no mediante identidad de clases o assemblies.

| Clase pública C# | DTO de API | Modelo de Web | Tipo equivalente Mobile |
| --- | --- | --- | --- |
| `SolicitudIngresoDto` | [AuthenticationRequest.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationRequest.cs) | [AuthenticationRequest.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationRequest.cs) | `SolicitudIngresoDto` |
| `RespuestaIngresoDto` | [AuthenticationSuccessResponse.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationSuccessResponse.cs) | [AuthenticationSuccessResponse.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationSuccessResponse.cs) | `RespuestaIngresoDto` |
| `RespuestaErrorIngresoDto` | [AuthenticationRejectionResponse.cs](../../src/Api/API-Berruti/API-Berruti/DTOs/Authentication/AuthenticationRejectionResponse.cs) | [AuthenticationRejectionResponse.cs](../../src/Web/Web-App/Web-App/Models/Authentication/AuthenticationRejectionResponse.cs) | `RespuestaErrorIngresoDto` |

Cada aplicación mantiene también su propio `RolIngreso`. Cambiar un lado requiere ejecutar las comprobaciones de compatibilidad; no se agrega un cliente HTTP ni se configura JSON en Web antes de tener un consumidor.

[authentication.ts](../../src/Mobile/src/contracts/authentication.ts) conserva exactamente las interfaces y la unión `RolIngreso = 'pasajero' | 'cobrador' | 'administrador'`; sus bytes no cambian. No existe un consumidor de ingreso.

## Campos y validación

| DTO | Propiedad C# / tipo | JSON MVC / TypeScript | Significado y reglas |
| --- | --- | --- | --- |
| Solicitud | `Correo: string` | `correo: string` | `Required`: `El correo es obligatorio.`; `EmailAddress`: `El correo no es válido.` |
| Solicitud | `Contrasena: string` | `contrasena: string` | `Required`: `La contraseña es obligatoria.`; no devolver ni registrar la credencial. |
| Éxito | `IdentificadorUsuario: string` | `identificadorUsuario: string` | Identificador público opaco interno; nunca visible al usuario ni credencial de sesión. No expone PK ni exige UUID/número. Generación/mapeo pendientes. |
| Éxito | `Roles: RolIngreso[]` | `roles: RolIngreso[]` | Array de textos españoles por el conversor MVC. Inicializador `new RolIngreso[0]`; sin cardinalidad mínima, orden obligatorio ni rol principal. |
| Rechazo | `Codigo: string` | `codigo: string` | Código genérico elegido: `ingresoRechazado`. |
| Rechazo | `Mensaje: string` | `mensaje: string` | `No se pudo ingresar con las credenciales proporcionadas.` |

Éxito y rechazo son DTOs separados sin envelope, ruta o estado HTTP definido; no incluyen credenciales. Email desconocido y contraseña incorrecta deberán recibir el mismo rechazo, todavía no implementado. Los mensajes de validación de formato/presencia no informan si existe una cuenta. No se modelan causas de red/servidor adicionales.

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

[Program.cs de Web](../../src/Web/Web-App/Web-App/Program.cs) contiene únicamente el recordatorio en español para un futuro cliente HTTP de API: deberá utilizar ese conversor para leer `RolIngreso`. No registra opciones HTTP JSON, DI ni cliente; no acredita integración. Mobile conserva textos españoles sin conversor runtime ni cambios de código.

## Evidencia por candidato

- **Candidato español anterior con constantes string:** el escritor observó RED de validación (ocho fallos esperados), GREEN nueve PASS y JSON doce PASS. La verificación independiente recibida del coordinador confirmó GREEN, JSON, builds/typecheck/convenciones/backlog/whitespace; no reprodujo RED. Su harness fue eliminado por el coordinador y se confirmó ausencia. Es evidencia histórica, no verificación del nuevo enum.
- **Candidato enum actual, observación del escritor:** antes del conversor, RED JSON exit 1, cinco fallos de veinte aserciones: salida numérica, roundtrip de textos, aceptación de números definidos/no definidos y serialización de enum no definido. Después del conversor: GREEN exit 0, veinte PASS. Validación sin cambios: nueve PASS. Builds API/Web, typecheck Mobile, convenciones y `git diff --check` pasan. Comprobación Python antes/después: JSON válido y huella idéntica del backlog excluyendo solo evidence de MVP-001-02, preservando otros 18 registros y todos los campos de estado/metadatos; huella de Mobile idéntica. Comandos de ejecución en [informe §7](../project-report.md#contratos-pasivos-mvp-001-02).
- **Verificación independiente:** JSON20 y validación9 PASS; builds API/Web, typecheck, convenciones, whitespace y backlog exit 0. RED5 permanece como evidencia del escritor. Revisión de Enzo pendiente. No se comprobó login, HTTP, UI, sesión o autorización.

## Comprobación vigente de separación y regresión

Desde la raíz del worktree:

```sh
dotnet run --project tests/AuthenticationContracts/AuthenticationContracts.csproj --no-launch-profile
```

El proyecto consola usa los proyectos reales API/Web y no agrega paquetes de pruebas. Comprueba clases públicas propias, ausencia de Shared y de dependencias entre aplicaciones, nombres de roles, campos y tipos JSON exactos, roundtrip, defaults, rechazos de enums y validaciones con sus mensajes originales en ambas aplicaciones. Compara con las opciones web y el conversor cuyo registro coteja en el código de API; no ejecuta su entrypoint, HTTP, UI ni autenticación. Devuelve exit 1 y mensajes `FAIL` por stderr si falla alguna comprobación; exit 0 si pasan todas. No requiere cuentas, base de datos ni servidores.

## Receta histórica del harness aislado

Esta receta documenta las pruebas del corte anterior con Shared. No es un comando vigente para el árbol actual: el harness externo fue eliminado y su referencia al proyecto compartido ya no existe. Usar la comprobación anterior para este corte, sin recrear recursos externos.

Se recreó `D:/CTC/Semestre-4/mvp-001-02-contracts-harness/` solo tras comprobar que no existía. Inventario propio: `Harness.csproj`, `Program.cs`, outputs `bin/` y `obj/`; el coordinador eliminó únicamente esos archivos generados tras verificar el inventario y la ausencia de enlaces simbólicos; confirmó que la carpeta ya no existe. La receta requiere recrearla para repetir las pruebas. Es consola `net10.0` sin paquetes, con `FrameworkReference` a `Microsoft.AspNetCore.App` y `ProjectReference` a Shared. No inicia un servidor.

```sh
dotnet run --project D:/CTC/Semestre-4/mvp-001-02-contracts-harness/Harness.csproj -- json
dotnet run --project D:/CTC/Semestre-4/mvp-001-02-contracts-harness/Harness.csproj -- validacion
```

**Opciones sometidas a prueba:** crear `ServiceCollection`, agregar logging y registrar `AddControllers().AddJsonOptions(...)` con el mismo cuerpo que la API. Resolver `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>.Value.JsonSerializerOptions` del proveedor. Cotejar el bloque completo del registro con la fuente API, normalizando solo CRLF/LF. El harness reproduce el registro y resuelve opciones MVC reales en memoria; no carga el entrypoint API ni ejecuta HTTP. Antes del conversor se cotejó y utilizó `AddControllers()` solo.

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
