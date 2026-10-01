# Obligatorio Berruti — MVP académico — Venta y validación de pasajes y abonos

> **Estado:** definición preliminar previa a la consigna oficial.
>
> **Propósito:** proyecto académico inspirado en la operativa observada de Berrutti. No representa un sistema oficial, aprobado ni encargado por la empresa.

## 1. Resultado esperado

El MVP permitirá que una persona compre digitalmente un viaje compuesto por uno o más tramos, reciba un ticket QR por cada tramo y pueda mostrarlo al cobrador. También permitirá administrar abonos por tramo y descontar una unidad cada vez que el cobrador valide su QR. Un dispositivo o celular transportado en el ómnibus publicará posiciones básicas para un servicio de demostración; el pasajero consultará la última posición conocida, una llegada aproximada y un estado básico de demora cuando haya datos suficientes.

La solución integrará:

- **Blazor:** portal de pasajeros y panel administrativo.
- **API:** reglas de negocio, autenticación, autorización y persistencia.
- **React Native con TypeScript y Expo:** una aplicación móvil con experiencia diferenciada para pasajero y cobrador.
- **Pago de prueba:** propuesta vigente revisable de integración con la API de Mercado Pago y credenciales TEST; sin cobros reales ni datos de tarjeta almacenados por la aplicación. Aún no implementado.

## 2. Criterio de éxito del MVP

El MVP se considera demostrable cuando se puede completar de punta a punta este recorrido:

1. Un administrador configura paradas, tramos, servicios, tarifas y un abono.
2. Un pasajero busca un viaje de Ombúes a Colonia.
3. El sistema presenta un itinerario con dos tramos: Ombúes–Radial de Conchillas y Radial de Conchillas–Colonia.
4. El pasajero completa un escenario de pago aprobado con Mercado Pago en entorno TEST; la API verifica el resultado del proveedor.
5. El sistema emite dos tickets, cada uno con su propio QR.
6. El cobrador valida cada ticket desde la aplicación móvil.
7. Un segundo intento de usar el mismo ticket es rechazado.
8. El pasajero también puede mostrar sus dos abonos y observar cómo se descuenta una unidad del abono correspondiente en cada tramo.
9. Un dispositivo o celular embarcado autorizado y asociado a un servicio publica periódicamente posiciones ficticias; la demostración contrasta el intervalo entre publicaciones con el parámetro académico configurado. El pasajero consulta la última actualización y, con datos suficientes, una llegada aproximada y el estado básico de demora. También se muestran información desactualizada y ausencia de datos.

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

- La compra digital del itinerario completo se paga en una operación de prueba mediante la API de Mercado Pago con credenciales TEST y emite todos sus tickets de tramo solo tras verificar aprobación con el proveedor. Es una decisión actual revisable, no una integración implementada ni autorización para cobrar dinero real.
- El asiento del tramo Radial–Colonia se asigna automáticamente. No se implementa un mapa de asientos.
- Cada ticket tiene un QR diferente y de un solo uso.
- Cada abono tiene su propio QR; validarlo descuenta únicamente una unidad de ese abono.
- La validación requiere conexión con la API. El trabajo sin conexión queda fuera del MVP.
- El administrador asigna o renueva abonos. No se modela la solicitud ni aprobación de becas.
- Las cuentas de demostración se precargan para cada rol.
- La ubicación y las referencias de llegada/demora son académicas y configurables; no representan seguimiento operativo real.

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
| Pasajero | Sí | Sí | Buscar viajes, comprar, consultar tickets, mostrar QR, consultar abonos/saldos y la información básica de llegada del servicio. |
| Cobrador | No | Sí | Escanear y validar tickets o abonos del tramo actual. |
| Administrador | Sí | No | Gestionar rutas, servicios, tarifas, usuarios, abonos y datos ficticios; consultar operaciones. |
| Dispositivo o celular embarcado | No | Canal de publicación asociado al servicio | Publicar posiciones básicas desde el ómnibus, sujeto a autorización de la API. |

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
- Confirmar la compra mediante un pago de prueba con Mercado Pago; el producto de checkout sigue sin elegirse.
- Recibir un ticket independiente por tramo.
- Consultar tickets vigentes y utilizados.
- Mostrar el QR de un ticket.
- Consultar abonos, vigencia y unidades restantes.
- Mostrar el QR del abono correspondiente al tramo.
- Consultar la última posición conocida y su momento de actualización; distinguir información desactualizada, sin datos o con error y permitir reintentar la consulta.
- Consultar una llegada aproximada y un estado básico de demora (en horario o con demora) solo si existen datos suficientes y referencias configuradas.
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
- Consultar pagos de prueba y validaciones.
- Registrar quién realizó cambios administrativos relevantes y cuándo.

