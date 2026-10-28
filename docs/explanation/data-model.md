# Obligatorio Berruti — Modelo conceptual del MVP y persistencia candidata

**Estado: base conceptual evolutiva aprobada por Diego y Enzo, según lo informado por el usuario; no esquema físico aprobado.** Se registra esa aprobación humana reportada, sin atribuir al agente la revisión de ninguno de ellos ni inventar una fecha de revisión. Primero se representan conceptos y reglas del dominio en UML; luego una posible persistencia MySQL en ER. La aprobación admite cambios futuros; las cardinalidades y restricciones marcadas **propuesta** no autorizan por sí solas políticas comerciales ni contratos DTO definitivos. Fuentes: [MVP](mvp.md), [requisitos](../reference/requirements.md), [historias](../reference/user-stories.md) y [backlog](../tasks.json). No se encontró consigna oficial; contrastar este borrador con ella antes de implementar.

## 1. Dominio (UML)

Un itinerario es una selección ordenada de servicios, no necesariamente una entidad durable. Un tramo representa el par de paradas de un segmento; el «orden dentro de una ruta» del inventario preliminar del MVP §8 es provisional: la pertenencia y el orden en una ruta requieren definición posterior. La instancia fechada que se valida es el servicio. El abono habilita un par de paradas en ambos sentidos (RN-13), no necesariamente una única dirección de servicio.

```mermaid
classDiagram
  class Usuario {
    identidad
    estado
    roles
  }
  class Parada {
    nombre
  }
  class Tramo {
    origen
    destino
  }
  class Servicio {
    fechaHorario
    capacidad
    requiereAsiento
  }
  class Tarifa {
    precio
    vigencia
  }
  class Itinerario {
    serviciosOrdenados
    precioTotal
  }
  class IntentoCompra {
    identidadDurable
  }
  class Compra {
    importePagado
    fecha
  }
  class Pago {
    estadoDominio
    referenciaExterna
    importe
    fecha
  }
  class Ticket {
    qrOpaco
    estado
    asientoOpcional
  }
  class AsignacionAbonos {
    periodo
    unidadesIniciales
  }
  class Abono {
    parParadas
    saldo
    vigencia
    qrOpaco
  }
  class Validacion {
    resultado
    fecha
    saldoResultanteOpcional
  }
  class Dispositivo {
    autorizado
  }
  class PosicionRecibida {
    coordenadas
    recibidaEn
  }
  class ConfiguracionUbicacion {
    periodicidad
    umbral
    referencias
  }
  class CambioAdministrativo {
    accion
    fecha
  }
  Parada "1" <-- "0..*" Tramo : origen/destino
  Tramo "1" <-- "0..*" Servicio : cubre
  Tramo "0..*" --> "0..*" Tarifa : aplicabilidad por decidir
  Itinerario "0..*" --> "1..*" Servicio : seleccion ordenada
  Usuario "1" <-- "0..*" Compra : pasajero
  Usuario "1" <-- "0..*" IntentoCompra : pasajero
  IntentoCompra "1" --> "0..*" Pago : conserva intentos
  IntentoCompra "1" --> "0..1" Compra : confirmacion idempotente
  Compra "0..*" --> "1" Itinerario : seleccion comprada ordenada
  Compra "1" --> "1..*" Ticket : aprobacion y disponibilidad
  Servicio "1" <-- "0..*" Ticket : viaje
  Usuario "1" <-- "0..*" AsignacionAbonos : pasajero
  AsignacionAbonos "1" o-- "2" Abono : demo
  Usuario "1" <-- "0..*" Validacion : cobrador
  Servicio "1" <-- "0..*" Validacion : contexto
  Ticket "0..1" <-- "0..*" Validacion : intento
  Abono "0..1" <-- "0..*" Validacion : intento
  Dispositivo "1" <-- "0..*" PosicionRecibida : publica
  Servicio "1" <-- "0..*" PosicionRecibida : recibe
  ConfiguracionUbicacion ..> Servicio : parametros demo
  Usuario "1" <-- "0..*" CambioAdministrativo : actor
```

