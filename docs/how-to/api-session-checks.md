# Comprobar sesiones API locales

## Alcance y límites

T2 implementa solo API y adaptación pasiva de contratos Web/TypeScript. No implementa UI, almacenamiento cliente, transporte de negocio ni roles UI. Cuentas exclusivamente ficticias y locales; contraseñas temporalmente en texto plano por excepción aprobada, nunca en respuestas/logs. No desplegar. Las pruebas unitarias no prueban durabilidad, SQL real, HTTPS público ni comportamiento en iPhone.

## Configuración humana (no necesaria para Weather/OpenAPI)

La API consume `ConnectionStrings:Berruti` mediante la configuración estándar de ASP.NET; su variable de entorno equivalente es `ConnectionStrings__Berruti`. Proporcionarla privadamente en runtime, nunca en archivos versionados, URLs, argumentos públicos ni logs. No hay archivo privado nuevo creado por el agente. No enviar su valor al chat.

### Arranque opcional con `.env` (solo API, PowerShell 5.1)

El arranque nativo anterior sigue igual: ASP.NET **no carga `.env` automáticamente**.
El launcher explícito usa siempre `src/Api/API-Berruti/API-Berruti/.env`, relativo
al script, no al directorio de quien lo invoca. Desde la raíz:

```powershell
Copy-Item src/Api/API-Berruti/API-Berruti/api-local-config.example.txt src/Api/API-Berruti/API-Berruti/.env
# Completar privadamente ConnectionStrings__Berruti en el archivo copiado.
.\scripts\start-api-local.ps1
```

La copia vacía **no configura una DB sin contraseña**, no cambia cuentas ni prueba
login o TLS. Es texto plano, no una bóveda: proteger acceso local y copias de
respaldo; nunca compartir contenido, capturas o logs. Git excluye `.env` y sus
variantes privadas del mismo directorio. La plantilla pública vacía
`api-local-config.example.txt` es versionable; no contiene configuración privada.
No copiar esta configuración a Web, Mobile ni variables `EXPO_PUBLIC`.

Sintaxis deliberadamente pequeña: líneas vacías, comentarios con `#` al inicio
(admite espacios antes del comentario) y exactamente una línea
`ConnectionStrings__Berruti=valor`. Clave exacta, sin espacios alrededor del `=`;
valor no vacío, sin comillas exteriores (balanceadas o no), `export`, otras claves
ni duplicados. No hay interpolación, evaluación ni comentarios al final: `#`,
`;`, `=` y expresiones dentro del valor son datos literales. Se conserva todo lo
que sigue al primer `=`, incluidos espacios. No admite valores multilínea ni NUL.

Archivo ausente, ilegible o inválido: diagnóstico constante y exit 2 antes de
invocar dotnet, sin modificar el entorno. Solo se asigna la clave de conexión en
el entorno del proceso; se restaura su valor anterior en `finally`, también ante
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

Otros entornos/pares, `root`, remotos y falta de configuración fallan cerrados. Los usuarios deben mantener únicamente DML sobre su propia base, según la revisión humana de [aprovisionamiento](local-auth-database.md); el guard de conexión no demuestra sus grants. T6 prepara una [propuesta manual separada de esquema y pasajero demo](demo-passenger-setup.md), sin ejecución autorizada por este documento. El usuario reportó `berrutilocal` vacía; versión, inventario y definiciones requieren confirmación humana. La API puede responder `500` ante fallos inesperados de proveedor/configuración mientras no cuente con esquema/cuenta y configuración privada preparados, revisados y comprobados. Nunca crear automáticamente esquema, migraciones o cuentas en startup. Se suprimen diagnósticos EF que podrían exponer excepciones del proveedor; el límite de auth devuelve solo error genérico.

## Esquema para revisión humana, no ejecutado

Los scripts de [fundación](../../scripts/database/create-auth-foundation.sql) y sesiones descritos aquí son **solo de pruebas**, nunca para demo. La [propuesta T6 para la demo existente](demo-passenger-setup.md) usa archivos separados, no reprovisiona bases/cuentas y requiere revisión/ejecución manual por una persona, una sentencia por vez; nada se ejecutó por el agente.

La fundación incorpora `usuario.identificador_publico`, aleatorio estable, diferente de `US-n` y del token. Su restricción UNIQUE no admite colisiones. Si existe una fundación anterior sin ese campo, **detenerse**: no se puede asumir compatibilidad, aplicar ALTER, resetear contadores ni reutilizar fixtures a ciegas. Solicitar revisión administrativa sin compartir contraseñas.

[Sesiones](../../scripts/database/create-auth-sessions.sql) propone solo la tabla nueva `berrutiauthtests.sesion`; requiere inventario/SHOW CREATE y revisión de las cuatro tablas existentes. Ejecutar manualmente una sentencia por vez, parar al primer error, nunca todo el archivo. No crear segunda DB, cambiar grants ni aplicar ese archivo de pruebas en demo. DDL tiene commits implícitos; no hay rollback automático.

