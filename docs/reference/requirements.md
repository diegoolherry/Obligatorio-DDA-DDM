# Obligatorio Berruti — Requerimientos del MVP académico

Este documento convierte el alcance preliminar en requisitos verificables para el MVP académico inspirado en Berrutti. Usa datos y reglas ficticios: no describe ni garantiza la operación oficial de una empresa de transporte.

## Lectura rápida

- El MVP propone compra con Mercado Pago en TEST (sin cobros reales), tickets y abonos con QR, administración básica y geolocalización básica del ómnibus.
- La API conserva la decisión de autorización y validación; los clientes solo muestran el resultado.
- Las frecuencias, tolerancias y métricas identificadas como parámetros son objetivos académicos configurables, no compromisos operativos.

## Actores y supuestos

| Actor | Objetivo en el MVP | Canal previsto |
| --- | --- | --- |
| Pasajero | Buscar, comprar con pago TEST de Mercado Pago, consultar sus tickets/abonos y su información básica de llegada. | Blazor y aplicación móvil. |
| Cobrador | Seleccionar el tramo actual y validar QR de ticket o abono. | Aplicación móvil. |
| Administrador | Configurar datos ficticios, abonos y consultar operaciones. | Blazor. |
| Dispositivo o celular embarcado | Publicar posiciones básicas desde el ómnibus para el servicio de demostración al que está asociado. | Dispositivo/celular transportado en el ómnibus. |
| API | Autorizar, aplicar reglas y persistir resultados consistentes. | Servicio interno. |

| ID | Supuesto explícito |
| --- | --- |
| SUP-01 | Las cuentas, servicios, tarifas, paradas y datos de ubicación son ficticios y configurables para la demostración. |
| SUP-02 | La validación de QR requiere conexión con la API; el modo sin conexión no integra este MVP. |
| SUP-03 | La ubicación se recibe desde un dispositivo o celular transportado en el ómnibus y asociado a su servicio de demostración; la API autoriza ese dispositivo. No se integra hardware ni telemetría oficial de Berrutti. |
| SUP-04 | El estado de demora básico se deriva de una referencia configurada para la demostración, no de una promesa de puntualidad real. |

## Alcance de geolocalización

| Capacidad | Alcance actual del MVP | Evolución futura reservada |
| --- | --- | --- |
| Publicación | Un dispositivo o celular transportado en el ómnibus publica periódicamente una posición básica para el servicio de demostración al que está asociado, con fecha/hora de recepción. La periodicidad es un parámetro académico configurable y la API autoriza el dispositivo. | Integración certificada con GPS/telemetría, calidad de señal y gestión de flotas. |
| Consulta | Mostrar la última posición conocida, su momento de actualización y una indicación de información desactualizada cuando supere un umbral configurable. | Seguimiento continuo, mapas interactivos y notificaciones proactivas. |
| Llegada | Mostrar una estimación aproximada cuando existan datos suficientes; informar indisponibilidad si no los hay. | Predicción avanzada basada en tráfico, clima, demanda o modelos históricos. |
| Demora | Comunicar un estado básico calculado con una referencia configurada: sin datos, en horario o con demora. | Diagnóstico causal, precisión operativa y compromisos de servicio. |
| Planificación | No modifica recorridos, conexiones ni asignaciones. | Optimización automática de recorridos, frecuencias y conexiones. |
| Análisis | No conserva ni explota analítica histórica como objetivo funcional. | Analítica histórica, tableros y mejora de predicciones. |

> **Decisión de alcance:** esta ampliación sustituye únicamente la exclusión previa de geolocalización en tiempo real por una versión básica de demostración. No convierte datos académicos en datos operativos reales.

## Requisitos funcionales