Las asociaciones del diagrama expresan intención conceptual, no un esquema físico aprobado: un servicio puede aparecer en múltiples selecciones de itinerario; cada selección contiene uno o más servicios ordenados. Compra enlaza la selección comprada mediante el concepto existente Itinerario; el camino Compra → Ticket → Servicio identifica los servicios adquiridos, pero no expresa por sí solo su orden. La asociación con Itinerario expresa ese orden conceptual, no una tabla permanente ni una nueva entidad. Cada ticket emitido pertenece a exactamente una compra confirmada; la generación de todos los tickets ocurre junto con ella. El intento durable de compra agrupa y conserva los intentos de pago, aunque no llegue a existir compra confirmada. El acuerdo conceptual del usuario admite a lo sumo una compra confirmada por intento durable; no define su representación física ni la retención temporal de los registros. Cada validación resuelta refiere **exactamente un** ticket o abono si el QR existe; un QR desconocido no refiere ninguno. Un ticket admite muchos intentos, pero a lo sumo uno exitoso.

## 2. Persistencia candidata (ER MySQL)

Los nombres y campos siguientes son **propuesta técnica**, no migración. `itinerario` se materializaría al comprar mediante `compra_servicio` (orden y precio congelado); la búsqueda puede construir itinerarios sin guardarlos. El ER representa únicamente compras confirmadas tras aprobación verificada, disponibilidad confirmada y hold vigente: cada `compra_servicio` tiene exactamente un ticket emitido. Se omite la relación física de pagos hasta diseñar la persistencia del intento durable de compra y sus intentos de pago; el UML expresa ese vínculo conceptual. En el diagrama, las patas opcionales y las claves foráneas nulas de validaciones se explican abajo.

```mermaid
erDiagram
  usuario ||--o{ usuario_rol : posee
  rol ||--o{ usuario_rol : asignado
  parada ||--o{ tramo : origen
  parada ||--o{ tramo : destino
  tramo ||--o{ servicio : programado
  tramo ||--o{ tarifa : cotiza
  usuario ||--o{ compra : realiza
  compra ||--|{ compra_servicio : ordena
  servicio ||--o{ compra_servicio : integra
  compra_servicio ||--|| ticket : emite
  usuario ||--o{ asignacion_abonos : recibe
  asignacion_abonos ||--|{ abono : agrupa
  parada ||--o{ abono : extremo_a
  parada ||--o{ abono : extremo_b
  usuario ||--o{ validacion : cobra
  servicio ||--o{ validacion : contexto
  ticket o|--o{ validacion : intento
  abono o|--o{ validacion : intento
  dispositivo ||--o{ dispositivo_servicio : autorizado
  servicio ||--o{ dispositivo_servicio : asociado
  dispositivo_servicio ||--o{ posicion_recibida : publica
  usuario ||--o{ cambio_administrativo : registra
```

`configuracion_ubicacion` es una configuración académica versionable por vigencia, no una fuente de posiciones; no se dibuja relación obligatoria con cada servicio hasta decidir el alcance de parámetros. Una publicación aceptada siempre conserva su asociación dispositivo/servicio y recepción; intentos rechazados no crean `posicion_recibida`.

### Identificadores de las entidades

**Formato acordado por el usuario, todavía no implementado:** los IDs propios son valores reales de tipo `string`, no códigos decorativos sobre otra PK. Se usan abreviaturas de dos letras seguidas de un guion y un correlativo, como `PA-1`. Las tres posiciones iniciales son el prefijo completo (`PA-`), por lo que `Substring(3)` extrae el sufijo (`1`).

**Abreviaturas aprobadas explícitamente por el usuario en el chat, incluida `US-` para Usuario; todavía no implementadas:** esta tabla organiza el modelo existente; no aprueba nuevas tablas, migraciones ni contratos. Los ejemplos con `1` ilustran el formato y coinciden con el inicio aprobado abajo; no definen el mecanismo de generación; el rango aprobado se detalla abajo.

