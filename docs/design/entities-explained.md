# Entidades del borrador: para qué sirven y cómo se conectan

**Respuesta corta:** el [modelo de datos](data-model.md) propone 16 conceptos de dominio para describir viajes, compras, abonos, validaciones y ubicación; su ER agrega cuatro tablas puente o técnicas. Este texto explica su propósito y sus vínculos, **no** aprueba el esquema ni acredita implementación. Las multiplicidades del dibujo son tentativas salvo reglas expresas del [MVP](../mvp.md) y los [requisitos](../requerimientos.md). Los motivos señalados como *rationale* explican la propuesta, no crean reglas nuevas.

## Índice por recorrido

| Recorrido | Conceptos |
| --- | --- |
| Preparar la oferta | [Usuario](#usuario), [Parada](#parada), [Tramo](#tramo), [Servicio](#servicio), [Tarifa](#tarifa) |
| Comprar y viajar | [Itinerario](#itinerario), [Compra](#compra), [Pago](#pago), [Ticket](#ticket) |
| Abonos y control | [Asignación de abonos](#asignación-de-abonos), [Abono](#abono), [Validación](#validación) |
| Ubicación y administración | [Dispositivo](#dispositivo), [Posición recibida](#posición-recibida), [Configuración de ubicación](#configuración-de-ubicación), [Cambio administrativo](#cambio-administrativo) |
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
- **Por qué:** *rationale:* separa el precio ofertado, potencialmente cambiante, del importe histórico pagado. **Pendiente:** decidir cuál vigencia rige el cálculo; no inferir una política comercial del FK provisional.

## Comprar y viajar

### Itinerario
- **Propósito:** mostrar una selección ordenada de uno o más servicios que conecta origen y destino con un precio total.
- **Relaciones:** selecciona servicios; al comprar, el ER propone conservar la secuencia en `compra_servicio` dentro de una compra. No exige tabla durable de itinerario para buscar.
- **Por qué:** **Regla:** RN-01 exige orden. *Rationale:* Ombúes–Radial seguido por Radial–Colonia permite mostrar transbordo y total antes de pagar; persistir una ruta independiente sigue sin decidirse.

### Compra
- **Propósito:** representar la operación aprobada del pasajero con fecha e importe total congelado.
- **Relaciones:** pertenece a un usuario pasajero y agrupa servicios comprados mediante `compra_servicio`; cada uno origina un ticket. Un pago puede vincularse opcionalmente a ella.
- **Por qué:** **Regla:** la API verifica aprobación del proveedor TEST antes de emitir todos los tickets (RN-02, RN-07). *Rationale:* el borrador ER conserva solo compras aprobadas; un rechazo puede quedar en pago sin compra. La vinculación entre reintentos y compra eventual está abierta.

### Pago
- **Propósito:** reflejar importe, fecha, estado del dominio y referencia externa cuando exista para un intento de pago de prueba.
- **Relaciones:** corresponde al pasajero; su FK a compra es opcional y provisional, porque un intento rechazado no emite compra. La compra aprobada habilita la emisión de tickets, pero todavía falta diseñar la referencia de intento y los reintentos.
- **Por qué:** **Regla:** el cliente no declara unilateralmente la aprobación; la API verifica el resultado del proveedor. La integración Mercado Pago TEST es propuesta revisable, no implementada; producto de checkout, verificación y mapeo de estados siguen abiertos. No se almacenan tarjetas ni CVV.

### Ticket
- **Propósito:** acreditar un tramo comprado mediante QR opaco, estado y asiento opcional.
- **Relaciones:** el ER propone un ticket por `compra_servicio`; este identifica su servicio y la compra permite conocer al pasajero. Las validaciones pueden referir ese ticket.
- **Por qué:** **Reglas:** un ticket por tramo tras aprobación, QR sin datos personales ni estado completo, uso exitoso único para servicio/tramo correcto y asiento obligatorio solo cuando el servicio lo requiera (RN-02 a RN-05). *Rationale:* dos tramos comprados juntos necesitan dos controles independientes.

## Abonos y control

### Asignación de abonos
- **Propósito:** agrupar la entrega o renovación de abonos de un pasajero para un período.
- **Relaciones:** pertenece al usuario pasajero y agrupa abonos; en la demo son dos para los dos pares de paradas del recorrido.
- **Por qué:** **Regla de demo:** se asignan dos con iguales unidades iniciales y saldos independientes (RN-08). *Rationale:* agruparlos deja trazable su entrega conjunta sin fundir sus consumos; el «2» del UML no debe generalizarse a cualquier itinerario.

### Abono
- **Propósito:** guardar vigencia, estado, QR opaco, unidades iniciales y saldo propio para un par de paradas.
- **Relaciones:** pertenece a una asignación de un pasajero; sus extremos son paradas; cada intento de validación puede referirlo y el servicio aporta contexto.
- **Por qué:** **Reglas:** el par autoriza ambos sentidos (RN-13), una validación exitosa descuenta una unidad solo de ese abono (RN-09), y vencido, suspendido o sin saldo no se usa (RN-10). *Rationale:* el segundo abono del ejemplo conserva saldo independiente aunque se hayan entregado juntos. El enlace de ida/vuelta con servicios concretos queda por resolver.

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

## Antes de convertir el borrador en esquema

La [lista de decisiones abiertas del modelo](data-model.md#4-decisiones-abiertas-para-revisión-humana) incluye tarifa/vigencia, identidad y reintentos de pago, ruta durable, sentido inverso de abonos, auditoría de QR desconocidos y concurrencia/cupos/asientos. Nada de lo explicado aquí cierra esas decisiones ni reemplaza revisión cruzada, migraciones o contratos API todavía por definir.