| ID | Prioridad | Resultado observable | Criterio de verificación |
| --- | --- | --- | --- |
| RF-001 | Alta | El usuario inicia y cierra sesión y solo accede a acciones de su rol. | Una operación protegida solicitada por un rol no autorizado es rechazada por la API. |
| RF-002 | Alta | El administrador gestiona paradas, tramos, servicios, tarifas y usuarios ficticios; configura la política de asiento del servicio. | Los datos de demostración permiten dejar disponibles los dos servicios conectados y sus atributos. |
| RF-003 | Alta | El pasajero busca por origen, destino y fecha y consulta un itinerario ordenado con tramos, transbordo, horarios, precio total y política de asiento. | Para datos compatibles se muestra el itinerario; sin coincidencias se comunica resultado vacío y se permite una nueva búsqueda. |
| RF-004 | Alta | El pasajero realiza un pago de prueba mediante la API de Mercado Pago con credenciales TEST y recibe una compra y un QR opaco por tramo solo tras aprobación verificada por la API. | En escenarios de prueba del proveedor, aprobación verificada emite tickets; rechazo o éxito informado solo por el cliente no los emite. La aplicación no solicita ni almacena números de tarjeta/CVV. |
| RF-005 | Alta | El pasajero consulta y muestra el QR de cada ticket; el Cobrador lo valida para el tramo actual y recibe un resultado claro. | Se mantiene el contrato de `MVP-005`: válido, ya utilizado, vencido, tramo incorrecto o código inexistente; solo la API consume un ticket válido de forma atómica. |
| RF-006 | Alta | El administrador asigna o renueva abonos relacionados; el pasajero consulta saldo, vigencia y QR, y el Cobrador los valida. | Se mantiene el contrato de `MVP-006`: una validación válida descuenta una unidad, solo del abono del tramo actual y de forma atómica; los estados no utilizables se rechazan sin saldo negativo. |
| RF-007 | Media | El administrador consulta pagos de prueba y validaciones; conservan actor y fecha la creación o actualización de paradas, tramos, servicios, tarifas y usuarios/roles de demostración, además de la asignación, renovación o suspensión de abonos. | La consulta permite verificar el actor y la fecha de cada una de esas operaciones administrativas delimitadas. |
| RF-008 | Media | Un dispositivo o celular transportado en el ómnibus, autorizado por la API y asociado a un servicio de demostración, registra periódicamente una posición básica. | Cada publicación aceptada conserva el dispositivo autorizado, servicio, posición y fecha/hora de recepción; la periodicidad se verifica contra el parámetro académico configurado. |
| RF-009 | Media | El pasajero consulta la última posición conocida, una llegada aproximada y un estado básico de demora del servicio. | Si hay datos vigentes se muestran los tres indicadores; si faltan o están desactualizados se informa la limitación sin fabricar precisión. |

### Límites funcionales deliberados

