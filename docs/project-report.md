# Obligatorio Berruti — Informe académico del proyecto

**Última actualización:** 2026-11-19

> **Qué es este documento:** la fuente en Markdown del informe académico; la versión Word se genera a partir de él. El sistema es un proyecto académico local, con datos ficticios y credenciales de prueba. No es un producto desplegado ni representa a la empresa que inspiró el caso.
>
> **Leyenda:** **Verificado** = respaldado por una prueba o un PR enlazado; **Decidido** = elección del equipo; **Planificado** = trabajo pendiente; **Pendiente** = falta una decisión o una evidencia.
>
> **Autoridad:** el estado de cada tarea lo define [`tasks.json`](tasks.json); este informe lo resume y no lo reemplaza. Los PR se enlazan por su número en [el repositorio](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pulls?q=is%3Apr+is%3Amerged).

## 1. Resumen y estado actual

**Obligatorio Berruti** es una plataforma de venta y validación de pasajes y abonos por tramo, inspirada en un operador real de transporte interdepartamental (Berrutti), sin afiliación ni aval. Tiene tres piezas:

- una **API** ASP.NET (`net10.0`) con Entity Framework Core y MySQL;
- una aplicación **Web** Blazor Web App con `InteractiveServer`, para pasajeros y administración;
- una aplicación **Mobile** Expo / React Native (TypeScript), para pasajeros y cobradores.

Estado del backlog al 2026-11-19, contado desde [`tasks.json`](tasks.json): 42 registros (23 originales y 19 hijos). Entre los 23 originales hay 11 `done`, 2 `in_progress` (MVP-004 y FIN-005) y 10 `pending`; los 19 hijos están `done`.