| Entidad o tabla candidata con identidad propia | Prefijo aprobado | Ejemplo |
| --- | --- | --- |
| Usuario | `US-` | `US-1` |
| Parada | `PA-` | `PA-1` |
| Tramo | `TR-` | `TR-1` |
| Servicio | `SE-` | `SE-1` |
| Tarifa | `TA-` | `TA-1` |
| IntentoCompra | `IC-` | `IC-1` |
| Compra | `CO-` | `CO-1` |
| Pago | `PG-` | `PG-1` |
| Ticket | `TI-` | `TI-1` |
| AsignacionAbonos | `AA-` | `AA-1` |
| Abono | `AB-` | `AB-1` |
| Validacion | `VA-` | `VA-1` |
| Dispositivo | `DI-` | `DI-1` |
| PosicionRecibida | `PR-` | `PR-1` |
| ConfiguracionUbicacion | `CU-` | `CU-1` |
| CambioAdministrativo | `CA-` | `CA-1` |
| `rol` | `RO-` | `RO-1` |
| `compra_servicio` | `CS-` | `CS-1` |
| `dispositivo_servicio` | `DS-` | `DS-1` |

**No se asigna un ID nuevo a cada concepto:** Itinerario no exige tabla propia; `usuario_rol` conserva la PK compuesta propuesta (`usuario_id`, `rol_id`), sin prefijo adicional. Los prefijos técnicos anteriores solo aplicarían si se mantienen las identidades propias del ER candidato. IntentoCompra y Pago tienen identidad conceptual, pero su representación física continúa pendiente. Usuario queda incluido en la regla con `US-` por confirmación explícita del usuario; esto no implementa autenticación ni incorpora Identity/OpenIddict. La ampliación final no recibe identidades nuevas por esta tabla.

**Referencias y límites:** una FK hacia una identidad prefijada conservaría su valor `string` completo; no se reemplaza por el sufijo numérico. Los IDs no reemplazan QR opacos, credenciales de sesión ni referencias externas de pago y no conceden autorización. `Substring(3)` no valida el prefijo ni el número: antes de convertir el sufijo deben comprobarse formato y rango. La [referencia del ejemplo docente](../../.agents/skills/ctc-dda-blazor/references/prefixed-string-ids.md) explica la extracción y el máximo numérico, distintos del orden alfabético de strings.

**Política del correlativo aprobada explícitamente por el usuario en el chat, todavía no implementada:**

- Inicio en **1**, sin ceros de relleno: `PA-1`, `PA-9`, `PA-10`.
- Contador **independiente por entidad**: pueden coexistir `PA-1` y `US-1`.
- Los IDs **no se reutilizan tras borrados**; se aceptan huecos en la numeración.
- La **API asigna el ID**; el cliente no elige el identificador de un alta.
- Rango **de 1 a 50.000 inclusive por entidad**: por ejemplo, `PA-1` hasta `PA-50000`. Es un límite del proyecto, no del tipo numérico de C#.
- El límite corresponde a números asignados históricamente, no a la cantidad de registros existentes; borrar registros no libera sus números.
- Al agotarse el rango, la API debe **rechazar el alta con un error claro**, sin reiniciar en 1 ni guardar parcialmente. El texto y contrato concreto del error quedan para el diseño del alta.
- La validación exige el prefijo correspondiente y un sufijo entero entre 1 y 50.000 inclusive, sin ceros de relleno.

**Justificación de la no reutilización, aceptada explícitamente por el usuario en el chat:** un ID identifica un registro concreto, no un lugar libre. Si `PA-3` identificaba «Terminal», se borra y luego se asigna a «Plaza», una referencia antigua en un cliente, enlace o historial podría apuntar a otra parada. Es un escenario ilustrativo, no un incidente observado. Las claves foráneas ayudan a proteger las relaciones dentro de la base de datos, pero no todas las referencias externas. Conservar la identidad histórica evita esa ambigüedad; los huecos son válidos. No es una obligación técnica universal, sino una política elegida para este proyecto. Alcanzar el límite no autoriza reciclar números: una eventual ampliación del rango requeriría otra decisión.

**Mecanismo aprobado por el usuario, todavía no implementado:** contador durable por entidad, separado de los registros, con incremento transaccional y protección concurrente; formar el ID prefijado a partir del número asignado y conservar el contador tras borrados. El ejemplo `Max + 1` sobre registros existentes por sí solo no garantiza estas propiedades.

