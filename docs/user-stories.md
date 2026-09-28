# Historias de usuario del MVP académico

Estas historias permiten revisar cada requisito funcional sin reinterpretar el alcance. Todos los datos, parámetros de ubicación y resultados operativos son de demostración; no representan reglas ni niveles de servicio oficiales.

## Mapa de cobertura

| Requisito funcional | Historias | Prioridad máxima |
| --- | --- | --- |
| RF-001 | US-001 | Alta |
| RF-002 | US-002 | Alta |
| RF-003 | US-003 | Alta |
| RF-004 | US-004 | Alta |
| RF-005 | US-005, US-006 | Alta |
| RF-006 | US-007, US-008 | Alta |
| RF-007 | US-009 | Media |
| RF-008 | US-010 | Media |
| RF-009 | US-011, US-012 | Media |

## Acceso y configuración

### US-001 — Acceder según mi rol

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-001 |
| Prioridad | Alta |
| Historia | Como usuario de demostración, quiero iniciar y cerrar sesión con mi rol para acceder solamente a las acciones autorizadas. |

**Criterios de aceptación**

- **Dado** un pasajero con una cuenta ficticia válida, **cuando** inicia sesión, **entonces** accede a las funciones de pasajero y puede cerrar sesión.
- **Dado** un usuario autenticado sin rol de administrador, **cuando** intenta invocar una operación administrativa protegida, **entonces** la API la rechaza aunque la interfaz o la URL sea accesible.
- **Dado** credenciales inválidas, **cuando** intenta iniciar sesión, **entonces** recibe un error comprensible y no obtiene una sesión autorizada.

### US-002 — Configurar la demostración operativa

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-002 |
| Prioridad | Alta |
| Historia | Como Administrador, quiero gestionar datos ficticios de paradas, tramos, servicios, tarifas y usuarios para preparar un recorrido demostrable. |

**Criterios de aceptación**

- **Dado** que inicié sesión como Administrador, **cuando** configuro los dos servicios conectados de demostración y su política de asiento, **entonces** quedan disponibles para la búsqueda del pasajero.
- **Dado** un servicio que requiere asiento, **cuando** guardo su configuración, **entonces** la política queda registrada para que el itinerario la muestre.
- **Dado** que falta un dato obligatorio de la configuración, **cuando** intento guardarla, **entonces** el sistema informa el error y no deja una configuración incompleta.

## Búsqueda y compra

### US-003 — Encontrar un itinerario

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-003 |
| Prioridad | Alta |
| Historia | Como Pasajero, quiero buscar un viaje por origen, destino y fecha para conocer las opciones de itinerario antes de comprar. |

**Criterios de aceptación**

- **Dado** que existen servicios compatibles para los datos de demostración, **cuando** busco un viaje, **entonces** veo los tramos ordenados, el transbordo, horarios, precio total y política de asiento.
- **Dado** que no existen servicios compatibles, **cuando** ejecuto la búsqueda, **entonces** veo un resultado vacío comprensible y puedo modificar los criterios para reintentar.
- **Dado** un fallo al consultar la API, **cuando** la búsqueda no puede completarse, **entonces** veo un estado de error y una opción de reintento.

### US-004 — Comprar con pago de prueba

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-004 |
| Prioridad | Alta |
| Historia | Como Pasajero, quiero pagar en un escenario TEST de Mercado Pago para recibir los tickets QR de mis tramos solo tras aprobación verificada por la API. |

**Criterios de aceptación**

- **Dado** un itinerario válido y aprobación en un escenario TEST del proveedor, **cuando** la API verifica ese resultado, **entonces** registra la compra y entrega un ticket con QR opaco por tramo.
- **Dado** un rechazo en un escenario TEST, **cuando** finaliza el intento, **entonces** no se emiten tickets y puedo reintentar sin que la aplicación solicite ni almacene números de tarjeta/CVV.
- **Dado** un éxito declarado solo por el cliente, resultado no verificado o error de verificación, **cuando** la API procesa el intento, **entonces** no presenta tickets como emitidos y la interfaz informa el estado sin inventar aprobación.

## Tickets y abonos

### US-005 — Mostrar un ticket QR

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-005 |
| Prioridad | Alta |
| Historia | Como Pasajero, quiero consultar mis tickets y mostrar el QR de cada tramo para presentarlo al Cobrador. |

**Criterios de aceptación**

- **Dado** que tengo tickets vigentes o utilizados, **cuando** consulto mis tickets, **entonces** puedo distinguir su estado y mostrar el QR opaco del tramo elegido.
- **Dado** que no tengo tickets para mostrar, **cuando** abro la consulta, **entonces** veo un estado vacío comprensible.
- **Dado** que la consulta remota falla, **cuando** intento cargar mis tickets, **entonces** veo un error y puedo reintentar.

### US-006 — Validar un ticket en el tramo actual

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-005 |
| Prioridad | Alta |
| Historia | Como Cobrador, quiero escanear el QR de un ticket para saber si puede utilizarse en el tramo actual. |

**Criterios de aceptación**

- **Dado** un ticket vigente que corresponde al tramo actual, **cuando** escaneo su QR por primera vez, **entonces** la API confirma la validación y lo marca como utilizado de forma atómica.
- **Dado** un ticket ya utilizado, vencido, de otro tramo o un código inexistente, **cuando** escaneo el QR, **entonces** recibo el resultado claro definido por `MVP-005` y el ticket no se consume nuevamente.
- **Dado** que no hay conexión con la API, **cuando** intento validar, **entonces** se informa el fallo y no se confirma una validación local.

