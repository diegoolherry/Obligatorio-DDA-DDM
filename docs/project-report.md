# Obligatorio Berruti — Informe académico del proyecto — pasajes y abonos

> **Estado del documento:** borrador vivo; Markdown es la fuente para la futura entrega en Word. No representa un sistema oficial ni un producto desplegado. Los datos operativos son ficticios.
>
> **Leyenda:** **Verificado** = respaldado por evidencia revisable; **Decidido** = elección del equipo todavía no necesariamente implementada; **Planificado** = objetivo con trabajo pendiente; **Pendiente** = falta decisión, evidencia o confirmación. Cada afirmación de ejecución deberá enlazar su prueba, fecha y resultado.

## 1. Resumen y estado actual

**Obligatorio Berruti** es el nombre del proyecto académico; **Berrutti** refiere al operador real que inspiró el caso, sin implicar afiliación ni aval.

El proyecto académico propone una plataforma de venta y validación de pasajes y abonos por tramo: un portal Blazor para pasajeros y administración, una aplicación React Native/Expo para pasajeros y cobradores, y una API compartida. La compra propone la API de Mercado Pago con credenciales TEST (aún sin integración implementada ni cobros reales), cada tramo emite su propio ticket QR y el cobrador valida tickets o descuenta unidades del abono correspondiente. La consulta de posición, llegada y demora es una demostración básica con datos ficticios, no seguimiento operativo garantizado. El alcance detallado y sus límites están en [MVP](mvp.md).

