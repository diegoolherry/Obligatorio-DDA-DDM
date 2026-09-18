# MVP académico — Venta y validación de pasajes y abonos

> **Estado:** definición preliminar previa a la consigna oficial.
>
> **Propósito:** proyecto académico inspirado en la operativa observada de Berrutti. No representa un sistema oficial, aprobado ni encargado por la empresa.

## 1. Resultado esperado

El MVP permitirá que una persona compre digitalmente un viaje compuesto por uno o más tramos, reciba un ticket QR por cada tramo y pueda mostrarlo al cobrador. También permitirá administrar abonos por tramo y descontar una unidad cada vez que el cobrador valide su QR.

La solución integrará:

- **Blazor:** portal de pasajeros y panel administrativo.
- **API:** reglas de negocio, autenticación, autorización y persistencia.
- **React Native con TypeScript y Expo:** una aplicación móvil con experiencia diferenciada para pasajero y cobrador.
- **Pago digital simulado:** sin cobros reales ni almacenamiento de datos bancarios.

## 2. Criterio de éxito del MVP

El MVP se considera demostrable cuando se puede completar de punta a punta este recorrido:

1. Un administrador configura paradas, tramos, servicios, tarifas y un abono.
2. Un pasajero busca un viaje de Ombúes a Colonia.
3. El sistema presenta un itinerario con dos tramos: Ombúes–Radial de Conchillas y Radial de Conchillas–Colonia.
4. El pasajero realiza un pago simulado aprobado.
5. El sistema emite dos tickets, cada uno con su propio QR.
6. El cobrador valida cada ticket desde la aplicación móvil.
7. Un segundo intento de usar el mismo ticket es rechazado.
8. El pasajero también puede mostrar sus dos abonos y observar cómo se descuenta una unidad del abono correspondiente en cada tramo.

## 3. Evidencia, decisiones y dudas

Para evitar convertir recuerdos o simplificaciones académicas en reglas reales, el documento usa tres categorías.

### 3.1 Reglas confirmadas por la experiencia del usuario

- Un viaje comprado en agencia desde Ombúes hasta Colonia genera dos tickets físicos.
- El pasajero utiliza un ticket en el tramo Ombúes–Radial y otro en Radial–Colonia.
- Si paga arriba de los ómnibus, abona nuevamente al subir al segundo vehículo.
- Los tickets comprados en agencia quedan vinculados a una fecha y un horario.
- Ombúes–Radial no tiene asiento asignado.
- Radial–Colonia sí tiene asiento asignado.
- El abono del recorrido completo se representa con dos abonos independientes, uno por tramo.
- Ambos abonos se emiten con la misma cantidad inicial de unidades.
- Cada viaje consume una unidad del abono correspondiente, tanto a la ida como al regreso.
- Se observó un abono de 20 unidades.
- El pago presencial observado es en efectivo.

### 3.2 Decisiones académicas del MVP

- La compra digital del itinerario completo se paga en una sola operación simulada y emite todos sus tickets de tramo.
- El asiento del tramo Radial–Colonia se asigna automáticamente. No se implementa un mapa de asientos.
- Cada ticket tiene un QR diferente y de un solo uso.
- Cada abono tiene su propio QR; validarlo descuenta únicamente una unidad de ese abono.
- La validación requiere conexión con la API. El trabajo sin conexión queda fuera del MVP.
- El administrador asigna o renueva abonos. No se modela la solicitud ni aprobación de becas.
- Las cuentas de demostración se precargan para cada rol.

### 3.3 Dudas que no deben resolverse inventando

- Si el pasajero puede elegir un asiento específico en Radial–Colonia.
- Cuáles son exactamente las cantidades permitidas para un abono además de 20 unidades.
- Cómo se aplica la política de cambio o reprogramación a cada tipo de ticket.
- Qué requisitos académicos habrá para base de datos, autenticación, documentación y despliegue.
- Cuáles serán los horarios, tarifas y cupos reales.

Estas dudas no bloquean el prototipo: se usarán datos ficticios y reglas configurables hasta recibir la consigna.

## 4. Actores y canales

| Actor | Blazor | Aplicación móvil | Responsabilidades principales |
| --- | --- | --- | --- |
| Pasajero | Sí | Sí | Buscar viajes, comprar, consultar tickets, mostrar QR y consultar abonos/saldos. |
| Cobrador | No | Sí | Escanear y validar tickets o abonos del tramo actual. |
| Administrador | Sí | No | Gestionar rutas, servicios, tarifas, usuarios y abonos; consultar operaciones. |

La aplicación móvil será una sola. Después de iniciar sesión, la navegación y las acciones disponibles dependerán del rol. La API deberá autorizar cada operación; ocultar una pantalla no será considerado seguridad suficiente.

## 5. Alcance funcional