**Pendiente antes de implementar:** definir y probar el SQL y el aislamiento concretos en MySQL, la asignación segura ante altas simultáneas, el límite de 50.000 y la no reutilización tras borrados. La aprobación del mecanismo no acredita ejecución, integración ni finalización del backlog.

### Diccionario y claves propuestas

| Tabla | Identidad y referencias propuestas | Integridad / nulabilidad propuesta |
| --- | --- | --- |
| `usuario`, `rol`, `usuario_rol` | PK `usuario_id`, `rol_id`; puente PK (`usuario_id`, `rol_id`) y dos FK | Identidad académica única; credencial segura fuera de este diseño; estado requerido. |
| `parada`, `tramo` | PK respectivas; tramo FK `origen_id`, `destino_id` | Ambos extremos NOT NULL y distintos; unicidad direccional del par por decidir con rutas. |
| `servicio` | PK `servicio_id`, FK `tramo_id` | Fecha/hora, capacidad positiva y `requiere_asiento` NOT NULL. Sin cupos inferidos de la venta hasta definir política. |
| `tarifa` | PK `tarifa_id`, FK `tramo_id` **provisional** | Importe no negativo, vigencia; aplicabilidad a tramo o servicio y solapamiento de vigencias pendientes. |
| `compra` | PK `compra_id`, FK `pasajero_id` | Fecha e importe total congelado no negativo; existencia final solo tras aprobación verificada por API, disponibilidad confirmada y hold vigente. |
| `compra_servicio` | PK `compra_servicio_id`, FK `compra_id`, `servicio_id` | `orden` positivo, UNIQUE (`compra_id`, `orden`); `precio_aplicado` no negativo congela importe por tramo. Conexión, suma y secuencia se verifican en API. |
| Intento durable de compra e intentos de pago | Tablas, PK/FK e índices **pendientes de diseño**; no se adopta la anterior FK opcional `pago.compra_id` | El intento durable agrupa y conserva los intentos de pago y puede generar a lo sumo una compra confirmada. Importe, fecha, estado de dominio y referencia externa cuando exista deben poder registrarse sin guardar credenciales ni tarjeta/CVV; mapeo del proveedor y representación física pendientes. |
| `ticket` | PK `ticket_id`, FK `compra_servicio_id`, FK pasajero derivable de compra | UNIQUE `qr_opaco`, UNIQUE `compra_servicio_id` (un ticket por tramo comprado), asiento NULL solo si el servicio no lo requiere; vencimiento/estado y asiento coherentes en API. |
| `asignacion_abonos`, `abono` | PK respectivas; asignación FK `pasajero_id`; abono FK `asignacion_id`, `extremo_a_id`, `extremo_b_id` | Vigencia, estado, unidades iniciales y saldo; `0 <= saldo <= iniciales`, extremos distintos, UNIQUE QR. Exactamente dos por asignación de demo, iguales unidades iniciales y saldos independientes: transacción API, no CHECK entre filas. |
| `validacion` | PK `validacion_id`; FK `cobrador_id`, `servicio_id`; FK `ticket_id` y `abono_id` NULL | Fecha, resultado requeridos; ambos FK NULL solo para código desconocido, nunca ambos presentes; código presentado opaco registrado con política de privacidad por definir, saldo resultante NULL salvo consumo de abono. Intentos fallidos no consumen. |
| `dispositivo`, `dispositivo_servicio`, `posicion_recibida` | PK respectivas; puente FK `dispositivo_id`, `servicio_id`; posición FK `dispositivo_servicio_id` | Autorización/asociación vigente verificada en API; latitud/longitud completas y dentro de rango, `recibida_en` NOT NULL. Conservar posiciones aceptadas sin prometer analítica; última = recepción más reciente del servicio. |
| `configuracion_ubicacion` | PK `configuracion_id` | Periodicidad y umbral positivos, referencias académicas configurables; cálculo no persistido como precisión garantizada. |
| `cambio_administrativo` | PK `cambio_id`, FK `actor_id` | Acción, objeto afectado y fecha requeridos para operaciones delimitadas en RF-007; no duplicar estados de negocio. |

