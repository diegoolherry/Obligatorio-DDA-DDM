# Comprobar la base de cuentas y contadores (T1)

Este corte agrega persistencia **sin conectarla a la API**: no cambia HTTP, DTO,
`Program.cs`, Web ni Mobile. Cubre la base del contador de S5, la política de S8
y las restricciones de S9; no implementa tokens, sesiones ni UI (T2–T4).
Solo cuentas ficticias locales; contraseñas temporalmente legibles según el
[contrato aprobado](../reference/auth-contracts.md). Nunca desplegar ni usar datos reales.

## Preparación humana, no automatizada

1. Leer [primer arranque](../tutorials/first-local-run.md),
   [cliente/API](client-api-configuration.md) y el
   [estado del aprovisionamiento](local-auth-database.md). Las dos bases/cuentas
   ya fueron reportadas por el usuario; **no repetir bootstrap ni ampliar grants**.
2. Un administrador revisa [create-auth-foundation.sql](../../scripts/database/create-auth-foundation.sql).
   Solo `berrutiauthtests`, MySQL 8.0.16+ con CHECK efectivos e InnoDB.
   Ejecutar primero el inventario: si devuelve tablas o errores, detenerse y
   revisar definiciones; no asumir compatibilidad de un esquema previo.
   Crear **una sentencia por vez**, detenerse al primer error. DDL produce
   commits implícitos: no es atómico ni idempotente. No hay DROP, ALTER,
   migraciones, limpieza, usuarios, grants ni tablas de sesiones.
3. La creación termina con las cuatro tablas vacías, **sin contadores**. Antes
   de cualquier asignación, revisar y elegir **exactamente una** fixture inicial:
   [normal](../../scripts/database/seed-auth-tests-normal.sql) (`US-`/`RO-` en 0)
   **o** [frontera](../../scripts/database/seed-auth-tests-boundary.sql) (ambos en
   49998). Esta última es artificial, no historia observada ni avance de una fila
   existente. Cada archivo contiene un único INSERT guardado: inserta ambas filas
   juntas solamente si las cuatro tablas están vacías; con datos no hace nada.
   Revisar el postcheck: exactamente dos filas esperadas y `fixture_fresca_correcta=1`.
   Si no coincide, detenerse; no intentar la otra semilla ni reparar datos.
   No ejecutar ambos archivos ni hacerlo con otros escritores. No volver a insertar,
   reiniciar, adelantar ni borrar contadores históricos. El runner vuelve a comprobar
   la fixture elegida por lectura **antes de cualquier DML**, incluso si se ignoró
   el preflight manual. El usuario runtime solo tiene cuatro DML; no aplica DDL.
4. Crear personalmente `.local/mysql-tests.json` en la raíz del worktree.
   Está excluido por la entrada exacta `/.local/mysql-tests.json` en `.gitignore`.
   No compartir, versionar, mostrar en terminal ni pasar a herramientas de agentes.
   Restringir su lectura local al usuario humano pertinente.

El archivo es un objeto JSON con **exactamente estas cinco claves** (casing exacto):

| Clave | Tipo y valor |
| --- | --- |
| `Server` | string: `localhost`, `127.0.0.1` o `::1` |
| `Port` | número: `3306` |
| `Database` | string: `berrutiauthtests` |
| `User` | string: `berruti_test` |
| `Password` | string no vacío: secreto fijado personalmente en Workbench |

No se proporciona un bloque JSON ejecutable con contraseña de ejemplo. Escribir
el secreto únicamente en el archivo privado. No hay otras claves consumidas,
variables de entorno ni configuración de conexión de la API en T1.
El runner valida esos valores **antes de abrir conexión**, usa un constructor de
cadena tipado y exige luego `CURRENT_USER() = berruti_test@localhost` e InnoDB en
las cuatro tablas. Rechaza demo, root, hosts remotos, campos desconocidos y JSON
mal formado sin mostrar contenido, cadena de conexión ni excepciones privadas.
No demuestra aislamiento de permisos contra la demo; no intenta acceder a ella.

