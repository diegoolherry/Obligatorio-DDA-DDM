# Comprobar la autenticación JWT de la API local

## Alcance y límites

La API autentica con JWT HS256 sin estado desde FIN-005 (decisión de Diego, 2026-11-18; ver [contratos](../reference/auth-contracts.md#decisiones-de-fin-005-jwt-y-hash-de-contraseñas)). Cuentas exclusivamente ficticias y locales, con contraseña guardada solo como hash `IPasswordHasher<Usuario>`; nunca en respuestas ni logs. **No desplegar ni usar credenciales reales** sin autorización humana explícita e independiente. Las pruebas unitarias no prueban SQL real, HTTPS público ni comportamiento en iPhone. Esta guía conserva su nombre por compatibilidad de enlaces: la sesión de servidor ya no existe.

## Configuración humana (no necesaria para Weather/OpenAPI)

La API consume `ConnectionStrings:Berruti` (variable `ConnectionStrings__Berruti`) y la clave de firma `Jwt:Key` (variable `Jwt__Key`, mínimo **32 bytes**) mediante la configuración estándar de ASP.NET. `Jwt:Issuer` y `Jwt:Audience` no son secretos y están en `appsettings.json`. Proporcionar ambos secretos privadamente en runtime, nunca en archivos versionados, URLs, argumentos públicos ni logs. Sin `Jwt__Key` válida el arranque normal termina con código 2 (`Configuración JWT inválida`). No enviar sus valores al chat.

### Arranque opcional con `.env` (solo API, PowerShell 5.1)

El arranque nativo anterior sigue igual: ASP.NET **no carga `.env` automáticamente**.
El launcher explícito usa siempre `src/Api/API-Berruti/API-Berruti/.env`, relativo
al script, no al directorio de quien lo invoca. Desde la raíz:

```powershell
Copy-Item src/Api/API-Berruti/API-Berruti/api-local-config.example.txt src/Api/API-Berruti/API-Berruti/.env
# Completar privadamente ConnectionStrings__Berruti y Jwt__Key (aleatoria, >= 32 bytes) en el archivo copiado.
.\scripts\start-api-local.ps1
```

La copia vacía **no configura una DB sin contraseña ni una clave JWT**, no cambia cuentas ni prueba
login o TLS. Es texto plano, no una bóveda: proteger acceso local y copias de
respaldo; nunca compartir contenido, capturas o logs. Git excluye `.env` y sus
variantes privadas del mismo directorio. La plantilla pública vacía
`api-local-config.example.txt` es versionable; no contiene configuración privada.
No copiar esta configuración a Web, Mobile ni variables `EXPO_PUBLIC`.

Sintaxis deliberadamente pequeña: líneas vacías, comentarios con `#` al inicio
(admite espacios antes del comentario) y exactamente una línea
`ConnectionStrings__Berruti=valor` y una línea `Jwt__Key=valor` (ambas obligatorias).
Opcionalmente, a lo sumo una línea de cada clave TEST de Mercado Pago:
`MercadoPago__AccessToken`, `MercadoPago__ExpectedSellerId`, `MercadoPago__ExpectedSiteId`
y `MercadoPago__TestCredentialOriginConfirmed`, con las mismas reglas. Clave exacta, sin espacios alrededor del `=`;
valor no vacío, sin comillas exteriores (balanceadas o no), `export`, otras claves
ni duplicados. No hay interpolación, evaluación ni comentarios al final: `#`,
`;`, `=` y expresiones dentro del valor son datos literales. Se conserva todo lo
que sigue al primer `=`, incluidos espacios. No admite valores multilínea ni NUL.

Archivo ausente, ilegible o inválido: diagnóstico constante y exit 2 antes de
invocar dotnet, sin modificar el entorno. Solo se asignan esas claves (las dos obligatorias y las opcionales presentes) en
el entorno del proceso; una opcional ausente no modifica el valor que ya tenga la terminal; se restauran sus valores anteriores en `finally`, también ante
fallo del hijo. No modifica entorno User/Machine. Invoca `dotnet run --project`
con el proyecto existente y `--launch-profile https` por defecto, sin secretos en
argumentos; devuelve el exit del hijo. Fallo al invocarlo: mensaje genérico/exit 2.
`-Profile http` selecciona el perfil de scaffold existente, **no** habilita envío
seguro de credenciales; no evita guardias ni validación de certificados.

Prueba determinista del launcher público con transporte dotnet ficticio (sin
servidor/SQL), desde la raíz: `powershell -NoProfile -File tests/ApiLocalEnv/check.ps1`.
Sus fixtures contienen solo valores sintéticos y quedan bajo `tests/ApiLocalEnv/fixtures/`.

El guard exige servidor `localhost`, `127.0.0.1` o `::1`, puerto 3306 y contraseña no vacía, con **exactamente** uno de estos pares:

| Entorno ASP.NET | Usuario/base |
| --- | --- |
| `Development` | `berruti_app` / `berrutilocal` |
| `AuthenticationTests` (modo aislado explícito) | `berruti_test` / `berrutiauthtests` |

Otros entornos/pares, `root`, remotos y falta de configuración fallan cerrados. Los usuarios deben mantener únicamente DML sobre su propia base, según la revisión humana de [aprovisionamiento](local-auth-database.md); el guard de conexión no demuestra sus grants. T6 prepara una [propuesta manual separada de esquema y pasajero demo](../archive/demo-passenger-setup.md), sin ejecución autorizada por este documento. El usuario reportó `berrutilocal` vacía; versión, inventario y definiciones requieren confirmación humana. La API puede responder `500` ante fallos inesperados de proveedor/configuración mientras no cuente con esquema/cuenta y configuración privada preparados, revisados y comprobados. Nunca crear automáticamente esquema, migraciones o cuentas en startup. Se suprimen diagnósticos EF que podrían exponer excepciones del proveedor; el límite de auth devuelve solo error genérico.

## Esquema para revisión humana, no ejecutado

Los scripts de [fundación](../../scripts/database/create-auth-foundation.sql) y sesiones descritos aquí son **solo de pruebas**, nunca para demo. La [propuesta T6 para la demo existente](../archive/demo-passenger-setup.md) usa archivos separados, no reprovisiona bases/cuentas y requiere revisión/ejecución manual por una persona, una sentencia por vez; nada se ejecutó por el agente.

La fundación incorpora `usuario.identificador_publico`, aleatorio estable, diferente de `US-n` y del token. Su restricción UNIQUE no admite colisiones. Si existe una fundación anterior sin ese campo, **detenerse**: no se puede asumir compatibilidad, aplicar ALTER, resetear contadores ni reutilizar fixtures a ciegas. Solicitar revisión administrativa sin compartir contraseñas.

**Obsoleto desde FIN-005.** [`create-auth-sessions.sql`](../../scripts/database/create-auth-sessions.sql) queda solo como historial: no hay sesión de servidor y la migración `QuitarSesion` elimina la tabla `sesion` ([script generado](../../scripts/database/20261118162624_QuitarSesion.sql)). El esquema vigente lo crean las migraciones de EF Core; para una base local, [recrearla con `--reset-local`](local-database-reset.md). Las bases locales son descartables; una con contraseñas en texto plano no autentica (se rechaza con el mismo `401`) y se recrea en lugar de migrarse.

## Pruebas sin red ni DB

Desde raíz, en primer plano:

```sh
dotnet run --project tests/AuthenticationSessions/AuthenticationSessions.csproj --no-launch-profile -- --unit
dotnet run --project tests/AuthenticationContracts/AuthenticationContracts.csproj --no-launch-profile
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --unit
dotnet build src/Api/API-Berruti/API-Berruti.slnx --no-restore --nologo
dotnet build src/Web/Web-App/Web-App.slnx --no-restore --nologo
```

Solo si existen dependencias Mobile:

```sh
cd src/Mobile && npm run typecheck
```

El runner Sessions admite únicamente `--unit`; otro argumento imprime uso y exit 1. Comprueba emisión y validación del JWT (emisor, audiencia y vencimiento; el alcance exacto se lee en `tests/AuthenticationSessions/Program.cs`), hash de contraseñas, `salida` sin estado, roles 401/403, DI real sin conexión y el pipeline en memoria sin servidor. No prueba persistencia ni el pipeline público. Contratos coteja exactamente cuatro campos del éxito, strings nuevos y mensajes/roles anteriores. Correlativo ahora se consume mediante `ICorrelativo` de instancia; no hay llamadas estáticas. Composición manual de pruebas T1: `CuentasRepository(contexto, new ContadoresRepository(contexto, correlativo))`, compartiendo el mismo contexto; producción usa DI scoped para esos contratos. Los modos T1 `--mysql` / `--mysql-boundary` no se ejecutaron en T2; conservan sus puertas de configuración/fixture de [T1](mysql-persistence-checks.md).

## Matriz pública pendiente (solo tras autorización y TLS confiable)

Consultar rutas exactas/status en [API](../reference/api.md). No usar HTTP para enviar contraseñas o tokens, ni desactivar validación de certificados. No registrar cuerpos de ingreso/respuestas ni encabezados Authorization. No modificar firewall, confianza OS, LAN o despliegue como parte de estas comprobaciones.

1. HTTPS: ingreso de cuenta pasajera ficticia activa → `200`, JWT y vencimiento; sin contraseña ni PK. Desconocido, contraseña incorrecta e inactiva → mismo `401`/`ingresoRechazado`/mensaje; formato/presencia inválidos → `400` con mensajes originales.
2. Sin Bearer, mal formado, alterado, firmado con otra clave, de otro emisor/audiencia o expirado → `401` con challenge Bearer en operaciones protegidas. Un usuario desactivado o con rol cambiado **conserva el efecto hasta el vencimiento** (riesgo aceptado): no se espera `401`.
3. Pasajero → identidad y prueba-pasajero `200`; prueba-administrador `403`, sin mutaciones. Un rol declarado por el cliente no concede acceso; los roles salen del token firmado por la API al ingresar.
4. Salida → `204` sin estado en el servidor: **no** se espera `401` con el mismo token después. Probar el límite de vencimiento con reloj controlado en entorno de pruebas, nunca ajustando el reloj del OS ni declarando unidades como prueba DB.
5. HTTP sensible con valores ficticios no secretos → `400` sin Location ni procesamiento; scaffold sin Bearer mantiene su camino original. Fallo inesperado capturado por el límite de autenticación (incluidos configuración ausente/proveedor caído) → auth `500`, JSON exacto `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}` y `Cache-Control: no-store`, sin detalles privados; Weather/OpenAPI continúan sin requerir DB. OpenAPI solo Development como antes.

**Límite histórico de esta unidad T2:** no se ejecutaron servidores ni SQL en ella, ni acredita S2/S3 completo. El [informe](../project-report.md) registra la evidencia pública posterior de Web/API y los pendientes vigentes; esta matriz no sustituye esa cronología. Integración Web/Mobile pertenece a T3/T4.

## Durabilidad y revocación: ya no aplican

Con JWT sin estado no hay sesión en MySQL que sobreviva o se revoque: un token firmado sigue válido tras reiniciar la API (misma clave) y hasta `exp`. La [guía anterior de durabilidad](../archive/api-session-durability-checks.md) se conserva como historial del mecanismo retirado y no es una comprobación vigente.