| Tema | Estado | Evidencia / próximo paso |
| --- | --- | --- |
| Alcance, requisitos y escenarios | **Planificado** | [Requerimientos](requerimientos.md), [historias](user-stories.md) y [MVP](mvp.md); contrastar con la consigna oficial. |
| Ejecución y planificación final | **Estado del backlog, no funcionalidad** | [Backlog del proyecto completo](tasks.json): 19 registros en un único array `tasks`: 14 originales (diez MVP y cuatro FIN) y cinco hijos ejecutables de MVP-001 (`MVP-001-01` a `MVP-001-05`, por `parentId`); 18 `pending`, uno `in_progress` (MVP-010), ninguno `done`. Los hijos tienen a Diego como responsable confirmado y a Enzo como revisor. Los originales con hijos son hitos, no funcionalidades adicionales; estos conteos no miden implementación ni completitud independiente. Clasificación por áreas `API`, `Web`, `Mobile` y `Docs`, con varias áreas para tareas transversales e IDs legados `MVP-*`/`FIN-*` estables. Responsables FIN propuestos, sin aprobación humana de asignación; revisión cruzada de Enzo del modelado pendiente. |
| Clientes y API compartida | **Decidido** | Portal Blazor + móvil React Native/TypeScript/Expo, API para reglas y contratos explícitos; verificar arquitectura en código al comenzar. |
| Arquitectura de API | **Decidido** | Controllers → Services → Repositories → EF Core DbContext; documentar interfaces y responsabilidades con evidencia al implementarlas. |
| Persistencia | **Decidido** (motor), **propuesto** (modelo) | MySQL seleccionado e instalado localmente; [MVP §10](mvp.md#10-arquitectura-y-límites) refleja el motor. El [borrador UML y ER](design/data-model.md) requiere decisiones y revisión de Enzo; esquema, integración EF Core, usuario de aplicación y entorno de producción siguen pendientes. |
| Pagos TEST | **Decidido, revisable; no implementado** | [MVP §5.6](mvp.md#56-pago-de-prueba-con-mercado-pago): API de Mercado Pago en TEST, resultado verificado por la API antes de emitir tickets. El usuario acordó intento durable de compra con pagos conservados: rechazo definitivo permite reintentar dentro del tiempo restante del hold; pendiente o desconocido bloquea otro pago hasta resolución autoritativa, aun vencido el hold. Hold por asiento/salida/intervalo de **5 minutos configurables desde creación en API**, sin reinicio por recarga o reintento. Aprobación verificada, disponibilidad confirmada **y hold vigente** generan una sola compra y sus tickets idempotentemente ([modelo §2](design/data-model.md#2-persistencia-candidata-er-mysql)). Vencer libera el asiento, no declara rechazo. Aprobación posterior al vencimiento no emite ni confirma, aunque haya lugar: requiere seguimiento de devolución hasta confirmación autoritativa. Esquema, transacciones, checkout, estados externos y soporte TEST de devolución pendientes; producción necesita decisión futura. |
| Despliegue | **Pendiente** | No hay entorno de producción, proveedor ni despliegue comprobados en este informe. |

El MVP de las primeras semanas de octubre es un corte inicial; la entrega completa incorpora las mejoras del equipo del [alcance final](final-scope.md), no requisitos académicos explícitos: catálogo/calendarios, planos académicos de vehículos y selección web con disponibilidad por intervalo. El [modelo complementario](design/data-model.md#5-modelo-conceptual-complementario-de-la-entrega-final) no sustituye el UML/ER MVP. Todo sigue planificado, sin implementación ni revisión humana acreditada.

## 2. Contexto, problema y objetivos

- **Problema y destinatarios — Planificado:** permitir a pasajeros consultar y comprar un itinerario de varios tramos, presentar cada comprobante y consultar abonos; permitir al cobrador validarlos y al administrador preparar datos ficticios. Reconstruir la justificación y antecedentes para la entrega sin atribuir patrocinio a la empresa que inspiró el caso.
- **Objetivo demostrable — Planificado:** recorrido Ombúes → Radial de Conchillas → Colonia, con dos tickets, validación de un solo uso y dos abonos de saldos independientes; agregar consulta básica de ubicación con estados de información vigente, desactualizada y ausente. Ver criterios en [MVP §2](mvp.md#2-criterio-de-éxito-del-mvp).
- **Límites — Decidido:** sin cobro real, datos financieros reales, integración empresarial, validación sin conexión ni predicción avanzada. Consultar [MVP §13](mvp.md#13-fuera-del-mvp) antes de ampliar alcance.
- **Pendiente:** confirmar consigna oficial, supuestos de operación y objetivos académicos definitivos; separar experiencia relatada, simplificación de demo y requisito exigido.

## 3. Requisitos y trazabilidad

| Entregable | Fuente vigente | Ampliación necesaria en este informe |
| --- | --- | --- |
| Actores, flujos y reglas de negocio | [MVP](mvp.md) | Explicar contexto y decisiones que afectan a ambos clientes. |
| Requisitos funcionales y no funcionales | [RF/RNF](requerimientos.md) | Sintetizar prioridades, restricciones y mediciones cuando exista evidencia. |
| Historias y aceptación | [Historias](user-stories.md) | Enlazar demostraciones y resultados sin copiar criterios volátiles. |
| Estado, dependencias y responsables por corte | [Backlog del proyecto completo](tasks.json) | MVP-010 diseña antes de MVP-001; resumir únicamente hitos realmente terminados y evidencia de revisión cruzada. |

**Pendiente:** matriz final requisito → historia → tarea → prueba/resultado; criterios de aceptación ejecutados y cambios de alcance aprobados. El backlog, no este resumen, determina qué tarea está terminada.

## 4. Solución y decisiones tecnológicas

| Área | Estado | Decisión / evidencia a incorporar |
| --- | --- | --- |
| Canales | **Decidido** | Blazor para pasajero y administración; una app React Native/Expo con vistas según rol para pasajero y cobrador. [Guía UX](../DESIGN.md) contiene propuestas visuales, no UI implementada. |
| Límite compartido | **Decidido** | API con DTO/contratos explícitos para ambos clientes; autorización y reglas en servidor, no en visibilidad de pantallas. |
| Capas del servidor | **Decidido** | Controllers → Services → Repositories → EF Core DbContext; documentar dependencias, persistencia y manejo transaccional al existir código. |
| Base de datos | **Decidido** | MySQL instalado y conexión administrativa local comprobada con `root`; **pendiente** crear el usuario de aplicación y verificar EF Core, migraciones, respaldos y compatibilidad de despliegue. |
| QR, pagos y ubicación | **Planificado** | QR opacos, escenarios de aprobación/rechazo del proveedor en TEST y posición de dispositivo embarcado autorizado. La aplicación no solicita ni guarda números de tarjeta/CVV; faltan integración, selección de checkout y pruebas. |

**Acuerdo conceptual de pagos y hold — Decidido por el usuario, revisión humana de Enzo pendiente:** el intento durable conserva sus pagos y es distinto del hold y de la compra confirmada. Hold por asiento/salida/intervalo de viaje de **5 minutos configurables desde creación en API**; recarga o reintento tras rechazo definitivo no reinician el plazo. Ese rechazo permite otro intento dentro del tiempo restante. Pendiente/desconocido, incluida falla de red, bloquea otro pago hasta resolución autoritativa incluso tras el vencimiento. La API exige aprobación verificada, disponibilidad confirmada y hold vigente para una sola compra y sus tickets idempotentemente. Vencer libera el asiento pero no prueba rechazo. Aprobación después del vencimiento no confirma ni emite, aunque el asiento siga disponible: registrar y seguir compensación/devolución; no declarar devuelto por haberlo solicitado, sino tras confirmación autoritativa. Detalle en [modelo §2](design/data-model.md#2-persistencia-candidata-er-mysql).

**Diseño e integración pendientes:** esquema físico, transacciones, concurrencia, checkout y mapeo/verificación del proveedor; comparación temporal, timestamp autoritativo y carreras aprobación/notificación/vencimiento. Notificación tardía no equivale automáticamente a aprobación tardía. Soporte de devoluciones en TEST **no verificado**, sin autorización de reemplazo simulado. No se acuerdan transbordos todo-o-nada, límites por usuario, corte antes de salida, cancelación/renovación ni reintento después del vencimiento. No hay implementación ni pruebas externas acreditadas.

**Alternativas y justificación — Pendiente:** comparar opciones de persistencia, hosting, autenticación, generación/lectura QR y publicación de ubicación según consigna, costo, compatibilidad y riesgos; registrar descartes con fuentes. No confundir tecnología elegida con integración completada.

## 5. Ambientes, factibilidad y recursos

| Apartado de la entrega | Estado | Qué falta documentar |
| --- | --- | --- |
| Desarrollo | **Pendiente** | Versiones verificadas de SDK .NET, Node/Expo, MySQL, IDE, SO, configuración reproducible y dependencias. |
| Producción y despliegue | **Pendiente** | Proveedor, topología, red, secretos, base de datos, dominio, respaldos, observabilidad y procedimiento de publicación/reversión. |
| Usuario final | **Planificado** | Navegador para Blazor, celular compatible para pasajero/cobrador, cámara y conectividad para QR; comprobar requisitos mínimos y permisos. |
| Factibilidad técnica | **Pendiente** | Prueba temprana de cámara/QR en Expo, transacciones para consumos concurrentes, compatibilidad de API y base de datos. |
| Factibilidad operativa y capacitación | **Pendiente** | Guías de uso para tres roles, datos de demo, permisos, recuperación de fallos y entrenamiento necesario. |
| Factibilidad económica y legal | **Pendiente** | Costos de hosting/dispositivos/tiempo, licencias, privacidad de cuentas y ubicación, uso de marca e imágenes; no atribuir aval empresarial. |
| Herramientas y alternativas | **Pendiente** | Inventario con versiones, finalidad, costos/licencias, alternativas evaluadas y criterios de selección. |

## 6. Equipo, iteraciones y cronograma

**Planificado:** primero revisión del modelado MVP-010 (Diego, revisor Enzo); MVP-001 depende de su finalización, sin iniciarse aún. La descomposición aprobada de MVP-001 organiza cinco hijos pendientes: estructura integrada, contratos tipados y sesiones de pasajero (Web/Mobile), conductor/cobrador (Mobile, rol cobrador del MVP) y administrador (Web), con autorización comprobable en API. Todos dependen explícitamente de MVP-010; contratos depende además de estructura y cada flujo de sesión de contratos, sin dependencias entre flujos ni hacia el padre. La jerarquía se deriva de `parentId`, sin agregación automática de estados. El padre conserva su puerta original y criterios autoritativos; solo podrá completarse con todos los hijos en `done`, criterios propios verificados y revisión cruzada, según [AGENTS.md](../AGENTS.md). Ningún hijo puede comenzar con dependencias pendientes. Esto organiza trabajo futuro, no acredita autenticación, compilaciones ni pruebas. Luego dos estudiantes desarrollan cortes verticales que atraviesan clientes y API, con revisión cruzada. [Backlog del proyecto completo](tasks.json) fija responsables, revisor, dependencias y orden sugerido; no sustituirlo por fechas inferidas. **Pendiente:** explicitar roles efectivos, disponibilidad, formación necesaria, iteraciones, hitos, estimaciones, calendario y ruta crítica (dependencias MVP-001/002 antes de flujos consumidores). Registrar desvíos y decisiones con fecha cuando ocurran; no declarar hitos alcanzados por el mero hecho de estar planificados.

Para la entrega final, FIN-001..004 ordenan catálogo/calendarios, configuraciones de vehículos, disponibilidad concurrente/compra y selección web con pruebas end-to-end, sobre fundamentos MVP. No se fija fecha final ni se inician tareas con dependencias pendientes. Las políticas de reintento, hold y aprobación posterior al vencimiento ya fueron acordadas por el usuario, sin acreditar revisión cruzada. Sus mecanismos y diseño físico requieren definición y revisión antes de implementar. La [puerta histórica](final-scope.md#puerta-abierta-reserva-temporal-y-pago) y las discusiones del [backlog vigente](tasks.json) sobre política aún pendiente quedan superadas por el acuerdo actual de §4; sus requisitos y los estados/criterios del backlog conservan autoridad para completar tareas. La transcripción/verificación completa del horario y una fuente reproducible compartible siguen pendientes; no se infieren tarifas ni flota real del horario.

## 7. Calidad, pruebas y gestión de configuración

**Planificado:** verificar autorización por rol, emisión idempotente solo tras aprobación TEST verificada por la API (no por el cliente) y disponibilidad confirmada con hold vigente, bloqueo de otro pago ante resultado pendiente/desconocido aun vencido el hold y reintento tras rechazo definitivo dentro del plazo restante, liberación por vencimiento sin inferir rechazo y seguimiento de devolución por aprobación posterior al vencimiento, QR de un solo uso, consumo atómico de abonos, estados de carga/error y datos de ubicación vencidos o insuficientes. [RNF y evidencia esperada](requerimientos.md#requisitos-no-funcionales) definen qué medir; faltan resultados. **Pendiente:** estrategia y herramientas de pruebas unitarias, integración y recorrido manual, matriz de casos y resultados con comando/fecha/entorno; control de versiones, ramas, revisión cruzada, registro de defectos, cambios y versiones entregables. No convertir criterios de aceptación en afirmaciones de pruebas exitosas. El [checklist para Enzo](design/data-model.md#escenarios-esperados-para-revisión-de-enzo) contiene expectativas de diseño, no pruebas ejecutadas ni revisión humana completada. **Verificado:** el workflow [Documentation validation](../.github/workflows/docs-validation.yml) terminó en `SUCCESS` en el [PR #4](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/4), luego mergeado. Ese resultado corresponde a la versión del backlog anterior a la migración. El workflow vigente comprueba la sintaxis de `docs/tasks.json` y los espacios del diff contra el commit base; su ejecución con la nueva ruta sigue pendiente. No valida semántica del backlog, renderizado Mermaid ni reemplaza la revisión cruzada de Enzo.

## 8. Riesgos, puesta en marcha y cierre

| Tema | Estado | Seguimiento |
| --- | --- | --- |
| Consigna y alcance | **Pendiente** | Contrastar propuesta con consigna oficial; conservar decisiones y dudas de [MVP](mvp.md). |
| Integración web/móvil/API | **Planificado** | Probar contratos comunes temprano y evitar duplicación de reglas. |
| Concurrencia y seguridad | **Planificado** | Probar validaciones simultáneas y autorización, sin exponer datos en QR. |
| Cámara y conectividad | **Pendiente** | Validar dispositivo/Expo y resultados cuando no hay red. |
| Ubicación académica | **Planificado** | Marcar antigüedad y precisión limitada; no atribuir telemetría oficial. |
| Implantación y soporte | **Pendiente** | Plan de instalación, configuración, demostración, monitoreo, rollback y soporte; nada desplegado se presume. |

**Conclusiones — Pendiente:** redactar al cierre con logros medidos, limitaciones, desvíos, aprendizaje y trabajo futuro. **Bibliografía — Pendiente:** reunir consigna, material docente, documentación técnica con versión/fecha y fuentes utilizadas; diferenciar referencias de evidencia de ejecución.

## 9. Mantenimiento y preparación de entrega

Tras un cambio importante en requisitos, arquitectura, tecnología, tareas del proyecto completadas, pruebas, despliegue, riesgos, cronograma o equipo, revisar esta síntesis y actualizar estados y evidencia; la regla operativa figura en [AGENTS.md](../AGENTS.md). Mantener enlaces a fuentes canónicas y ampliar secciones solo con información comprobable. Antes de exportar a Word: reconciliar alcance y backlog, completar pendientes o declararlos como limitaciones, verificar enlaces y citas, y revisar formato/tablas en el documento convertido. El archivo Markdown sigue siendo la fuente de verdad. El control automático propuesto en [§7](#7-calidad-pruebas-y-gestión-de-configuración) no acredita por sí solo la entrega: quedan pendientes el resultado del check del PR y la revisión humana.