## Inspección automática de solo lectura (T15)

Antes de elegir o cargar una fixture, una persona prepara **privadamente** el
archivo de cinco claves anterior. No compartirlo con agentes ni copiar secretos
al chat. La inspección reutiliza el proveedor instalado; no necesita Workbench,
CLI MySQL, paquetes nuevos ni la configuración de la API.

```sh
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --help
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --inspect
```

`--inspect` conserva el mismo parser JSON y destino restringido. Conecta con una
conexión dedicada, comprueba **CURRENT_USER() y DATABASE() reales**, exige
`berruti_test@localhost` y `berrutiauthtests`, y luego usa
`START TRANSACTION READ ONLY`. Siempre intenta `ROLLBACK` al salir de ese bloque
y libera comandos, lectores y conexión, también ante errores. Timeout por comando:
5 segundos (conexión: 3); mantener además el límite externo de 30 segundos.

No exige cuatro tablas ni semilla: informa objetos ausentes, extras y tablas
vacías sin crearlos ni repararlos. La rama termina antes de las puertas de fixture
y de todo repositorio/DML de los otros modos. No permite SQL por argumentos.
Devuelve únicamente:

- Inventario de nombres/tipos/motores de tablas y vistas; columnas con nombre,
  tipo básico `DATA_TYPE`, nulabilidad y collation. No muestra literales de tipos
  ENUM/SET, defaults, comentarios ni expresiones SQL.
- Nombres/tipos de restricciones, columnas de claves y referencias FK; nombres,
  tabla, momento y evento de triggers, **nunca cuerpos ni expresiones CHECK**.
- Conteos solo de `BASE TABLE` conocidas: `usuario`, `rol`, `usuario_rol`,
  `contador_entidad`, `sesion`. No cuenta vistas (incluso con esos nombres) ni
  tablas desconocidas. No consulta filas de cuentas/sesiones, correos, contraseñas,
  huellas ni tokens.
- `Contadores: [inválidas,cantidadUS,mínimoUS,máximoUS,cantidadRO,mínimoRO,máximoRO]`.
  Los mínimos/máximos solo consideran valores entre 0 y 50000; sin filas válidas
  son `null`. Prefijos desconocidos/nulos y valores nulos/fuera de rango se cuentan
  como inválidos sin mostrarlos. Los prefijos se comparan de forma binaria. Antes
  del agregado se exige metadato `prefijo` CHAR/VARCHAR y `ultimo` entero; tipos
  incompatibles o resultados textuales producen un fallo genérico, no un eco.

Salida acotada: hasta 128 tablas/vistas, 1024 columnas, 512 restricciones,
1024 componentes de claves y 128 triggers, con campos de hasta 256 caracteres.
Superar un límite falla sin informe parcial. Los metadatos se serializan como
arrays JSON para escapar controles. La visibilidad depende de los permisos de
la cuenta: un inventario vacío no certifica ausencia absoluta ni compatibilidad.
No se ejecuta SHOW CREATE, SHOW GRANTS, DDL, DML, cambios de permisos ni resets.

Exit 0 y «Inspección ejecutada» significan solo que se terminó la inspección y su
limpieza; **no son PASS normal/frontera, validación completa del DDL, aislamiento
de privilegios ni autorización para sembrar o ejecutar DML**. El coordinador debe
revisar los metadatos antes de cualquier siguiente paso. Ausencia de configuración,
rechazos o errores devuelven 1 sin detalles del proveedor ni secretos. La preparación
y las pruebas sintéticas no acreditan una inspección real de MySQL.

Pruebas públicas sin MySQL (conexiones falsas y raíces CLI con configuración
exclusivamente sintética bajo `tests/MySqlInspection/fixtures/cli-<GUID>/`):