### 5.5 Geolocalización básica de demostración

- Autorizar desde la API un dispositivo o celular transportado en el ómnibus y asociado a un servicio de demostración antes de aceptar su publicación periódica de posiciones básicas.
- Conservar dispositivo, servicio, posición y fecha/hora de recepción de cada publicación aceptada; rechazar identidad no autorizada, servicio inexistente o posición incompleta sin reemplazar la última posición conocida.
- Configurar como parámetros académicos la periodicidad de publicación, el umbral de desactualización y las referencias necesarias para estimar llegada y demora.
- Consultar la última posición conocida y su actualización; marcarla como desactualizada al superar el umbral configurado. La llegada y la demora son aproximadas y se informan como no disponibles si faltan datos vigentes o referencias.

### 5.6 Pago de prueba con Mercado Pago

- Integrar la API de Mercado Pago con credenciales TEST como decisión vigente revisable; probar aprobación y rechazo con escenarios del proveedor, sin inventar mecánica determinista local.
- Registrar importe, fecha, usuario, referencia del intento y estado de dominio; proponer referencia externa y mapeo de estados del proveedor sin cerrar aún el ciclo compra/reintentos.
- La API debe verificar el resultado con el proveedor antes de emitir tickets; nunca confiar únicamente en un éxito informado por el cliente.
- La aplicación no debe solicitar ni guardar número de tarjeta, CVV o datos financieros equivalentes. El producto de checkout está pendiente de selección; producción y cobros reales requieren decisión de alcance explícita.

## 6. Flujos principales

### 6.1 Compra de Ombúes a Colonia

1. El pasajero selecciona Ombúes, Colonia y una fecha.
2. La API encuentra dos servicios compatibles conectados por Radial de Conchillas.
3. La interfaz muestra el itinerario y el precio total.
4. El primer tramo indica “sin asiento asignado”.
5. El segundo tramo muestra el asiento asignado automáticamente.
6. El pasajero realiza el pago en un escenario TEST del proveedor.
7. Si la API verifica la aprobación del proveedor, crea la compra y los dos tickets en una única operación consistente.
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
| RN-07 | Los tickets se emiten únicamente después de que la API verifica la aprobación del pago de prueba con el proveedor; el cliente no decide la aprobación. |
| RN-08 | Dos abonos relacionados comienzan con la misma cantidad de unidades, pero mantienen saldos independientes. |
| RN-09 | Una validación correcta de abono descuenta exactamente una unidad del abono del tramo validado. |
| RN-10 | Un abono vencido, suspendido o sin saldo no puede utilizarse. |
| RN-11 | La API, no la interfaz, decide si una validación es válida y aplica el cambio atómicamente. |
| RN-12 | Cada endpoint sensible exige el rol correspondiente o, para publicar ubicación, la identidad del dispositivo autorizado. |
| RN-13 | En el MVP, un abono autoriza el par de paradas indicado en ambos sentidos; cada validación consume una unidad. |
| RN-14 | Una posición publicada debe provenir de un dispositivo o celular transportado en el ómnibus, autorizado y asociado al servicio de demostración, y conservar su fecha/hora de recepción. |
| RN-15 | La estimación y el estado de demora usan solo referencias y umbrales configurados para el MVP académico. |

## 8. Modelo de dominio preliminar

El [borrador canónico de modelado UML y ER MySQL](design/data-model.md) desarrolla esta lista como propuesta para revisión cruzada; no autoriza migraciones ni resuelve decisiones comerciales pendientes. Primero revisar el dominio, después la representación relacional y finalmente los contratos DTO independientes.

| Entidad | Responsabilidad y datos esenciales |
| --- | --- |
| Usuario | Identidad, credenciales académicas, estado y roles. |
| Parada | Nombre y ubicación descriptiva. |
| Tramo | Parada de origen y destino; el orden dentro de una ruta es provisional y está pendiente de definir frente al orden de servicios en el itinerario. |
| Servicio | Tramo, fecha, horario, capacidad y política de asiento. |
| Tarifa | Precio vigente para un tramo o servicio. |
| Itinerario | Combinación ordenada de servicios que conecta origen y destino. |
| Compra | Pasajero, itinerario, importe total congelado y fecha; su vínculo con reintentos de pago sigue abierto. |
| Pago | Importe, estado del dominio y referencia externa del proveedor cuando exista; un rechazo puede no tener compra emitida. |
| Ticket | Servicio, pasajero, QR opaco, estado y asiento opcional. |
| Asignación de abonos | Agrupa los abonos entregados juntos para un itinerario y período. |
| Abono | Pasajero, tipo —por ejemplo, estudiante—, par de paradas autorizado, vigencia, unidades iniciales, saldo y QR opaco. |
| Validación | Tipo, ticket o abono, servicio, cobrador, fecha, resultado y saldo resultante cuando corresponda. |
| Posición recibida | Dispositivo autorizado, servicio asociado, posición básica y fecha/hora de recepción. |