- La integración de Mercado Pago TEST es una decisión actual revisable y no está implementada. La política conceptual de intento durable con pagos conservados, hold y reintentos ya está acordada en [el modelo canónico](../explanation/data-model.md#acuerdo-conceptual-de-compra-hold-y-reintentos). Siguen pendientes representación física, checkout, referencia externa, mapeo/verificación de estados, concurrencia, comparación temporal y mecanismos de reintento/devolución; soporte de devolución TEST no verificado, sin reemplazo simulado ni pruebas del proveedor ejecutadas. Producción/cobros reales requieren nueva decisión de alcance.
- `RF-005` y `RF-006` remiten a los comportamientos ya aprobados en `MVP-005` y `MVP-006`; no agregan políticas nuevas de QR, consumo ni validación.
- La ubicación puede mostrarse como dato aproximado de demostración; no habilita decisiones automáticas de cobro, validación, recorrido ni asignación de asientos.
- Los clientes deben comunicar carga, vacío, error, éxito y reintento en los flujos remotos aplicables, incluida la consulta de ubicación.

## Requisitos no funcionales

Todos los valores siguientes son **objetivos académicos del MVP** o **parámetros configurables**. Deben validarse antes de convertirlos en una garantía operativa.

| ID | Área | Requisito medible | Evidencia esperada |
| --- | --- | --- | --- |
| RNF-001 | Seguridad | El 100 % de los casos de prueba definidos para operaciones protegidas debe ser rechazado por la API cuando el rol no está autorizado. | Resultado de pruebas de autorización por rol. |
| RNF-002 | Consistencia | En una prueba repetible de dos intentos concurrentes sobre el mismo ticket o unidad final de abono, como máximo uno puede ser exitoso. | Prueba de concurrencia y estado persistido resultante. |
| RNF-003 | Datos QR | El contenido legible del QR de demostración no debe incluir datos personales ni el estado completo del ticket o abono. | Inspección del valor QR y prueba del contrato. |
| RNF-004 | Experiencia | El 100 % de las pantallas remotas incluidas en el recorrido de demostración debe contemplar carga, vacío cuando aplique, error, éxito y reintento cuando la acción pueda repetirse. | Lista de pantallas y prueba manual o automatizada. |
| RNF-005 | Ubicación | El umbral de desactualización y la periodicidad de publicación deben ser parámetros visibles de la configuración académica; la interfaz marca la información vencida al superar el umbral. | Prueba con marcas de tiempo antes y después del umbral configurado. |
| RNF-006 | Estimación | La llegada y la demora deben indicar que son aproximadas y no deben mostrarse cuando faltan los datos requeridos por el cálculo configurado. | Casos con datos suficientes e insuficientes. |
| RNF-007 | Rendimiento | El tiempo máximo objetivo de respuesta de cada flujo remoto se define como parámetro académico antes de la demostración y se registra en una medición de prueba. | Parámetro declarado y registro de medición; no es SLA. |
| RNF-008 | Trazabilidad | El 100 % de los requisitos funcionales debe enlazarse con al menos una historia y un criterio de aceptación. | Matriz de trazabilidad y verificación de IDs. |
| RNF-009 | Datos de demostración | El 100 % de los datos de operación usados en la demo debe estar identificado como ficticio o configurable. | Revisión de datos semilla, pantallas y documentación. |

## Reglas de negocio relacionadas

| ID | Regla | Requisitos vinculados |
| --- | --- | --- |
| RN-01 | Un itinerario contiene uno o más tramos ordenados. | RF-003 |
| RN-02 | Una compra aprobada genera un ticket por cada tramo. | RF-004 |
| RN-03 | El QR es opaco y no contiene datos personales ni estado completo. | RF-004, RF-005, RF-006 |
| RN-04 | Un ticket solo se valida una vez y en su servicio/tramo correspondiente. | RF-005 |
| RN-07 | Los tickets se emiten solo tras verificar la aprobación del proveedor TEST desde la API, nunca por éxito informado solo por el cliente. | RF-004 |
| RN-08 | Dos abonos relacionados empiezan con igual cantidad inicial y conservan saldos independientes. | RF-006 |
| RN-09 | Una validación correcta de abono consume exactamente una unidad del abono del tramo validado. | RF-006 |
| RN-10 | Un abono vencido, suspendido o sin saldo no puede utilizarse. | RF-006 |
| RN-11 | La API decide la validez y aplica el cambio de estado atómicamente. | RF-001, RF-005, RF-006 |
| RN-12 | Cada endpoint sensible exige el rol de usuario correspondiente o, para publicación de ubicación, la identidad del dispositivo autorizado. | RF-001, RF-002, RF-007, RF-008 |
| RN-13 | En el MVP, un abono autoriza el par de paradas indicado en ambos sentidos; cada validación consume una unidad. | RF-006 |
| RN-14 | Una posición publicada debe provenir de un dispositivo o celular transportado en el ómnibus, autorizado y asociado al servicio de demostración, y conservar su fecha/hora de recepción. | RF-008, RF-009 |
| RN-15 | La estimación y el estado de demora usan solo referencias y umbrales configurados para el MVP académico. | RF-009 |

Las reglas `RN-01` a `RN-13` son las reglas mínimas ya definidas en `docs/explanation/mvp.md`. `RN-14` y `RN-15` delimitan exclusivamente la ampliación académica de geolocalización.

## Trazabilidad para revisión

| Requisito | Historias | Backlog / reglas | Calidad principal |
| --- | --- | --- | --- |
| RF-001 | US-001 | MVP-001, RN-11, RN-12 | RNF-001 |
| RF-002 | US-002 | MVP-002, RN-12 | RNF-009 |
| RF-003 | US-003 | MVP-003, RN-01 | RNF-004 |
| RF-004 | US-004 | MVP-004, RN-02, RN-03, RN-07 | RNF-003 |
| RF-005 | US-005, US-006 | MVP-005, RN-03, RN-04, RN-11 | RNF-002 |
| RF-006 | US-007, US-008 | MVP-006, RN-03, RN-08 a RN-11, RN-13 | RNF-002 |
| RF-007 | US-009 | MVP-007, RN-12 | RNF-009 |
| RF-008 | US-010 | Ampliación acordada, RN-12, RN-14 | RNF-005 |
| RF-009 | US-011, US-012 | Ampliación acordada, RN-14, RN-15 | RNF-004, RNF-005, RNF-006 |

## Fuera de alcance y próxima validación

Quedan fuera del MVP la predicción avanzada, la optimización de recorridos o conexiones, la analítica histórica, la precisión operativa, los horarios/frecuencias reales y cualquier garantía oficial. Antes de implementar geolocalización, el equipo debe acordar el dispositivo o celular embarcado de demostración, su asociación con el servicio y los parámetros académicos de publicación, vencimiento, estimación y demora.