```sh
dotnet run --project tests/MySqlInspection/MySqlInspection.csproj --no-launch-profile
node --test tests/MySqlInspection/*.test.mjs
```

Los probes copian solo DLLs/deps/runtimeconfig públicos a esas raíces; nunca leen
el archivo privado del worktree ni la configuración API. Sus fixtures generadas
están ignoradas de forma precisa y no se usan para conectar. No demuestran
comportamiento del servidor/proveedor real.

## Ejecutar desde la raíz

SDK .NET 10. Proveedor Oracle fijado: `MySql.EntityFrameworkCore` **10.0.9**;
no cambiar target framework, instalar CLI ni actualizar otros paquetes.
Cada comando se ejecuta en primer plano, con límite externo de **30 segundos**;
un timeout es fallo, no reintentar automáticamente ni dejar procesos en segundo plano.

```sh
dotnet restore src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --disable-parallel
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --unit
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --mysql
dotnet run --project tests/AuthenticationContracts/AuthenticationContracts.csproj --no-launch-profile
dotnet build src/Api/API-Berruti/API-Berruti.slnx --no-restore --nologo
git diff --check
```

- `--unit`: política pura de inicio 1, límite inclusivo 50000, formato sin relleno,
  prefijos, agotamiento/estados inválidos y validación de destino privado. Exit 0
  solo si todo pasa. No conecta ni acredita bloqueo, durabilidad o concurrencia.
- Ambos modos MySQL: sin archivo privado, exit 1 y mensaje accionable con su ruta;
  **no es skip ni PASS**. No ejecutar hasta revisión administrativa del esquema y
  fixture elegida. Exigen cuentas/roles/membresías vacías y exactamente dos
  contadores con el valor inicial del escenario; un estado usado, incorrecto o
  incompleto se rechaza antes de DML. No ejecutar con otros escritores simultáneos.
- `--mysql`: requiere fixture **normal en 0**. Verifica altas/lecturas públicas,
  membresía, contadores independientes, rechazo por unicidad/FK y entrada inválida
  sin cambio de estado, no reutilización después de borrar exclusivamente su propia
  fixture y cuatro altas simultáneas mediante contextos separados. Conserva nuevas
  fixtures `t1-<marca>@example.invalid` y consume números históricos; no vacía tablas
  ni limpia ejecuciones previas. Exit 0 solo si todas sus aserciones pasan;
  **no acredita la frontera MySQL**.
- `--mysql-boundary`: requiere fixture **artificial fresca en 49998**. Crea roles
  `RO-49999`/`RO-50000` con `Pasajero`/`Cobrador` por `CrearRolAsync`, luego rechaza
  un tercer rol `Administrador` por agotamiento exacto, no por duplicado. Crea
  `US-49999`/`US-50000` por `CrearUsuarioAsync`, comprueba sus lecturas/roles públicos
  y rechaza un tercer usuario por el mismo agotamiento. Cada rechazo compara un
  snapshot completo de usuarios, roles, membresías y ambos contadores, sin imprimirlo.
  Verifica persistencia y numeración independiente tras cada alta. Exit 0 solo si
  todas sus aserciones pasan; **no acredita concurrencia ni el escenario normal**.

El comando de frontera, **alternativo** al normal sobre una preparación fresca, es:

```sh
dotnet run --project tests/MySqlPersistence/MySqlPersistence.csproj --no-launch-profile -- --mysql-boundary
```

**T1 necesita evidencia de ambos escenarios.** La misma base instalada no puede
correrlos secuencialmente después de escrituras históricas: cada escenario consume
su fixture y la puerta rechaza repetirla. Se requiere otra preparación fresca
**expresamente autorizada por una decisión humana** para obtener la otra evidencia.
Este documento no autoriza borrar/recrear tablas, reseedear historia ni agregar
otra base. Solicitar esa decisión al coordinador; no improvisar limpieza o resets.
El archivo privado mantiene exactamente las mismas cinco claves en los tres modos de conexión.
- Errores operativos producen exit 1 sin detalles sensibles. No hay reparación,
  migración ni cleanup automáticos. Conservar evidencia sin secretos y consultar
  al administrador; nunca ampliar permisos para sortear errores.