### US-007 — Consultar mis abonos

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-006 |
| Prioridad | Alta |
| Historia | Como Pasajero, quiero consultar la vigencia, el saldo y el QR de cada abono para usar el correspondiente a mi tramo. |

**Criterios de aceptación**

- **Dado** que tengo dos abonos relacionados asignados, **cuando** los consulto, **entonces** veo su vigencia, saldos independientes y el QR de cada uno.
- **Dado** que no tengo abonos asignados, **cuando** abro la consulta, **entonces** veo un estado vacío comprensible.
- **Dado** que la consulta falla, **cuando** intento cargar los abonos, **entonces** veo un error y puedo reintentar.

### US-008 — Validar un abono en el tramo correspondiente

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-006 |
| Prioridad | Alta |
| Historia | Como Cobrador, quiero validar el QR de un abono para autorizar el tramo correcto y actualizar solo su saldo. |

**Criterios de aceptación**

- **Dado** un abono activo con saldo y correspondiente al tramo actual, **cuando** escaneo su QR, **entonces** la API descuenta exactamente una unidad de ese abono de forma atómica.
- **Dado** un abono sin saldo, vencido, suspendido o de otro tramo, **cuando** intento validarlo, **entonces** recibo un rechazo claro y no se genera saldo negativo ni se modifica el otro abono relacionado.
- **Dado** dos intentos simultáneos sobre la última unidad disponible, **cuando** la API procesa las validaciones, **entonces** como máximo una resulta exitosa conforme al contrato de `MVP-006`.

## Operación y trazabilidad

### US-009 — Consultar operaciones administrativas

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-007 |
| Prioridad | Media |
| Historia | Como Administrador, quiero consultar pagos de prueba y validaciones para revisar las operaciones de demostración y su auditoría básica. |

**Criterios de aceptación**

- **Dado** que se crearon o actualizaron paradas, tramos, servicios, tarifas o usuarios/roles de demostración, o se asignaron, renovaron o suspendieron abonos, **cuando** consulto la auditoría, **entonces** cada una de esas operaciones muestra el actor y la fecha registrados.
- **Dado** que no hay operaciones para el criterio consultado, **cuando** realizo la consulta, **entonces** veo un resultado vacío comprensible.
- **Dado** que no estoy autorizado, **cuando** intento consultar operaciones administrativas, **entonces** la API rechaza el acceso.

## Geolocalización básica

### US-010 — Publicar una posición de demostración

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-008 |
| Prioridad | Media |
| Historia | Como dispositivo o celular transportado en el ómnibus, quiero publicar posiciones básicas para el servicio de demostración al que estoy asociado para que los pasajeros puedan consultar su referencia más reciente. |

**Criterios de aceptación**

- **Dado** un dispositivo o celular transportado en el ómnibus, autorizado por la API y asociado a un servicio de demostración existente, **cuando** publica una posición válida, **entonces** se conserva la posición junto con el dispositivo, el servicio y la fecha/hora de recepción.
- **Dado** la periodicidad académica configurada, **cuando** el dispositivo o celular embarcado publica posiciones durante la demostración, **entonces** se puede verificar su cumplimiento contra ese parámetro sin declararlo una frecuencia real.
- **Dado** un dispositivo no autorizado, un servicio inexistente o datos de posición incompletos, **cuando** se intenta publicar, **entonces** la API rechaza la operación y no reemplaza la última posición conocida.

### US-011 — Consultar la última posición conocida

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-009 |
| Prioridad | Media |
| Historia | Como Pasajero, quiero ver la última posición conocida de mi servicio para contar con una referencia básica de su recorrido. |

**Criterios de aceptación**

- **Dado** un servicio con una posición recibida dentro del umbral académico configurado, **cuando** consulto su información, **entonces** veo la última posición y su momento de actualización.
- **Dado** una posición anterior al umbral configurado, **cuando** la consulto, **entonces** la interfaz la identifica como desactualizada sin presentarla como ubicación en tiempo real garantizada.
- **Dado** que no hay posiciones publicadas o falla la consulta, **cuando** solicito la información, **entonces** veo respectivamente un estado sin datos o de error con posibilidad de reintento.

### US-012 — Conocer una llegada aproximada y demora básica

| Campo | Definición |
| --- | --- |
| Requisito vinculado | RF-009 |
| Prioridad | Media |
| Historia | Como Pasajero, quiero conocer una estimación aproximada de llegada y un estado básico de demora para decidir con mejor información durante la demostración. |

**Criterios de aceptación**

- **Dado** datos suficientes según el cálculo académico configurado, **cuando** consulto el servicio, **entonces** veo una llegada aproximada y un estado básico: en horario o con demora.
- **Dado** datos insuficientes, desactualizados o sin una referencia configurada, **cuando** consulto la estimación, **entonces** se informa que no está disponible y no se inventa una hora ni una precisión.
- **Dado** que el cálculo remoto falla, **cuando** solicito el estado, **entonces** veo un mensaje de error y puedo reintentar la consulta.

## Límites de estas historias

Las historias de geolocalización cubren únicamente publicación periódica básica, última posición, llegada aproximada y estado básico de demora. La predicción avanzada, optimización de recorridos o conexiones, analítica histórica, precisión operativa y garantías de servicio quedan expresamente para trabajo futuro.