La sesión guarda huella SHA-256 de 64 hex como PK única, FK `usuario_id`, creación, vencimiento y revocación. Fechas `DATETIME(6)` son UTC de aplicación: creación se trunca a microsegundos antes de persistir/devolver; `vence = creada + 7 días`. Comparación exacta `ahora >= vence` invalida, sin sliding/refresh. No se necesita contador de sesiones. Colisión de digest falla el ingreso; jamás devuelve token de una sesión existente. Mapeo EF/proveedor/DDL real todavía sin comprobar.

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

El runner Sessions admite únicamente `--unit`; otro argumento imprime uso y exit 1. Comprueba políticas, DI real sin conexión, guardia transaccional, servicio con dobles manuales y handler/authorizer/middleware invocados en memoria sin servidor. No prueba persistencia ni pipeline público. Contratos coteja exactamente cuatro campos del éxito, strings nuevos y mensajes/roles anteriores. Correlativo ahora se consume mediante `ICorrelativo` de instancia; no hay llamadas estáticas. Composición manual de pruebas T1: `CuentasRepository(contexto, new ContadoresRepository(contexto, correlativo))`, compartiendo el mismo contexto; producción usa DI scoped para esos contratos. Los modos T1 `--mysql` / `--mysql-boundary` no se ejecutaron en T2; conservan sus puertas de configuración/fixture de [T1](mysql-persistence-checks.md).

## Matriz pública pendiente (solo tras autorización y TLS confiable)

Consultar rutas exactas/status en [API](../reference/api.md). No usar HTTP para enviar contraseñas o tokens, ni desactivar validación de certificados. No registrar cuerpos de ingreso/respuestas ni encabezados Authorization. No modificar firewall, confianza OS, LAN o despliegue como parte de estas comprobaciones.

1. HTTPS: ingreso de cuenta pasajera ficticia activa → `200`, token opaco y vencimiento; sin contraseña ni PK. Desconocido, contraseña incorrecta e inactiva → mismo `401`/`ingresoRechazado`/mensaje; formato/presencia inválidos → `400` con mensajes originales.
2. Sin Bearer, mal formado, desconocido, expirado, revocado o usuario inactivo → `401` con challenge Bearer en operaciones protegidas.
3. Pasajero → identidad y prueba-pasajero `200`; prueba-administrador `403`, sin mutaciones. Cambiar membresía por mecanismo humano autorizado y verificar lectura autoritativa siguiente, sin claims cliente.
4. Salida → `204`; mismo token luego → `401`; otra sesión del mismo usuario sigue válida. Reiniciar API y comprobar persistencia sin renovar vencimiento. Probar límite exacto con reloj controlado en entorno de pruebas, nunca ajustando reloj del OS ni declarando unidades como prueba DB.
5. HTTP sensible con valores ficticios no secretos → `400` sin Location ni procesamiento; scaffold sin Bearer mantiene su camino original. Fallo inesperado capturado por el límite de autenticación (incluidos configuración ausente/proveedor caído) → auth `500`, JSON exacto `{"codigo":"autenticacionNoDisponible","mensaje":"La autenticación no está disponible. Intentá nuevamente."}` y `Cache-Control: no-store`, sin detalles privados; Weather/OpenAPI continúan sin requerir DB. OpenAPI solo Development como antes.

**Límite histórico de esta unidad T2:** no se ejecutaron servidores ni SQL en ella, ni acredita S2/S3 completo. El [informe](../project-report.md) registra la evidencia pública posterior de Web/API y los pendientes vigentes; esta matriz no sustituye esa cronología. Integración Web/Mobile pertenece a T3/T4.

## Durabilidad con dos reinicios de API

La [guía del diagnóstico con pausas y resultado observado](api-session-durability-checks.md#resultado-local-observado-dos-reemplazos-reales-de-proceso) registra la prueba local reportada por el padre (observación documental 2026-10-28T11:18:29-03:00): un ingreso, mismo token protegido 200 tras el primer reinicio, salida 204 y mismo token anterior 401 tras el segundo. La persona ingresó la contraseña privadamente; con permiso explícito el agente realizó los clics y ambos reinicios, con identidad/parada/nuevo arranque de procesos 18564→20708→400 comprobados externamente al panel. Se conserva la opción manual y todas las salvaguardas. No se reejecutó la matriz inmediata histórica ni se atribuye verificación independiente en vivo. Quedan pendientes los demás casos de la matriz no acreditados por el informe: otras sesiones, invariancia DB/contadores, SQL fresco/concurrencia/rollback, vencimiento y fallos/reintento integrados, Mobile/iPhone (diferido) y revisión de Enzo. No prueba reinicio de MySQL Server ni seguridad integral; no completa el backlog.