### 5.1 Autenticación y autorización

- Iniciar y cerrar sesión.
- Identificar al usuario autenticado y su rol.
- Proteger las operaciones de pasajero, cobrador y administrador desde la API.
- Utilizar cuentas ficticias para la demostración.

### 5.2 Portal y experiencia del pasajero

- Buscar un viaje indicando origen, destino, fecha y servicio disponible.
- Visualizar el itinerario completo antes de comprar.
- Ver los tramos, horarios, transbordo, precio total y política de asiento de cada tramo.
- Confirmar la compra mediante un pago simulado.
- Recibir un ticket independiente por tramo.
- Consultar tickets vigentes y utilizados.
- Mostrar el QR de un ticket.
- Consultar abonos, vigencia y unidades restantes.
- Mostrar el QR del abono correspondiente al tramo.
- Ver estados de carga, resultado vacío, error, éxito y reintento.

### 5.3 Experiencia del cobrador

- Escanear el QR de un ticket o abono.
- Enviar el código y el contexto del tramo actual a la API.
- Recibir una respuesta clara: válido, ya utilizado, vencido, sin saldo, tramo incorrecto o código inexistente.
- Mostrar la información mínima necesaria para comprobar la operación.
- Impedir una segunda validación exitosa del mismo ticket.
- Descontar una unidad del abono de forma atómica cuando la validación sea correcta.

### 5.4 Panel administrativo

- Gestionar paradas y tramos.
- Gestionar servicios con fecha, horario, origen, destino y capacidad básica.
- Configurar si un servicio requiere asiento.
- Gestionar tarifas por tramo.
- Consultar y gestionar usuarios y roles de demostración.
- Asignar o renovar dos abonos relacionados para un itinerario, con la misma cantidad inicial de unidades.
- Consultar pagos simulados y validaciones.
- Registrar quién realizó cambios administrativos relevantes y cuándo.

### 5.5 Pago simulado

- Permitir resultados deterministas de aprobación y rechazo.
- Registrar importe, fecha, usuario, referencia de compra y estado.
- Emitir tickets únicamente después de una aprobación.
- No solicitar ni guardar número real de tarjeta, CVV, cuenta bancaria ni documento financiero equivalente.

## 6. Flujos principales

### 6.1 Compra de Ombúes a Colonia

1. El pasajero selecciona Ombúes, Colonia y una fecha.
2. La API encuentra dos servicios compatibles conectados por Radial de Conchillas.
3. La interfaz muestra el itinerario y el precio total.
4. El primer tramo indica “sin asiento asignado”.
5. El segundo tramo muestra el asiento asignado automáticamente.
6. El pasajero confirma el pago simulado.
7. Si se aprueba, la API crea la compra y los dos tickets en una única operación consistente.
8. El pasajero puede consultar y mostrar cada QR por separado.
9. Si se rechaza, no se emiten tickets y la interfaz permite reintentar.

### 6.2 Validación de ticket

1. El cobrador inicia sesión y selecciona el servicio o tramo actual.
2. Escanea el QR presentado por el pasajero.
3. La API verifica que el código exista, pertenezca al tramo, esté vigente y no haya sido utilizado.
4. La API marca el ticket como utilizado de forma atómica.
5. La aplicación muestra el resultado con texto e indicador visual.
6. Un nuevo escaneo del mismo ticket devuelve “ya utilizado”.

### 6.3 Asignación y uso de abonos

1. El administrador selecciona un pasajero, período, itinerario y cantidad de unidades.
2. El sistema crea dos abonos relacionados con igual cantidad inicial: uno para Ombúes–Radial y otro para Radial–Colonia.
3. El pasajero consulta ambos desde la web o la aplicación móvil.
4. En el primer vehículo muestra el QR de Ombúes–Radial.
5. La validación descuenta una unidad solamente de ese abono.
6. En el segundo vehículo muestra el QR de Radial–Colonia.
7. La segunda validación descuenta una unidad del segundo abono.
8. El historial conserva fecha, servicio, cobrador y saldo resultante.

## 7. Reglas de negocio mínimas

| ID | Regla |
| --- | --- |
| RN-01 | Un itinerario puede contener uno o más tramos ordenados. |
| RN-02 | Una compra aprobada genera un ticket por cada tramo. |
| RN-03 | Cada ticket posee un identificador QR opaco; el QR no contiene datos personales ni el estado completo. |
| RN-04 | Un ticket solo puede validarse una vez y para su servicio/tramo correspondiente. |
| RN-05 | El asiento es opcional en el modelo de ticket y obligatorio solo cuando el servicio lo requiere. |
| RN-06 | Ombúes–Radial no asigna asiento; Radial–Colonia sí lo asigna en los datos de demostración. |
| RN-07 | Los tickets se emiten únicamente después de aprobar el pago simulado. |
| RN-08 | Dos abonos relacionados comienzan con la misma cantidad de unidades, pero mantienen saldos independientes. |
| RN-09 | Una validación correcta de abono descuenta exactamente una unidad del abono del tramo validado. |
| RN-10 | Un abono vencido, suspendido o sin saldo no puede utilizarse. |
| RN-11 | La API, no la interfaz, decide si una validación es válida y aplica el cambio atómicamente. |
| RN-12 | Cada endpoint sensible exige el rol correspondiente. |
| RN-13 | En el MVP, un abono autoriza el par de paradas indicado en ambos sentidos; cada validación consume una unidad. |