Los modelos expuestos por la API serán contratos explícitos. Los clientes no dependerán directamente de las entidades de persistencia.

## 9. Estados relevantes

### Ticket

- `Vigente`
- `Utilizado`
- `Vencido`
- `Cancelado` — reservado para una etapa posterior

### Pago de prueba

- `Pendiente`
- `Aprobado`
- `Rechazado`
- `Reembolsado` — fuera del flujo principal del MVP

### Abono

- `Activo`
- `Agotado`
- `Vencido`
- `Suspendido`

Los estados de pago son del dominio propuesto, no una afirmación sobre estados soportados por Mercado Pago; su mapeo está pendiente. Las transiciones serán controladas por la API. La interfaz mostrará el estado, pero no lo decidirá por sí sola.

## 10. Arquitectura y límites

| Componente | Responsabilidad |
| --- | --- |
| Blazor Web App | Portal del pasajero, formularios validados y panel administrativo. |
| React Native + Expo | Compra/consulta del pasajero y escaneo/validación del cobrador según el rol. |
| API ASP.NET Core | Autenticación, autorización, reglas, contratos, verificación del resultado del proveedor TEST antes de emitir tickets, posiciones y validaciones atómicas. |
| Persistencia | MySQL almacenará usuarios, configuración operativa, compras, tickets, abonos, pagos, validaciones y posiciones recibidas. La API accederá mediante EF Core y el patrón Repository. |
| Mercado Pago TEST | Integración API propuesta, aún no implementada; escenarios de prueba del proveedor sin cobro real. Producto de checkout y mecanismo de verificación por decidir. |

MySQL es la tecnología de persistencia decidida para el proyecto y su conexión administrativa local ya fue comprobada. Antes de crear migraciones se deberá seleccionar y verificar un proveedor de EF Core compatible con la versión de .NET, crear un usuario exclusivo para la API y resolver el alojamiento de producción. Las bibliotecas de QR y autenticación continúan pendientes y deberán considerar la consigna, la compatibilidad con Expo Go y el contenido enseñado en clase.

## 11. Escenarios de aceptación

| ID | Dado | Cuando | Entonces |
| --- | --- | --- | --- |
| CA-01 | Hay servicios compatibles Ombúes–Radial y Radial–Colonia | El pasajero busca Ombúes–Colonia | Recibe un itinerario ordenado de dos tramos. |
| CA-02 | El itinerario es válido | La API verifica la aprobación del pago TEST con el proveedor | Se crean una compra y dos tickets con QR distintos. |
| CA-03 | El proveedor rechaza el intento TEST | Finaliza el intento | No se emiten tickets y se puede reintentar. |
| CA-04 | El ticket corresponde al tramo actual | El cobrador lo escanea por primera vez | La validación es exitosa y el ticket queda utilizado. |
| CA-05 | El ticket ya fue utilizado | Se vuelve a escanear | La API rechaza el uso duplicado. |
| CA-06 | El ticket pertenece a otro tramo | El cobrador lo escanea | La API informa “tramo incorrecto” y no lo consume. |
| CA-07 | Un pasajero recibe una asignación de 20 unidades | El administrador confirma | Se crean dos abonos de 20 unidades con saldos independientes. |
| CA-08 | El abono está activo y tiene saldo | Se valida en su tramo | El saldo disminuye exactamente en una unidad. |
| CA-09 | El abono no tiene saldo | Se intenta validar | La operación es rechazada sin generar saldo negativo. |
| CA-10 | Un usuario pasajero conoce una URL administrativa | Intenta ejecutar la operación | La API responde sin autorización. |
| CA-11 | Un dispositivo embarcado autorizado está asociado a un servicio existente | Publica una posición válida | Se conservan dispositivo, servicio, posición y fecha/hora de recepción. |
| CA-12 | El dispositivo no está autorizado, el servicio es inexistente o los datos de posición están incompletos | Intenta publicar | La API rechaza la publicación sin reemplazar la última posición. |
| CA-13 | Existe una posición vigente y referencias académicas suficientes | El pasajero consulta el servicio | Ve la posición y actualización, llegada aproximada y estado básico de demora. |
| CA-14 | La posición está desactualizada, faltan datos o falla la consulta | El pasajero consulta | Ve la limitación o error y puede reintentar; no se fabrica una estimación. |

