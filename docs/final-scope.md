# Obligatorio Berruti — Alcance de la entrega final

La entrega completa conserva el [MVP](mvp.md) y agrega mejoras elegidas por el equipo: catálogo de rutas/calendarios, configuraciones de vehículos y selección web de asientos con disponibilidad por intervalo. **No son requisitos académicos explícitos ni funcionalidad implementada.** La consigna oficial sigue pendiente. El usuario informa aprobación de Diego y Enzo de la base conceptual evolutiva del [modelo](design/data-model.md), no del esquema físico ni de la implementación de estas ampliaciones.

## 1. Dos cortes, sin reescribir el MVP

| Corte | Objetivo planificado | Fuente |
| --- | --- | --- |
| Primeras semanas de octubre: MVP | Demo Ombúes–Radial–Colonia, asiento automático en el segundo servicio, tickets/abonos y pagos TEST | [MVP vigente](mvp.md) |
| Entrega final completa | MVP más las ampliaciones de este documento; sin fecha final confirmada aquí | [Backlog del proyecto completo](tasks.json), tareas FIN |

El [modelo complementario](design/data-model.md#5-modelo-conceptual-complementario-de-la-entrega-final) no reemplaza el UML/ER MVP ni resuelve sus preguntas abiertas. No se amplían pagos a producción, reglas comerciales, tarifas, telemetría ni otros canales de selección de asiento por esta planificación.

## 2. Catálogo y evidencia de horarios

Familias de rutas a validar, con dirección y variantes explícitas:

- Nueva Palmira–Colonia vía Carmelo y Radial de Conchillas.
- Conexiones de Ombúes y Conchillas.
- Carmelo–Mercedes vía Nueva Palmira y Dolores.
- Nueva Palmira–Carmelo, incluidas variantes por Polancos.
- Nueva Palmira–Agraciada.

La fuente inspeccionada es la imagen `horalinea 02-03-26.jpg`, horario de invierno 2026. El bloque principal Palmira–Colonia indica vigencia **02/03/26–20/12/26**; otros bloques no heredan automáticamente ese fin de vigencia. La fuente distingue días hábiles, fines de semana, feriados, excepciones y llegadas aproximadas: el catálogo deberá conservar y verificar esas diferencias, no convertir aproximaciones en garantías.

**Pendiente:** transcripción completa y contraste de cada bloque, sentidos, paradas, variantes, calendarios y excepciones. Las imágenes aportadas están solo en Downloads del usuario, sin archivo versionado ni URL pública: la reproducción independiente queda pendiente de acordar una referencia compartible y su uso autorizado. No se copian imágenes ni se publican rutas privadas. Del horario no se infieren tarifas, capacidades ni asignaciones reales de flota.

## 3. Planos académicos de vehículos

Configuraciones aprobadas para esta propuesta: **22, 28, 42 y 46 asientos**, con **2+2 y pasillo central**, numeración por fila de izquierda a derecha, avanzando desde el frente. Son planos académicos, no planos oficiales verificados.

| Capacidad | Filas completas de cuatro | Fila final |
| --- | --- | --- |
| 22 | 5: asientos 1–20 | Dos izquierdos: 21–22 |
| 28 | 7: asientos 1–28 | Sin fila parcial |
| 42 | 10: asientos 1–40 | Dos izquierdos: 41–42 |
| 46 | 11: asientos 1–44 | Dos izquierdos: 45–46 |

El vehículo asignado a la salida determina su configuración. Capacidades publicadas no prueban asignaciones a líneas regulares. Una salida sin asignación/configuración válida no puede ofrecer un plano inventado. Un cambio de vehículo exige revalidar selección y compatibilidad antes de confirmar; la política de reasignación queda pendiente.

## 4. Disponibilidad por intervalo

Estas son **reglas y aceptación de diseño**, no garantías implementadas:

1. Una salida es una instancia fechada de una ruta dirigida. Cada parada tiene un orden dentro de esa misma ruta/salida; números de órdenes de rutas distintas no son comparables.
2. El viaje ocupa el intervalo semiabierto `[boardingOrder, alightingOrder)`, con ambos extremos válidos de la misma salida y `boardingOrder < alightingOrder`.
3. Para el mismo asiento de la misma salida, `[a,b)` y `[c,d)` se superponen **si y solo si `a < d && c < b`**. Nunca se venden dos ocupaciones superpuestas.
4. Se reutiliza el asiento en intervalos no superpuestos, incluido relevo en una parada compartida: `[1,3)` y `[3,5)` son compatibles; `[1,4)` y `[3,5)` no; `[1,5)` y `[2,3)` tampoco.
5. El mismo vehículo en dos salidas tiene inventarios separados. Un transbordo a otra salida exige otra selección/asignación: no supone conservar número ni disponibilidad de asiento.
6. La consulta del cliente es orientativa. El servidor revalida salida, ruta, intervalo, vehículo, asiento y disponibilidad al confirmar; la confirmación debe ser **atómica e idempotente**, incluso con solicitudes simultáneas o repetidas. El mecanismo concreto y sus pruebas aún no existen.

## 5. Compra y estados de experiencia

Camino previsto: buscar salida y paradas → consultar plano/disponibilidad para el intervalo → seleccionar asiento por salida → revisar itinerario → pago TEST → confirmación del servidor → tickets. Una selección visual no es una reserva ni una venta confirmada. Se mantienen verificación del proveedor por la API, ausencia de cobros reales y prohibición de guardar tarjetas/CVV del [MVP](mvp.md#56-pago-de-prueba-con-mercado-pago).

| Estado o fallo | Respuesta prevista / límite |
| --- | --- |
| Carga de salidas o plano | Mostrar progreso; evitar confirmar datos incompletos. |
| Vacío / sin asientos compatibles | Explicar ausencia y permitir cambiar salida o intervalo. |
| Ruta/órdenes inválidos, salida o configuración ausente | Rechazar; corregir selección sin inventar plano. |
| Error de servidor / sin conexión | Conservar datos recuperables y ofrecer reintento; no prometer reserva ni compra offline. |
| Disponibilidad cambió / conflicto concurrente | No confirmar el asiento; actualizar disponibilidad y permitir otra selección. |
| Vehículo/configuración cambió | Revalidar y explicar incompatibilidad; no reasignar silenciosamente. |
| Pago pendiente, rechazado o no verificado | No mostrar tickets emitidos. Pendiente/desconocido bloquea otro pago hasta resolución autoritativa, aun vencido el hold; rechazo definitivo permite reintento en el mismo intento durable dentro del plazo restante, sin reiniciarlo. |
| Timeout / respuesta de confirmación perdida | Consultar estado autoritativo antes de repetir; repetición idempotente sin duplicar venta/tickets. |
| Éxito | Mostrar tickets solo tras aprobación TEST verificada por API, disponibilidad confirmada y hold vigente; una sola compra y sus tickets idempotentemente. |
| Aprobación TEST verificada tardía | Si se aprueba después del vencimiento, no confirmar ni emitir aunque haya lugar; registrar y seguir devolución hasta confirmación autoritativa. Notificación tardía no prueba aprobación tardía. |

Los estados deben usar texto además de color y distinguir seleccionado, disponible y confirmado. Las pruebas web deberán cubrir transbordos, intervalos adyacentes/superpuestos, concurrencia, cambios de configuración y recuperación de red.

### Puerta abierta: reserva temporal y pago

**Política conceptual acordada; mecanismos pendientes:** rige el [acuerdo del modelo](design/data-model.md#acuerdo-conceptual-de-compra-hold-y-reintentos), incluido en la base evolutiva aprobada por Diego y Enzo según el usuario. El hold protege asiento/salida/intervalo durante **5 minutos configurables desde su creación en la API**; recargar o reintentar no reinicia el plazo. Vencer libera el asiento, sin declarar rechazo ni resolver pagos pendientes. No asumir que abrir un plano, seleccionar un asiento o iniciar pago crea el hold.

Antes de implementar siguen pendientes persistencia, concurrencia, liberación e idempotencia, checkout y verificación/mapeo del proveedor, timestamp y comparación temporal autoritativos y carreras aprobación/notificación/vencimiento. El soporte de devolución en TEST no está verificado: solicitarla no acredita devolución y no se autoriza reemplazo simulado. FIN-003 conserva la puerta técnica y sus dependencias; no hay integración ni pruebas del proveedor ejecutadas. Transbordos todo-o-nada, límites por usuario, corte antes de salida, cancelación/renovación y reintento después del vencimiento siguen abiertos. Aplicabilidad/vigencia de tarifas y correspondencia bidireccional abono–servicio requieren decisiones futuras antes de implementar esas funciones.

## 6. Implementación posterior

[FIN-001 a FIN-004](tasks.json) ordenan catálogo/calendarios → configuraciones → disponibilidad/compra → selección web y pruebas de punta a punta. Todos siguen `pending`, con responsables **propuestos**, no asignación humana confirmada. La aprobación conceptual reportada no completa implementación ni acredita revisión técnica de sus mecanismos. El [informe](project-report.md) resume ambos cortes sin sustituir este alcance ni los estados del JSON.