## 8. Modelo de dominio preliminar

| Entidad | Responsabilidad y datos esenciales |
| --- | --- |
| Usuario | Identidad, credenciales académicas, estado y roles. |
| Parada | Nombre y ubicación descriptiva. |
| Tramo | Parada de origen, parada de destino y orden dentro de una ruta. |
| Servicio | Tramo, fecha, horario, capacidad y política de asiento. |
| Tarifa | Precio vigente para un tramo o servicio. |
| Itinerario | Combinación ordenada de servicios que conecta origen y destino. |
| Compra | Pasajero, itinerario, importe total, pago y fecha. |
| Pago | Estado simulado: pendiente, aprobado o rechazado. |
| Ticket | Servicio, pasajero, QR opaco, estado y asiento opcional. |
| Asignación de abonos | Agrupa los abonos entregados juntos para un itinerario y período. |
| Abono | Pasajero, tipo —por ejemplo, estudiante—, par de paradas autorizado, vigencia, unidades iniciales, saldo y QR opaco. |
| Validación | Tipo, ticket o abono, servicio, cobrador, fecha, resultado y saldo resultante cuando corresponda. |

Los modelos expuestos por la API serán contratos explícitos. Los clientes no dependerán directamente de las entidades de persistencia.

## 9. Estados relevantes

### Ticket

- `Vigente`
- `Utilizado`
- `Vencido`
- `Cancelado` — reservado para una etapa posterior

### Pago simulado

- `Pendiente`
- `Aprobado`
- `Rechazado`
- `Reembolsado` — fuera del flujo principal del MVP

### Abono

- `Activo`
- `Agotado`
- `Vencido`
- `Suspendido`

Las transiciones serán controladas por la API. La interfaz mostrará el estado, pero no lo decidirá por sí sola.

## 10. Arquitectura y límites

| Componente | Responsabilidad |
| --- | --- |
| Blazor Web App | Portal del pasajero, formularios validados y panel administrativo. |
| React Native + Expo | Compra/consulta del pasajero y escaneo/validación del cobrador según el rol. |
| API ASP.NET Core | Autenticación, autorización, reglas, contratos, pagos simulados y validaciones atómicas. |
| Persistencia | Usuarios, configuración operativa, compras, tickets, abonos, pagos y validaciones. Tecnología pendiente de la consigna. |
| Simulador de pagos | Produce respuestas de prueba controladas sin integrar dinero real. |

No se elegirán todavía bibliotecas de QR, autenticación ni persistencia. Esa selección deberá considerar la consigna, compatibilidad con Expo Go y el contenido enseñado en clase.

## 11. Escenarios de aceptación

| ID | Dado | Cuando | Entonces |
| --- | --- | --- | --- |
| CA-01 | Hay servicios compatibles Ombúes–Radial y Radial–Colonia | El pasajero busca Ombúes–Colonia | Recibe un itinerario ordenado de dos tramos. |
| CA-02 | El itinerario es válido | El pago simulado se aprueba | Se crean una compra y dos tickets con QR distintos. |
| CA-03 | El pago simulado se rechaza | Finaliza el intento | No se emiten tickets y se puede reintentar. |
| CA-04 | El ticket corresponde al tramo actual | El cobrador lo escanea por primera vez | La validación es exitosa y el ticket queda utilizado. |
| CA-05 | El ticket ya fue utilizado | Se vuelve a escanear | La API rechaza el uso duplicado. |
| CA-06 | El ticket pertenece a otro tramo | El cobrador lo escanea | La API informa “tramo incorrecto” y no lo consume. |
| CA-07 | Un pasajero recibe una asignación de 20 unidades | El administrador confirma | Se crean dos abonos de 20 unidades con saldos independientes. |
| CA-08 | El abono está activo y tiene saldo | Se valida en su tramo | El saldo disminuye exactamente en una unidad. |
| CA-09 | El abono no tiene saldo | Se intenta validar | La operación es rechazada sin generar saldo negativo. |
| CA-10 | Un usuario pasajero conoce una URL administrativa | Intenta ejecutar la operación | La API responde sin autorización. |