| Tema | Estado | Evidencia |
| --- | --- | --- |
| Autenticación y roles (MVP-001) | **Verificado** (`done`) | Ver [§5](#5-cortes-implementados). |
| Administración operativa y demo (MVP-002) | **Verificado** (`done`) | PR #61–#68. |
| Búsqueda de itinerarios (MVP-003) | **Verificado** (`done`) | PR #69–#71. |
| Compra, pago TEST y tickets (MVP-004) | **Implementado y fusionado**; en `tasks.json` figura `in_progress` | PR #121–#131; recorrido real con Mercado Pago TEST el 2026-11-19 ([§7](#7-calidad-y-evidencia-de-pruebas)). |
| Ubicación de demostración (MVP-009) | **Verificado** (`done`) | PR #72–#74. |
| Catálogo, administración y diseño (FIN-001, 008, 010, 011, 012) y análisis offline (FIN-006) | **Verificado** (`done`) | Ver [§5](#5-cortes-implementados). |
| JWT sin estado (FIN-005) | **Implementado y fusionado**; en `tasks.json` figura `in_progress` | PR #86–#89 y #120. |
| QR y validación (MVP-005) y siguientes | **Planificado** (`pending`) | Ver [§8](#8-riesgos-y-pendientes). |
| Despliegue | **Fuera de alcance** | Solo entorno local con datos ficticios. |

## 2. Contexto, problema y objetivos

- **Problema:** permitir a un pasajero buscar y comprar un itinerario de varios tramos, presentar un QR por tramo y consultar abonos; al cobrador, validar esos QR; y al administrador, preparar los datos de la demostración.
- **Objetivo demostrable:** el recorrido Ombúes → Radial de Conchillas → Colonia con dos tickets, validación de un solo uso y dos abonos con saldos independientes, más una consulta básica de ubicación. Criterios en [MVP §2](explanation/mvp.md#2-criterio-de-éxito-del-mvp).
- **Límites:** sin cobro real, sin datos financieros reales, sin integración con la empresa, sin validación sin conexión ni predicción avanzada ([MVP §13](explanation/mvp.md#13-fuera-del-mvp)). La entrega final suma catálogo y calendarios, planos académicos de vehículos y selección web de asientos ([alcance final](reference/final-scope.md)); esas ampliaciones son elección del equipo, no requisitos explícitos de la consigna.

## 3. Requisitos y trazabilidad

| Entregable | Fuente vigente |
| --- | --- |
| Actores, flujos y reglas de negocio | [MVP](explanation/mvp.md) |
| Requisitos funcionales y no funcionales | [Requisitos](reference/requirements.md) |
| Historias y criterios de aceptación | [Historias de usuario](reference/user-stories.md) |
| Contratos de autenticación | [Contratos de autenticación](reference/auth-contracts.md) |
| API implementada | [Referencia de la API](reference/api.md) |
| Modelo de datos | [Modelo UML y ER](explanation/data-model.md), [entidades explicadas](explanation/entities-explained.md) |
| Estado, dependencias y responsables | [Backlog](tasks.json) |

**Pendiente:** una matriz final requisito → historia → tarea → prueba. Hoy la trazabilidad se apoya en los criterios de aceptación de cada tarea y en el PR que la entrega.

## 4. Solución y decisiones tecnológicas

### Arquitectura

| Capa | Decisión |
| --- | --- |
| API | Controllers → Services → Repositories → `DbContext` de EF Core, cada Service y Repository detrás de su propia interfaz. DTOs propios, separados de las entidades. MySQL como motor; el esquema se define con migraciones de EF Core. |
| Web | Blazor Web App con `InteractiveServer` por página. Modelos y servicios propios, sin biblioteca compartida con la API. La sesión vive en memoria del circuito. |
| Mobile | Expo SDK 58 y React Native con TypeScript, vistas según el rol (pasajero y cobrador), NativeWind y el [sistema de diseño](explanation/ux-design.md). La sesión vive solo en memoria. |
| Contratos | DTOs en la API y modelos propios en cada cliente, con JSON compatible; no hay referencia entre aplicaciones ([referencia](reference/auth-contracts.md)). |

El modelo conceptual (ER y UML) fue aprobado como base evolutiva por Diego y Enzo y está en [data-model.md](explanation/data-model.md). Los identificadores son `string` con prefijo de dos letras y guion; ver el [catálogo](explanation/data-model.md#identificadores-de-las-entidades).

### Decisiones

| Decisión | Detalle | Origen |
| --- | --- | --- |
| **JWT sin estado** | HS256, vigencia de **7 días**, contraseñas guardadas solo como hash con `IPasswordHasher<Usuario>`. No se adoptan OpenIddict ni Identity. Riesgo aceptado: cerrar sesión no revoca el token en el servidor ([detalle](reference/auth-contracts.md#decisiones-de-fin-005-jwt-y-hash-de-contraseñas)). | Diego, 2026-11-18 (FIN-005); implementado en [#86](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/86). |
| **Hold de 15 minutos** | El asiento se reserva 15 minutos, configurables, desde la creación del hold en la API y sin reinicio. Reemplaza los cinco minutos de la base conceptual previa. Un rechazo definitivo permite reintentar dentro del tiempo restante; un pago pendiente o desconocido bloquea otro pago hasta que se resuelva. | Aprobado por Diego y Enzo el 2026-11-19. |
| **Baja lógica en el catálogo** | Bloques, rutas y salidas se dan de baja con `activo = 0`; feriados y excepciones se eliminan de verdad ([API](reference/api.md#administración-del-catálogo-fin-008)). | FIN-008: [#82](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/82), [#83](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/83), [#84](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/84). |
| **Mercado Pago Checkout Pro por la Orders API, en TEST** | La API crea la orden, devuelve el link del checkout y verifica el resultado con el proveedor antes de confirmar la compra y emitir tickets. Solo credenciales de prueba, sin cobros reales. | MVP-004: [#123](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/123), [#124](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/124). |
| **Despliegue** | Solo entorno local con datos ficticios; no se despliega en ninguna plataforma. | Restricción del proyecto. |

## 5. Cortes implementados

Cada corte se trabajó en una rama propia y se entregó en un PR con revisión cruzada entre Diego y Enzo.

| Tarea | Qué entrega | PR |
| --- | --- | --- |
| **MVP-001** — autenticación y roles | Persistencia de cuentas, ingreso y autorización por rol en la API; ingreso en Web y Mobile; comprobación de cobrador y de administrador; migraciones EF Core y reinicio local de la base. | [#37](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/37) (integra #38–#51), [#52](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/52), [#55](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/55), [#56](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/56), [#59](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/59), [#60](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/60) |
| **MVP-002** — administración operativa y demo | Esquema operativo, administración en la API y en Blazor, preparación idempotente de la demostración, diagnóstico local. | [#61](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/61)–[#68](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/68) |
| **MVP-003** — búsqueda de itinerarios | Endpoint público de búsqueda y pantallas en Blazor y React Native. | [#69](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/69), [#70](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/70), [#71](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/71) |
| **MVP-009** — ubicación de demostración | API de publicación y consulta; publicación del cobrador y consulta del pasajero en Mobile y Web. | [#72](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/72), [#73](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/73), [#74](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/74) |
| **FIN-001** — catálogo de rutas y calendarios | Catálogo de horarios en la API a partir de la transcripción del horario de invierno 2026, y página de horarios en Blazor. | [#75](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/75), [#76](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/76) |
| **FIN-006** — análisis offline-first de Mobile | Análisis del material del curso: la compra, el pago y la validación del QR siguen en línea y autoritativos en el servidor. La implementación no se inició. | [#77](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/77) ([análisis](explanation/offline-first-mobile.md)) |
| **FIN-008** — administración del catálogo | ABM de bloques, rutas y salidas con baja lógica; feriados y excepciones; página `/admin/catalogo`. | [#82](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/82), [#83](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/83), [#84](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/84) |
| **FIN-010 y FIN-011** — diseño y accesibilidad | Sistema de diseño y mockups Mobile; NativeWind en Mobile; diseño en Web; accesibilidad de validación y menú sin sesión. | [#78](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/78), [#79](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/79), [#80](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/80), [#81](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/81) |
| **FIN-012** — respuesta acotada ante filas bloqueadas | La API responde de forma acotada cuando MySQL retiene filas bloqueadas. | [#85](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/85) |

### MVP-004 — compra, pago TEST y tickets

Fusionada en `main`. En `tasks.json` la tarea sigue en `in_progress` hasta que el backlog registre su cierre con la revisión cruzada confirmada. Los PR son:

| PR | Contenido |
| --- | --- |
| [#121](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/121) | Base de compra e inventario. |
| [#122](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/122) | Reservas temporales (holds). |
| [#123](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/123) | Checkout de prueba de Mercado Pago. |
| [#124](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/124) | Pagos y cotizaciones. |
| [#125](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/125) | Confirmación de compras y generación de tickets con QR opaco. |
| [#126](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/126), [#127](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/127) | Compra en Web y en Mobile. |
| [#128](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/128), [#129](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/129) | Corrección y verificación del flujo; correcciones de la revisión cruzada. |
| [#130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130) | Correcciones del recorrido con Mercado Pago TEST y [guía para defender los conceptos](explanation/mvp-004-defensa-conceptos.md). |
| [#131](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/131) | El arranque local de la API lee las claves TEST de Mercado Pago desde la configuración privada. |

### FIN-005 — JWT sin estado

La API reemplaza las sesiones opacas por JWT sin estado y hashea las contraseñas ([#86](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/86)); Web y Mobile aceptan los tokens ([#88](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/88)); los scripts de arranque y demostración se adaptaron ([#87](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/87)); la documentación recoge la custodia de la clave y el estado verificado ([#89](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/89)); todo se integró en [#120](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/120). La tarea sigue `in_progress` en el backlog; el recorrido HTTPS real con MySQL permanece pendiente según la [referencia de la API](reference/api.md#límites-y-evidencia).

## 6. Ambientes, factibilidad y recursos

| Apartado | Estado |
| --- | --- |
| Desarrollo | Todo corre en local: API y Web con el SDK de .NET (`net10.0`), Mobile con Node y Expo ([versiones en `package.json`](../src/Mobile/package.json)). Base MySQL local descartable, con migraciones y datos de demostración reproducibles ([reinicio de la base](how-to/local-database-reset.md)). Arranque paso a paso en el [tutorial](tutorials/first-local-run.md). |
| Usuario final | Navegador para Blazor y celular con Expo para pasajero y cobrador. Cámara y permisos para el QR: **pendiente** (MVP-005). |
| Producción | **Fuera de alcance:** sin despliegue ni credenciales reales. |
| Pagos | Cuentas de prueba de Mercado Pago y credenciales TEST; ver las [pruebas de compra](how-to/purchase-test-checks.md). |
| Factibilidad económica y legal | **Pendiente** de documentar: licencias, privacidad de cuentas y ubicación, uso de marca. |

## 7. Calidad y evidencia de pruebas

### Pruebas automatizadas sin red

Las suites están en [`tests/`](../tests) y se ejecutan sin servicios externos, con dobles de prueba para el transporte y el proveedor de pagos. Protegen los invariantes de autorización, pagos, QR y concurrencia de la API y los clientes.

| Suite | Qué protege |
| --- | --- |
| `AuthenticationContracts`, `AuthenticationSessions` | Contratos de ingreso, JWT, roles y rechazos. |
| `PaymentContracts` | Estados y errores del pago, mapeo de Mercado Pago y cancelación de órdenes sin pagar. |
| `PurchaseHolds`, `PurchaseHoldSchema` | Reglas de hold, vencimiento y esquema. |
| `OperationalAdministration`, `OperationalControllerValidation`, `RouteCatalog` | Administración operativa y catálogo. |
| `WebAuthentication`, `WebPurchase` | Servicios y guardas de la Web, incluida la compra. |
| `MobileAuthentication`, `MobilePurchase`, `MobileLocation`, `Geolocation` | Servicios de Mobile y ubicación. |
| `MySqlPersistence`, `MySqlPurchase`, `MySqlInspection` | Persistencia real; requieren una base MySQL local. |
| `ApiLocalEnv`, `DemoAdministratorPreparation`, `LocalMigrationDiagnostics`, `SessionDurabilityDiagnostic` | Scripts de arranque, demostración y diagnóstico local. |

Resultados informados en el [PR #130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130): compilación de la API sin errores; `PaymentContracts` 378 comprobaciones sin fallas; `PurchaseHolds` 83/83; `WebPurchase` 213/213; `WebAuthentication` 182/182. En el [PR #131](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/131): `ApiLocalEnv` 45/45.

**Fallos conocidos:** `RouteCatalog` dejó de compilar con MVP-004 y una prueba de sesión de `MobileLocation` no pasa. Se corrigen en la rama de preentrega; este informe no los da por resueltos hasta ver el PR correspondiente.

Las suites con dobles no prueban llamadas reales al proveedor ni concurrencia real contra MySQL.

### Recorrido real con Mercado Pago TEST (2026-11-19)

Descrito en el cuerpo del [PR #130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130): Web y API locales con credenciales del vendedor de prueba, base reiniciada, itinerario Ombúes → Colonia de dos tramos.

| Caso | Resultado |
| --- | --- |
| Pago aprobado | Compra con 2 tickets (tramo 1 sin asiento, tramo 2 con asiento 1). Una segunda compra tomó el asiento 2, sin reutilizar el 1. |
| Aprobación verificada después del vencimiento del hold | Sin tickets; devolución confirmada por Mercado Pago. |
| Orden sin pagar al vencer el hold | La API canceló la orden, el pago quedó `Rechazado` y el pasajero pudo iniciar otra compra. |
| Menú del pasajero | Sin enlaces de administración. |
| Rechazo de tarjeta con hold vigente y reintento | **No ejecutado:** con sesión iniciada, Mercado Pago no pide titular para forzar el rechazo. Cubierto solo por pruebas automáticas. |
| Mobile, suite MySQL `--run` y persistencia real de `RechazarSinOrdenAsync` | **No ejecutados** en el recorrido. |

Las correcciones surgidas del recorrido (orden recién creada sin `status_detail`, fallo antes de enviar la orden, cancelación al vencer el hold) están en el [PR #130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130).

### Gestión de configuración

- Una rama por tarea del backlog y un PR por rama; la revisión cruzada se hace sobre ese PR.
- Los scripts SQL de cada migración se generan con `dotnet ef migrations script` y se versionan en `scripts/database/`; no se editan a mano.
- Las claves (cadena de conexión, clave JWT, credenciales TEST) viven en un archivo privado de configuración local que no se versiona ([guía](how-to/local-database-reset.md)).
- Los PR de documentación se validan con integración continua ([#4](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/4)).

## 8. Riesgos y pendientes

### Riesgos

| Riesgo | Estado |
| --- | --- |
| **Hora que define el vencimiento del hold.** La API compara la hora en que verifica el pago, no la de aprobación en Mercado Pago: quien paga a tiempo pero vuelve tarde recibe una devolución. | **Decisión pendiente** del equipo; figura en el [PR #130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130). |
| **JWT sin revocación.** Un token robado o un cambio de rol siguen surtiendo efecto hasta los 7 días. | Riesgo aceptado por Diego; reducir la vigencia o revocar exige otra decisión. |
| **Rechazo con hold vigente sin recorrido real.** | Cubierto solo por pruebas automáticas. |
| **Mobile sin recorrido real de compra.** | No ejecutado el 2026-11-19. |
| **Recorrido HTTPS/MySQL del JWT.** | Pendiente de verificación integrada. |
| **Avisos de `npm audit` en dependencias de Mobile.** | Sin remediar; la auditoría es histórica y hay que repetirla. |
| **Pagos TEST ≠ producción.** | No se verificó nada contra cobros reales ni se autoriza hacerlo. |

### Próximos pasos

| Tarea | Qué falta | Depende de |
| --- | --- | --- |
| MVP-004 y FIN-005 | Cerrar en el backlog con la evidencia y la revisión cruzada. | — |
| **MVP-005** | Visualización del QR y validación de ticket por el cobrador, con consumo atómico. | MVP-004 |
| **MVP-006** | Abonos con saldos independientes. | MVP-002, MVP-005 |
| **MVP-007** | Ver [`tasks.json`](tasks.json). | MVP-002, MVP-004, MVP-006 |
| **MVP-008** | Ver [`tasks.json`](tasks.json). | MVP-005, MVP-006, MVP-007 |
| **FIN-013** | Invalidar los tickets de una compra cuyo pago fue devuelto (decisión de Diego del 2026-11-19). | MVP-004, MVP-005 |
| FIN-002, FIN-003, FIN-004, FIN-007, FIN-009 | Configuraciones de vehículos, disponibilidad por intervalo, selección web de asientos y mejoras en Mobile. | Ver [`tasks.json`](tasks.json). |

No hay fecha final confirmada en este informe.

## 9. Proceso del equipo y mantenimiento

- **Equipo:** Diego y Enzo. Cada tarea tiene responsable y revisor; Diego revisa las tareas de Enzo y viceversa.
- **Backlog:** [`tasks.json`](tasks.json) es la única fuente de estados. Las tareas grandes se descomponen en hijos con `parentId`; el padre se cierra solo cuando todos sus hijos y sus criterios originales están cumplidos. Una tarea `done` no se reabre.
- **Flujo:** una rama por tarea, un PR por rama, revisión cruzada sobre el PR y pruebas automáticas obligatorias para autorización, pagos, QR, abonos y concurrencia. El resto se verifica con un recorrido manual descrito en el PR.
- **Convenciones de código:** las [skills del proyecto](../.agents/skills/ctc-project-conventions/SKILL.md) fijan las reglas de Blazor, C# y React Native del curso.
- **Este informe:** se actualiza una vez por semana, con los cambios importantes y enlaces a sus PR, sin copiar evidencia. La versión Word se genera después.
- **Para la entrega:** la matriz de trazabilidad, las alternativas tecnológicas descartadas con su justificación y la factibilidad económica y legal siguen **pendientes**.