## 12. Requisitos de calidad del MVP

- Validaciones de ticket y abono consistentes ante intentos simultáneos.
- Contratos tipados en C# y TypeScript, sin usar `any` para ocultar errores.
- Formularios Blazor con `EditForm`, anotaciones y mensajes por campo.
- Pantallas móviles con estados de carga, vacío, error, éxito y reintento.
- QR sin información personal visible ni reglas de negocio embebidas.
- Registro de errores suficiente para explicar una demostración fallida.
- Navegación clara, un objetivo principal por pantalla y controles táctiles accesibles.
- Datos ficticios claramente identificados.
- Periodicidad de publicación y umbral de desactualización visibles como parámetros académicos configurables; información vencida identificada al superar el umbral.
- Llegada y demora identificadas como aproximadas, sin estimaciones cuando faltan datos requeridos; carga, sin datos, error y reintento en la consulta remota.

## 13. Fuera del MVP

- Cobros reales, credenciales de producción y almacenamiento de tarjetas; habilitarlos requeriría una decisión de alcance futura.
- Integración con sistemas internos de Berrutti o de la Intendencia.
- Solicitud, evaluación o financiación de becas.
- Venta presencial en agencia y cobro en efectivo arriba del ómnibus.
- Funcionamiento sin conexión y sincronización posterior.
- Seguimiento continuo garantizado, mapas interactivos, telemetría oficial, precisión operativa y predicción avanzada de llegada o demora.
- Optimización mediante ubicación y analítica histórica de recorridos o conexiones.
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
- Escenarios de pago aprobado y rechazado del proveedor en TEST, pendientes de ejecutar.
- Un dispositivo o celular embarcado ficticio asociado a un servicio y posiciones de demostración con fecha/hora de recepción; casos con datos vigentes, desactualizados y ausentes.
- Parámetros académicos configurados para periodicidad, desactualización y referencias de llegada/demora, con un caso sin datos suficientes.

## 15. División sugerida entre dos estudiantes

La división debe hacerse por funcionalidades verticales y con revisión cruzada, no separando completamente “frontend” y “backend”.

| Estudiante | Responsabilidad inicial | Revisión cruzada |
| --- | --- | --- |
| A | Búsqueda, itinerarios, compra, pago TEST y tickets. | Revisa abonos y validación. |
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
| La ubicación se interpreta como seguimiento operativo preciso | Mostrar actualización, vencimiento y límites de estimación; usar solo datos y parámetros académicos. |

## 17. Orden recomendado de construcción

1. Revisar y acordar el borrador de modelado UML y ER MySQL ([MVP-010](mvp-tasks.json)); es dependencia de MVP-001 y no habilita implementación mientras esté en revisión.
2. Crear solución, clientes y API con autenticación mínima por roles.
3. Configurar paradas, tramos, servicios y tarifas ficticias.
4. Implementar búsqueda e itinerario de dos tramos.
5. Integrar pago TEST de Mercado Pago, verificación por la API y emisión de tickets.
6. Mostrar QR y validar ticket de un solo uso.
7. Crear, consultar y consumir abonos.
8. Completar panel administrativo y registro de operaciones.
9. Integrar los recorridos de compra, validación y abonos de los tres roles; preparar sus datos, estados de error y evidencia reproducible de demostración (MVP-008), sin ampliar esa tarea a geolocalización.
10. Incorporar publicación autorizada de posiciones y consulta básica de ubicación, llegada aproximada y demora con datos ficticios (MVP-009); depende de la fundación y los servicios configurados y se demuestra por separado con posiciones vigentes, desactualizadas y ausentes, más error y reintento.

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
- [ ] Un dispositivo o celular embarcado autorizado y asociado publica posiciones básicas, con fecha/hora de recepción, sin alterar la última posición ante publicaciones inválidas.
- [ ] El pasajero distingue última posición y actualización, datos desactualizados o ausentes y errores con reintento.
- [ ] Llegada y demora aproximadas se muestran solo con datos suficientes y parámetros académicos configurados de publicación, vencimiento y referencias.
- [ ] Las pantallas remotas contemplan carga, vacío, error, éxito y reintento.
- [ ] No se utilizan datos financieros ni datos operativos reales.
- [ ] Las dudas pendientes están documentadas y no escondidas en el código.

## 19. Próxima revisión

Cuando se publique la consigna, este documento deberá compararse contra ella punto por punto. La consigna definirá qué partes se mantienen, cuáles se eliminan y qué requisitos nuevos pasan a ser obligatorios.