## 12. Requisitos de calidad del MVP

- Validaciones de ticket y abono consistentes ante intentos simultáneos.
- Contratos tipados en C# y TypeScript, sin usar `any` para ocultar errores.
- Formularios Blazor con `EditForm`, anotaciones y mensajes por campo.
- Pantallas móviles con estados de carga, vacío, error, éxito y reintento.
- QR sin información personal visible ni reglas de negocio embebidas.
- Registro de errores suficiente para explicar una demostración fallida.
- Navegación clara, un objetivo principal por pantalla y controles táctiles accesibles.
- Datos ficticios claramente identificados.

## 13. Fuera del MVP

- Pasarela de pago real y almacenamiento de tarjetas.
- Integración con sistemas internos de Berrutti o de la Intendencia.
- Solicitud, evaluación o financiación de becas.
- Venta presencial en agencia y cobro en efectivo arriba del ómnibus.
- Funcionamiento sin conexión y sincronización posterior.
- Geolocalización del ómnibus en tiempo real.
- Notificaciones push.
- Mapa interactivo para elegir asiento.
- Reprogramaciones, devoluciones y reembolsos completos.
- Optimización automática de recorridos o conexiones.
- Tarifas, horarios y cupos reales.
- Aplicaciones separadas para pasajero y cobrador.

## 14. Datos mínimos de demostración

- Tres paradas: Ombúes, Radial de Conchillas y Colonia.
- Dos servicios conectados para una fecha de prueba.
- Un servicio sin asiento y otro con asignación automática.
- Una tarifa por tramo y un total calculado.
- Un pasajero, un cobrador y un administrador ficticios.
- Dos abonos relacionados de 20 unidades.
- Un escenario de pago aprobado y otro rechazado.

## 15. División sugerida entre dos estudiantes

La división debe hacerse por funcionalidades verticales y con revisión cruzada, no separando completamente “frontend” y “backend”.

| Estudiante | Responsabilidad inicial | Revisión cruzada |
| --- | --- | --- |
| A | Búsqueda, itinerarios, compra, pago simulado y tickets. | Revisa abonos y validación. |
| B | Abonos, QR, validación y administración operativa. | Revisa compra y tickets. |

Ambos deben trabajar en Blazor, API y React Native durante el proyecto para poder explicar la integración completa.

## 16. Riesgos principales

| Riesgo | Tratamiento |
| --- | --- |
| La consigna contradice esta propuesta | Adaptar primero alcance y contratos; no defender decisiones preliminares. |
| El proyecto crece por duplicar web y móvil | Compartir API y contratos conceptuales; priorizar los flujos de demostración. |
| Se implementan reglas reales no confirmadas | Mantenerlas configurables o marcadas como duda. |
| Dos validaciones consumen el mismo recurso | Aplicar la validación y el cambio de estado en una operación atómica. |
| La biblioteca de escaneo no funciona en Expo Go | Hacer una prueba técnica temprana antes de comprometer la implementación. |
| Se confunde prototipo con producto oficial | Mostrar el aviso académico en documentación y datos de demostración. |

## 17. Orden recomendado de construcción

1. Crear solución, clientes y API con autenticación mínima por roles.
2. Configurar paradas, tramos, servicios y tarifas ficticias.
3. Implementar búsqueda e itinerario de dos tramos.
4. Implementar pago simulado y emisión de tickets.
5. Mostrar QR y validar ticket de un solo uso.
6. Crear, consultar y consumir abonos.
7. Completar panel administrativo y registro de operaciones.
8. Preparar datos, estados de error y recorrido de demostración.

Cada etapa debe terminar en una integración ejecutable, no en capas aisladas sin recorrido visible.

## 18. Definición de terminado

- [ ] Los tres roles pueden autenticarse con cuentas ficticias.
- [ ] Blazor permite comprar como pasajero y administrar datos básicos.
- [ ] React Native permite comprar/consultar como pasajero y validar como cobrador.
- [ ] La API protege operaciones por rol.
- [ ] Una compra aprobada emite dos tickets para el recorrido de demostración.
- [ ] La política de asiento difiere correctamente entre ambos tramos.
- [ ] Un QR utilizado no puede volver a validarse.
- [ ] Los dos abonos empiezan con igual cupo y consumen saldos independientes.
- [ ] El pago rechazado no genera tickets.
- [ ] Las pantallas remotas contemplan carga, vacío, error, éxito y reintento.
- [ ] No se utilizan datos financieros ni datos operativos reales.
- [ ] Las dudas pendientes están documentadas y no escondidas en el código.

## 19. Próxima revisión

Cuando se publique la consigna, este documento deberá compararse contra ella punto por punto. La consigna definirá qué partes se mantienen, cuáles se eliminan y qué requisitos nuevos pasan a ser obligatorios.