**Alcance revisable de pagos:** la propuesta vigente usa la API de Mercado Pago con credenciales TEST y escenarios de prueba del proveedor, sin cobro real; la integración no está implementada. La API verifica el resultado del proveedor antes de emitir tickets, nunca confía en el éxito indicado solo por el cliente. Identificador externo y mapeo entre estado del proveedor y estado del dominio son cuestiones de diseño propuestas; producto de checkout, mecanismo de verificación y representación física quedan pendientes; las reglas conceptuales de reintento se acuerdan a continuación. Producción requiere otra decisión explícita.

### Acuerdo conceptual de compra, hold y reintentos

**Acuerdo conceptual incluido en la base evolutiva aprobada por Diego y Enzo, según lo informado por el usuario; no implementado.** Un intento durable de compra agrupa intentos de pago conservados; es distinto del hold temporal y de una compra confirmada. El hold protege un asiento de una salida para un intervalo de viaje; dura **5 minutos configurables desde su creación en la API**. Recargar o reintentar tras rechazo definitivo no reinicia el plazo.

| Situación | Regla acordada |
| --- | --- |
| Rechazo definitivo con hold vigente | Permite otro pago dentro del mismo intento durable y del tiempo restante; conserva el anterior y el vencimiento original. |
| Pago pendiente o resultado desconocido, incluida falla de red | Bloquea otro pago hasta resolución autoritativa de la API, incluso después de vencer el hold; perder la respuesta no demuestra rechazo. |
| Aprobación verificada por la API, disponibilidad confirmada **y hold vigente** | Genera una sola compra confirmada y sus tickets idempotentemente; solicitudes o notificaciones repetidas no deben duplicarlos. |
| Vencimiento del hold | Libera el asiento para ese intervalo, pero no demuestra rechazo del pago ni resuelve un resultado pendiente/desconocido. |
| Aprobación después del vencimiento | No confirma compra ni emite tickets, aunque el asiento siga disponible. Registrar y seguir compensación/devolución hasta confirmación autoritativa; solicitarla no equivale a haber devuelto el pago. |

**Mecanismos pendientes, no política indecisa:** esquema físico del intento, pagos, hold y seguimiento de devolución; estrategia transaccional/idempotente, concurrencia y liberación; checkout, verificación y mapeo del proveedor. Falta definir comparación temporal, timestamp autoritativo y carreras entre aprobación, notificación y vencimiento: una notificación recibida tarde no establece por sí sola cuándo se aprobó el pago. El soporte de devoluciones del checkout/proveedor en TEST no está verificado; sigue pendiente y **no se autoriza un reemplazo simulado**. No hay integración ni pruebas del proveedor ejecutadas. La aprobación sola no autoriza emisión sin disponibilidad y hold vigente.

**Fuera del acuerdo:** transbordos todo-o-nada, límites por usuario, corte antes de salida, cancelación/renovación y reintento después del vencimiento permanecen abiertos; no inferirlos de este hold.

### Escenarios esperados para revisión de Enzo

Checklist de diseño para implementación y revisiones futuras, **no pruebas ejecutadas**. La aprobación conceptual conjunta reportada por el usuario no acredita ejecución individual de estos escenarios; por eso permanecen sin marcar:

- [ ] Aprobación activa: API verifica aprobación, disponibilidad y hold vigente; una sola compra y sus tickets.
- [ ] Rechazo definitivo: reintenta durante el tiempo restante, conserva pagos y no reinicia el vencimiento.
- [ ] Pendiente/desconocido: también ante falla de red, bloquea otro pago hasta resolución autoritativa, aun vencido el hold.
- [ ] Hold vencido: libera el asiento/intervalo sin declarar rechazo ni resolver el pago.
- [ ] Aprobación posterior al vencimiento: no emite ni confirma aunque haya lugar; sigue devolución sin declararla completada hasta confirmación autoritativa.
- [ ] Notificación duplicada: no duplica compra, tickets ni efectos de compensación; distinguir notificación tardía de aprobación tardía sigue siendo cuestión de diseño.
- [ ] Holds superpuestos: no protegen simultáneamente el mismo asiento/salida en intervalos superpuestos; mecanismo de concurrencia pendiente.

