# Entidades del modelo: para qué sirven y cómo se conectan

**Respuesta corta:** el [modelo de datos](data-model.md) describe 17 conceptos de dominio para viajes, compras, abonos, validaciones y ubicación; su ER agrega cuatro tablas puente o técnicas. El usuario informa que Diego y Enzo aprobaron el modelo conceptual actual como base evolutiva, conservando cambios futuros. Este texto registra esa aprobación reportada y explica sus vínculos; **no** aprueba el esquema físico ni acredita implementación, DTO definitivos o pruebas del proveedor. Las multiplicidades del dibujo son tentativas salvo reglas expresas del [MVP](../mvp.md) y los [requisitos](../requerimientos.md). Los motivos señalados como *rationale* explican la propuesta, no crean reglas nuevas.

## Índice por recorrido

| Recorrido | Conceptos |
| --- | --- |
| Preparar la oferta | [Usuario](#usuario), [Parada](#parada), [Tramo](#tramo), [Servicio](#servicio), [Tarifa](#tarifa) |
| Comprar y viajar | [Itinerario](#itinerario), [Intento de compra](#intento-de-compra), [Compra](#compra), [Pago](#pago), [Ticket](#ticket) |
| Abonos y control | [Asignación de abonos](#asignación-de-abonos), [Abono](#abono), [Validación](#validación) |
| Ubicación y administración | [Dispositivo](#dispositivo), [Posición recibida](#posición-recibida), [Configuración de ubicación](#configuración-de-ubicación), [Cambio administrativo](#cambio-administrativo) |
| Ampliación final propuesta | [Rutas, salidas y asientos por intervalo](#ampliación-final-rutas-salidas-y-asientos-por-intervalo) |
| Solo ER | [`rol`](#rol), [`usuario_rol`](#usuario_rol), [`compra_servicio`](#compra_servicio), [`dispositivo_servicio`](#dispositivo_servicio) |

## Preparar la oferta

### Usuario
- **Propósito:** representar identidad, estado y roles de pasajero, cobrador o administrador.
- **Relaciones:** el pasajero realiza compras y recibe asignaciones de abonos; el cobrador registra validaciones; el administrador figura como actor de cambios administrativos. En el ER, `usuario_rol` conecta usuarios y roles.
- **Por qué:** *rationale:* separa quién compra, quién controla y quién modifica la configuración. **Regla:** la API autoriza las operaciones sensibles por rol (RN-12); una pantalla oculta no sustituye esa autorización.

### Parada
- **Propósito:** identificar los extremos de los desplazamientos.
- **Relaciones:** es origen o destino de un tramo y extremo A o B del par habilitado por un abono.
- **Por qué:** *rationale:* permite conectar servicios y comprobar el par autorizado sin confundir parada con viaje fechado. Ejemplo ficticio: Radial de Conchillas conecta Ombúes–Radial con Radial–Colonia.

### Tramo
- **Propósito:** representar el par origen/destino de un segmento, sin asumir una ruta persistida.
- **Relaciones:** une dos paradas; lo cubren servicios; la tarifa se asocia provisionalmente al tramo. El abono utiliza un par de paradas, pero todavía no está resuelto cómo vincular los sentidos inversos con tramos y servicios.
- **Por qué:** *rationale:* distingue el segmento de su salida concreta. El orden dentro de una ruta sigue abierto; no hay que inventarlo a partir del orden del itinerario.

### Servicio
- **Propósito:** describir una salida fechada con horario, tramo, capacidad y política de asiento.
- **Relaciones:** cubre un tramo; integra itinerarios y compras; cada ticket refiere un servicio; las validaciones ocurren en su contexto. Recibe posiciones publicadas por dispositivos asociados; la configuración de ubicación aporta parámetros académicos, sin relación obligatoria por servicio definida.
- **Por qué:** *rationale:* permite distinguir dos salidas del mismo tramo y controlar el viaje concreto. **Regla de demo:** Ombúes–Radial no asigna asiento y Radial–Colonia lo asigna automáticamente (RN-06); la política de cupos y unicidad de asiento sigue abierta.

### Tarifa
- **Propósito:** representar precio y vigencia para cotizar el viaje.
- **Relaciones:** el ER propone una FK al tramo, pero su aplicabilidad a tramo o servicio y el solapamiento de vigencias están pendientes; `compra_servicio` congelaría el precio aplicado al comprar.
- **Por qué:** *rationale:* separa el precio ofertado, potencialmente cambiante, del importe histórico pagado. **Puerta futura de dominio:** decidir aplicabilidad y vigencia antes de implementar tarifas; no inferir una política comercial aprobada del FK provisional ni de la aprobación conceptual.

## Comprar y viajar

### Itinerario
- **Propósito:** mostrar una selección ordenada de uno o más servicios que conecta origen y destino con un precio total.
- **Relaciones:** selecciona servicios ordenados y representa la selección adquirida vinculada a Compra en UML. Compra → Ticket → Servicio identifica los servicios comprados, mientras Itinerario expresa su orden conceptual. El ER propone conservar la secuencia en `compra_servicio`; no exige tabla permanente de itinerario ni agrega otra entidad.
- **Por qué:** **Regla:** RN-01 exige orden. *Rationale:* Ombúes–Radial seguido por Radial–Colonia permite mostrar transbordo y total antes de pagar; persistir una ruta independiente sigue sin decidirse.

### Intento de compra
- **Propósito:** mantener una identidad durable para la operación que el pasajero intenta confirmar, aunque no llegue a existir una compra.
- **Relaciones:** pertenece al pasajero, agrupa y conserva cero o más intentos de pago y genera **a lo sumo una compra confirmada**. Es distinto del hold temporal y de la compra final.
- **Por qué:** **Acuerdo conceptual:** permite conservar rechazos y resolver resultados pendientes sin duplicar pagos ni compras. El UML lo denomina `IntentoCompra`; sus tablas, claves, índices y retención temporal siguen pendientes de diseño.

### Compra
- **Propósito:** representar la operación confirmada del pasajero con fecha e importe total congelado.
- **Relaciones:** pertenece a un usuario pasajero y resulta de un intento durable de compra; agrupa servicios comprados mediante `compra_servicio`, y cada uno origina un ticket.
- **Por qué:** **Reglas:** la API verifica la aprobación del proveedor TEST (RN-02, RN-07); el acuerdo vigente exige además disponibilidad confirmada y hold vigente. Compra y tickets se generan de forma atómica e idempotente: repetir solicitudes o notificaciones no los duplica. El ER representa compras confirmadas, no intentos rechazados; el mecanismo técnico todavía no está implementado.

### Pago
- **Propósito:** reflejar importe, fecha, estado del dominio y referencia externa cuando exista para un intento de pago de prueba.
- **Relaciones:** pertenece a un intento durable de compra, que conserva sus pagos aunque no genere compra confirmada. **No se adopta la anterior FK opcional `pago.compra_id`:** la representación física del vínculo está pendiente.
- **Por qué:** **Regla:** el cliente no declara unilateralmente la aprobación; la API verifica el resultado del proveedor. Un rechazo definitivo permite reintentar con hold vigente; un resultado pendiente o desconocido bloquea otro pago hasta resolución autoritativa. Mercado Pago TEST sigue siendo una propuesta revisable, sin integración ni pruebas del proveedor ejecutadas; checkout, verificación y mapeo de estados están pendientes. No se almacenan tarjetas ni CVV.

### Hold y resolución de pagos

**Acuerdo incluido en la base conceptual evolutiva aprobada por Diego y Enzo, según lo informado por el usuario; no implementación.** El hold protege un asiento de una salida para un intervalo de viaje; no se presenta como nueva tabla ni entidad física aprobada.

| Situación | Regla acordada |
| --- | --- |
| Creación del hold | Dura **5 minutos configurables desde su creación en la API**; recargar o reintentar no reinicia el plazo. |
| Rechazo definitivo con hold vigente | Admite otro pago en el mismo intento durable durante el tiempo restante, conservando el anterior y el vencimiento original. |
| Pago pendiente o desconocido, incluida falla de red | Bloquea otro pago hasta resolución autoritativa de la API, incluso con hold vencido; perder la respuesta no demuestra rechazo. |
| Aprobación verificada, disponibilidad confirmada y hold vigente | Genera una sola compra y todos sus tickets de forma atómica e idempotente. |
| Vencimiento del hold | Libera el asiento para ese intervalo; no demuestra rechazo ni resuelve el pago pendiente. |
| Aprobación después del vencimiento | No confirma compra ni emite tickets, aunque haya lugar; registra y sigue compensación/devolución hasta confirmación autoritativa. Solicitar devolución no equivale a haberla completado. |

**Pendiente técnico:** persistencia, concurrencia, liberación, idempotencia y comparación temporal autoritativa. Una notificación tardía no prueba una aprobación tardía. El soporte de devoluciones del proveedor/checkout en TEST no está verificado y no se autoriza sustituirlo por una simulación. Transbordos todo-o-nada, límites por usuario, corte antes de salida, cancelación/renovación y reintento después del vencimiento permanecen abiertos. El detalle autoritativo está en [el acuerdo del modelo](data-model.md#acuerdo-conceptual-de-compra-hold-y-reintentos).

### Ticket
- **Propósito:** acreditar un tramo comprado mediante QR opaco, estado y asiento opcional.
- **Relaciones:** el ER propone un ticket por `compra_servicio`; este identifica su servicio y la compra permite conocer al pasajero. Las validaciones pueden referir ese ticket.
- **Por qué:** **Reglas:** un ticket por tramo tras aprobación verificada, disponibilidad confirmada y hold vigente, QR sin datos personales ni estado completo, uso exitoso único para servicio/tramo correcto y asiento obligatorio solo cuando el servicio lo requiera (RN-02 a RN-05). *Rationale:* dos tramos comprados juntos necesitan dos controles independientes.

## Abonos y control

### Asignación de abonos
- **Propósito:** agrupar la entrega o renovación de abonos de un pasajero para un período.
- **Relaciones:** pertenece al usuario pasajero y agrupa abonos; en la demo son dos para los dos pares de paradas del recorrido.
- **Por qué:** **Regla de demo:** se asignan dos con iguales unidades iniciales y saldos independientes (RN-08). *Rationale:* agruparlos deja trazable su entrega conjunta sin fundir sus consumos; el «2» del UML no debe generalizarse a cualquier itinerario.

### Abono
- **Propósito:** guardar vigencia, estado, QR opaco, unidades iniciales y saldo propio para un par de paradas.
- **Relaciones:** pertenece a una asignación de un pasajero; sus extremos son paradas; cada intento de validación puede referirlo y el servicio aporta contexto.
- **Por qué:** **Reglas:** el par autoriza ambos sentidos (RN-13), una validación exitosa descuenta una unidad solo de ese abono (RN-09), y vencido, suspendido o sin saldo no se usa (RN-10). *Rationale:* el segundo abono del ejemplo conserva saldo independiente aunque se hayan entregado juntos. El enlace de ida/vuelta con servicios concretos es una decisión futura de dominio que debe resolverse antes de implementar esa correspondencia; la aprobación conceptual no elige cómo hacerlo.

### Validación
- **Propósito:** representar un intento de control, su resultado, fecha y saldo resultante cuando consume abono.
- **Relaciones:** identifica al cobrador (usuario) y al servicio; si el QR existe, refiere exactamente un ticket **o** un abono, nunca ambos. Para un QR desconocido puede no referir ninguno.
- **Por qué:** **Regla:** solo la API decide y aplica atómicamente el consumo; un ticket admite como máximo un éxito y un fallo no descuenta saldo (RN-04, RN-11). *Rationale:* separar intentos de recursos permite explicar rechazos y duplicados. **Pendiente:** conservación de todos los intentos desconocidos y privacidad del código presentado; una solicitud no autorizada puede rechazarse antes de registrarse.

## Ubicación y administración

### Dispositivo
- **Propósito:** identificar el celular o dispositivo embarcado autorizado para publicar ubicaciones.
- **Relaciones:** `dispositivo_servicio` lo asocia al servicio; sus publicaciones aceptadas originan posiciones recibidas mediante esa asociación.
- **Por qué:** **Regla:** la API comprueba identidad autorizada y asociación al servicio (RN-12, RN-14). *Rationale:* saber quién publicó evita atribuir una posición a cualquier emisor.

### Posición recibida
- **Propósito:** conservar coordenadas y hora de recepción de una publicación aceptada.
- **Relaciones:** en el UML vincula dispositivo y servicio; en el ER referencia su asociación `dispositivo_servicio`.
- **Por qué:** **Regla:** la consulta toma la recepción más reciente válida del servicio; publicaciones inválidas no reemplazan la última válida (RN-14, RF-008). *Rationale:* la marca de tiempo permite indicar si el dato está desactualizado; no implica seguimiento oficial ni analítica histórica.

### Configuración de ubicación
- **Propósito:** definir periodicidad, umbral de desactualización y referencias académicas para llegada y demora aproximadas.
- **Relaciones:** el UML muestra dependencia de servicio para los parámetros de demo; el ER aún no fija una relación obligatoria por servicio.
- **Por qué:** **Regla:** sin posición vigente o referencias suficientes no se fabrica estimación (RN-15). *Rationale:* parámetros explícitos hacen explicable la demo, no garantizan precisión operativa.

### Cambio administrativo
- **Propósito:** registrar acción, objeto afectado, fecha y actor de operaciones administrativas delimitadas.
- **Relaciones:** referencia al usuario administrador que efectuó el cambio; puede describir altas o modificaciones de paradas, tramos, servicios, tarifas, usuarios/roles y asignación, renovación o suspensión de abonos.
- **Por qué:** **Requisito:** RF-007 pide identificar actor y fecha de esas operaciones. *Rationale:* facilita revisar quién cambió la configuración sin duplicar estados de negocio; no representa cualquier evento del sistema.

## Tablas técnicas del ER, no nuevos conceptos UML

### `rol`
- **Propósito:** catálogo técnico de roles de demostración.
- **Relaciones:** `usuario_rol` vincula cada rol con usuarios.
- **Por qué:** *rationale:* representa las funciones de pasajero, cobrador y administrador usadas al autorizar acciones (RN-12); no sustituye la comprobación de permisos en la API.

### `usuario_rol`
- **Propósito:** puente propuesto entre `usuario` y `rol`.
- **Relaciones:** referencia ambas tablas con par (`usuario_id`, `rol_id`) como clave propuesta.
- **Por qué:** *rationale:* materializa los roles mostrados como atributo de Usuario en UML sin repetir la identidad por rol.

### `compra_servicio`
- **Propósito:** materializar la selección comprada, su orden y precio aplicado congelado.
- **Relaciones:** une `compra` y `servicio`; emite exactamente un `ticket` por fila de compra aprobada en el ER propuesto.
- **Por qué:** *rationale:* conserva la secuencia e importe de cada tramo al cambiar tarifas; permite explicar los dos tickets del ejemplo. La API debe verificar conexión, suma y secuencia; la tabla por sí sola no las garantiza.

### `dispositivo_servicio`
- **Propósito:** materializar la asociación autorizada entre emisor y salida.
- **Relaciones:** une `dispositivo` y `servicio`; `posicion_recibida` apunta a esta asociación.
- **Por qué:** *rationale:* cada posición aceptada puede atribuirse al par correcto. La vigencia y autorización se comprueban en API; una publicación rechazada no crea posición.

## Ampliación final: rutas, salidas y asientos por intervalo

El [modelo complementario de la entrega final](data-model.md#5-modelo-conceptual-complementario-de-la-entrega-final) propone los siguientes conceptos fuera de los 17 del UML MVP. Son mejoras del equipo, no exigencias académicas explícitas ni un esquema aprobado; su correspondencia con `Tramo` y `Servicio` sigue por acordar.

| Concepto candidato | Propósito y vínculo |
| --- | --- |
| Ruta dirigida / variante | Ordena ocurrencias de parada con sentido explícito; no comparte órdenes numéricos con otras variantes por inferencia. |
| Parada en ruta | Identifica una ocurrencia ordenada dentro de una ruta, incluso si se visita más de una vez la misma parada. |
| Calendario de operación | Define días, vigencia y excepciones por bloque/variante; la validación de su fuente sigue pendiente. |
| Salida | Instancia fechada de una ruta, con secuencia y vehículo asignado; no equivale automáticamente al `Servicio` MVP. |
| Vehículo / configuración | Define el plano académico y números únicos de asiento; los planos no acreditan asignaciones reales. |
| Asiento de salida | Inventario identificado por salida y asiento de su configuración; reutilizar vehículo no comparte inventario entre salidas. |
| Ocupación confirmada | Asocia asiento e intervalo de embarque/desembarque de la misma ruta/salida; admite reutilización solo sin superposición. |
| Selección / intento de compra | La selección del cliente no es ocupación confirmada; el intento durable conserva pagos y confirma como máximo una compra según el acuerdo vigente. |

El hold temporal protege ese asiento/intervalo sin convertirse en ocupación confirmada. La regla de superposición de intervalos `[a,b)` y sus ejemplos están en [disponibilidad por intervalo](../final-scope.md#4-disponibilidad-por-intervalo). Una restricción UNIQUE por asiento/salida no basta para permitir reutilización y evitar superposición; los mecanismos transaccionales e índices requieren diseño y pruebas. No se definen migraciones aquí.

## Antes de convertir el modelo en esquema

La [lista de decisiones abiertas del modelo](data-model.md#4-decisiones-abiertas-para-revisión-humana) incluye tarifa/vigencia, persistencia del intento durable y sus pagos, checkout y verificación, mecanismos de hold/devolución, ruta durable, sentido inverso de abonos, auditoría de QR desconocidos y concurrencia/cupos/asientos. **Las reglas conceptuales de hold, reintentos y aprobación tardía ya están acordadas; sus mecanismos técnicos no.** La aprobación conjunta reportada por el usuario incluye la revisión de Enzo de la base conceptual, no una aprobación física. Este texto no acredita implementación ni sustituye el diseño y revisión específicos de migraciones o contratos API todavía por definir. El [backlog](../tasks.json) conserva la autoridad sobre estados y finalización de tareas.