## Contrato de repositorios y transacción

`CuentasRepository` crea roles válidos del enum existente y usuarios con membresía
como una sola alta. Cada operación requiere un contexto sin cambios pendientes
ni transacción externa; usar un contexto por operación concurrente. Correo y
contraseña no se normalizan. `ArgumentException` señala entrada inválida;
`InvalidOperationException` señala contador ausente, corrupto o agotado/contexto
ocupado. Fallos del proveedor, FK o unicidad se propagan para manejo interno,
**nunca para serializar ni registrar la excepción/contraseña**. No son aún respuestas HTTP.

El alta abre una transacción MySQL y ejecuta un `SELECT ultimo ... WHERE prefijo =
@prefijo FOR UPDATE` sobre una fila preparada. Valida el siguiente número,
actualiza esa misma fila con SQL parametrizado, inserta el registro y membresía,
y confirma juntos. El bloqueo exclusivo de InnoDB dura hasta commit/rollback;
los otros escritores de ese prefijo esperan. No usa `MAX`, no crea contadores
al asignar y no confirma números separados del registro. Un fallo revierte todo
el alta; el repositorio separa del tracking sus entidades fallidas. Borrar una
cuenta no modifica el contador. La PK de `usuario_rol` es compuesta, sin otro ID.
No hay repositorio genérico, mocks de atomicidad ni middleware dormido.

## Evidencia y pendientes

El RED observado fue la política aún no implementada (exit 1); el GREEN unitario
comprueba reglas puras, no MySQL. La restauración real del proveedor en net10
pasó. Sin configuración privada provista ni ejecución humana del esquema, las
pruebas vivas de SQL/proveedor, rollback, concurrencia, primer número, borrado y
frontera siguen **sin acreditar**. La revisión estática del SQL y compilar no
sustituyen esos resultados. No aplicar este esquema en `berrutilocal` todavía.

### Hallazgo real y corrección manual acotada (T18/S20)

La inspección real posterior del padre ([L96–L98](../../odd/tasks/mvp-001-03-passenger-auth.md)) terminó exit 0 en 3 segundos: cuatro `BASE TABLE` InnoDB, todas con conteo 0, contadores ausentes y `sesion` ausente (aceptable para T1). Sustituye la falta de conexión del snapshot anterior, no acredita DDL compatible ni PASS normal/frontera. La PK real de `usuario_rol` tiene solo `usuario_id` en posición 1; falta `rol_id` en posición 2. El CREATE canónico y el mapeo EF ya usan la pareja correcta. Los metadatos básicos no prueban longitudes VARCHAR completas ni semántica de CHECK.

El usuario aprobó **preparar**, no ejecutar, [la corrección manual](../../scripts/database/repair-auth-tests-membership-primary-key.sql). Un administrador en Workbench debe revisar consultas frescas: cuatro tablas vacías, columnas esperadas NOT NULL, PK simple exacta, ambas FK conservadas y ninguna referencia entrante desde cualquier esquema, con visibilidad suficiente. Sin otros escritores/administradores, selecciona una sentencia por vez y para al primer error o estado inesperado; si la pareja ya es correcta, no reaplica. Las puertas son humanas, no protección automática de lote. DDL tiene commits implícitos y no garantía de rollback; ante fallo no reintentar ni eliminar FK.

`berruti_test` sigue DML-only, sin ampliar privilegios. No repetir creación canónica, sembrar, borrar datos/tablas ni ejecutar normal/frontera todavía. No se ejecutó el parche. Tras confirmación humana de ejecución y postchecks PK/FK/conteos, el coordinador reutilizará `--inspect` una vez; la API debe permanecer apagada por elección humana. T1 y MVP-001-03 continúan bloqueados por evidencia pendiente.