**Restricciones candidatas:** PK/FK y UNIQUE para QR, orden e identidad impiden referencias rotas y duplicados directos; CHECK de cantidades, importes y coordenadas restringe filas individuales. Para un solo uso se puede añadir indicador de consumo único en ticket y unicidad parcial mediante estrategia compatible con MySQL, pero el índice concreto queda para implementación. Tras verificar la aprobación con el proveedor, confirmar disponibilidad y comprobar hold vigente, la API debe crear compra y emisión completa de forma atómica e idempotente. También se propone transacción para asignación doble y validación con actualización condicional de estado o saldo y registro de intento; ante dos consumos simultáneos de ticket o última unidad solo uno confirma. FKs/CHECK por sí solos no verifican ruta conectada, pago aprobado, vigencia/autorización, asiento disponible por servicio ni igualdad entre dos filas. Las comprobaciones de asiento asignado automáticamente en Radial–Colonia son parte confirmada de la demo; los mecanismos de unicidad/capacidad y del hold acordado siguen sin definir.

## 3. Límites y trazabilidad

| Contrato | Cobertura del modelo | Fuente |
| --- | --- | --- |
| Identidad y configuración | Usuario/rol, paradas, tramos, servicio, tarifa, auditoría | RF-001/002/007; US-001/002/009; RN-12 |
| Itinerario y compra | Orden, precio congelado, pago aprobado/rechazado, ticket por servicio | RF-003/004; US-003/004; RN-01/02/05/06/07; CA-01/02/03 |
| Validación | QR opaco, intento desconocido o rechazado, ticket único, abono con saldo independiente | RF-005/006; US-005 a US-008; RN-03/04/08/09/10/11/13; CA-04 a CA-09; RNF-002/003 |
| Ubicación | Posiciones recibidas aceptadas y parámetros para última posición/estimación | RF-008/009; US-010 a US-012; RN-14/15; CA-11 a CA-14 |

El **dominio** expresa reglas y comportamiento; las tablas son una alternativa de almacenamiento y no contratos públicos. Los **DTO de API**, aún no definidos, deberán separar datos de compra, consulta y validación de las entidades persistidas: nunca exponer credenciales, precio mutable como precio pagado ni el estado completo dentro del QR. Para código inexistente, el DTO responde rechazo aunque `validacion` no tenga FK de recurso; una llamada sin autorización puede rechazarse antes de registrar un intento. Posiciones inválidas nunca reemplazan la última válida.

## 4. Decisiones abiertas para revisión humana

1. ¿Tarifa por tramo o por servicio y qué vigencia aplica al precio congelado? El FK de `tarifa` es provisional.
2. ¿Cómo se persiste el intento durable de compra que agrupa intentos de pago conservados y genera a lo sumo una compra confirmada? El vínculo conceptual y las reglas de reintento están acordados en §2; tablas, claves e índices siguen abiertos. También quedan por elegir producto de checkout, mapeo de estados externos y modo de verificar el resultado sin confiar en el cliente, además de persistencia/liberación del hold, comparación temporal autoritativa y ejecución/confirmación de devoluciones según la política acordada.
3. ¿Existe ruta persistida con orden propio o alcanza el tramo entre paradas y la secuencia de servicios? No equiparar ruta y tramo sin consigna.
4. ¿Cómo se vincula cada abono bidireccional con servicios y tramos inversos al validar? RN-13 autoriza ambos sentidos, no define la representación de rutas.
5. ¿Se conservan todos los intentos de validación, incluido QR desconocido, y con qué política de retención del código presentado? La referencia exclusiva a ticket/abono y las respuestas de API deben acordarse juntas.
6. ¿Qué garantiza la unicidad de asiento por servicio y qué ocurre ante concurrencia/cupo lleno? La asignación automática de la demo ya está decidida, no su algoritmo ni el mecanismo concurrente del hold por asiento/salida/intervalo acordado en §2.

**Puertas de dominio para implementación:** la aplicabilidad y vigencia de tarifas (1) y la correspondencia bidireccional abono–servicio (4) requieren decisiones futuras explícitas antes de implementar esas funciones; no son elecciones aprobadas por omisión ni por el FK provisional. La aprobación reportada acepta esta base conceptual evolutiva con esas puertas abiertas. Las restantes preguntas físicas y operativas también deben resolverse y revisarse en su ámbito antes de derivar migraciones o contratos definitivos; no se aprueba aquí el esquema físico.

## 5. Modelo conceptual complementario de la entrega final

**Propuesta complementaria, no esquema aprobado ni implementación.** Las mejoras del equipo se delimitan en [alcance final](../reference/final-scope.md); no son exigencias académicas explícitas. El UML/ER MVP incorpora el acuerdo conceptual de §2, sin resolver las preguntas físicas y operativas anteriores. Estos conceptos permiten discutir la ampliación sin decidir silenciosamente cómo migrar `tramo`, `servicio`, tarifas, pagos o abonos existentes.

| Concepto candidato | Relación e invariante de diseño |
| --- | --- |
| Ruta dirigida / variante | Secuencia de paradas con sentido explícito; variantes no comparten órdenes numéricos por inferencia. |
| Parada en ruta | Pertenencia y orden local; identificar la ocurrencia ordenada evita confundir visitas a una misma parada. |
| Calendario de operación | Días hábiles, fines de semana, feriados, vigencia y excepciones por bloque/variante; validación de fuente pendiente. |
| Salida | Instancia fechada de una ruta dirigida, con secuencia y vehículo asignado; su correspondencia con `Servicio` MVP sigue por acordar. |
| Vehículo / configuración | Vehículo asignado determina plano académico 22/28/42/46; configuración contiene filas, posiciones y números únicos de asiento. |
| Asiento de salida | Identidad de inventario = salida + asiento de su configuración; reutilizar vehículo no comparte inventario entre salidas. |
| Ocupación confirmada | Asiento de salida e intervalo de embarque/desembarque válido de esa misma ruta/salida; no permite superposiciones. |
| Selección / intento de compra | Selección de cliente no es ocupación confirmada. El intento durable agrupa pagos conservados y genera a lo sumo una compra confirmada según §2; hold de 5 minutos configurables acordado, representación física y mecanismos pendientes. |

Relaciones propuestas: una ruta dirigida contiene una secuencia de ocurrencias de parada y puede generar múltiples salidas según calendario; una salida usa un vehículo y su configuración, que define los asientos ofrecidos. Un asiento de salida admite múltiples ocupaciones solamente en intervalos no superpuestos. Un itinerario con transbordo refiere varias salidas y requiere selección/asignación independiente en cada una.

La regla formal `[a,b)` contra `[c,d)` y sus ejemplos se mantienen en [disponibilidad por intervalo](../reference/final-scope.md#4-disponibilidad-por-intervalo), junto con [estados de compra y discusiones históricas](../reference/final-scope.md#5-compra-y-estados-de-experiencia). El acuerdo actual de §2 rige la política de hold/reintentos/aprobación tardía reflejada allí; no sustituye sus requisitos ni los estados y criterios de finalización del [backlog vigente](../tasks.json). La confirmación atómica/idempotente del servidor es un acuerdo conceptual todavía sin implementación: PK/FK o un UNIQUE por asiento/salida no bastan para permitir reutilización y excluir superposición. Estrategia transaccional, representación física e índices requieren diseño y pruebas; no se proponen migraciones aquí.

**Puertas pendientes:** acordar relación ruta/salida con el modelo MVP, configuración ante cambio de vehículo y mecanismos de hold/pago/devolución TEST. La política del hold y de aprobación posterior al vencimiento está acordada en §2; una entidad física de reserva temporal y sus estados no se dan por aprobados. No cambia la limitación TEST/sin producción ni se resuelven tarifa, checkout, implementación de reintentos, abonos o retención de validaciones. Las reglas conceptuales de §2 sí están acordadas; alcance y backlog remiten a este acuerdo sin cambiar sus requisitos, estados de tareas ni autoridad para acreditar finalización. La evidencia de horarios y sus límites está en [catálogo y fuente](../reference/final-scope.md#2-catálogo-y-evidencia-de-horarios); capacidades y planos no prueban asignaciones reales. La revisión cruzada debe preceder contratos definitivos.
