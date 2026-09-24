# Guía visual y de experiencia de usuario — MVP académico

> **Estado:** propuesta para revisión del equipo, anterior a la consigna oficial. Producto académico ficticio inspirado en un contexto público de transporte; no es un canal de Berrutti ni representa un servicio afiliado, sus datos u operación real.

## 1. Propósito y límites

Esta guía define decisiones **propuestas** de interfaz para el portal Blazor y la aplicación React Native del MVP descrito en [docs/mvp.md](docs/mvp.md), [docs/requerimientos.md](docs/requerimientos.md) (RF-001 a RF-009) y [docs/user-stories.md](docs/user-stories.md). Orienta pantallas, prioridades y estados; no define arquitectura, endpoints ni implementación. Ante diferencias prevalecen la consigna oficial y los requisitos vigentes.

**Objetivo de UX:** que el pasajero encuentre y presente el comprobante correcto por tramo; que el cobrador distinga una validación confirmada de cualquier rechazo o pérdida de conexión; y que el administrador configure y compruebe datos de demostración sin confundirlos con datos reales. Una pantalla debe hacer evidente su acción principal y qué ocurrió después de realizarla.

## 2. Referencias públicas: observación, no especificación

| Fuente pública | Observación acotada | Traducción propuesta para este MVP (no copia) |
| --- | --- | --- |
| [Inicio — index.html](http://www.berruttiturismo.com.uy/index.html) | Imagen de ómnibus, encabezado, navegación superior y acceso a horarios. | Priorizar una entrada clara a búsqueda y viajes; sin reutilizar fotografía, logo ni composición. |
| [Historia — historia.html](http://www.berruttiturismo.com.uy/historia.html) | Relato histórico en forma de cronología. | No crear sección histórica: no ayuda a completar la tarea principal. |
| [Servicios — servicios.html](http://www.berruttiturismo.com.uy/servicios.html) | Presenta líneas, pasajes, abonos y otros servicios. | Distinguir «Tickets» y «Abonos»; los demás servicios quedan fuera de alcance. |
| [Horarios — horarios.html](http://www.berruttiturismo.com.uy/horarios.html) | Horarios publicados como imagen. | Mostrar itinerarios ficticios como información legible y adaptable, no transcribir horarios reales ni depender de una imagen. |
| [Presupuesto — presupuesto.html](http://www.berruttiturismo.com.uy/presupuesto.html) | Canal para consultar presupuestos. | No incluir solicitudes de presupuesto: precio y pago simulados pertenecen al flujo académico de compra. |
| [Contacto — contacto.html](http://www.berruttiturismo.com.uy/contacto.html) | Formulario de contacto/reserva. | No copiar formulario, detalles de contacto ni prometer reservas oficiales. |

**Límite de identidad:** el logo observado combina rojo, naranja, negro y blanco; eso no autoriza su uso ni demuestra una paleta oficial. Una variante naranja en `skin.css` tampoco acredita que esté activa. No copiar marca, fotografías, textos extensos, horarios, teléfonos, direcciones ni datos de contacto. Usar rótulos y datos ficticios identificados como tales.

## 3. Lenguaje visual propuesto, sujeto a aprobación

| Token propuesto | Valor / uso previsto | Contraste de texto sobre fondo indicado |
| --- | --- | --- |
| Fondo base / superficie | `#F7F4EF` / `#FFFFFF`; paneles sin textura | — |
| Texto principal | `#14324A` sobre blanco o fondo base; títulos, cuerpo y navegación | 13,25:1 / 12,08:1 |
| Acento de acción | `#A74220` sobre blanco; enlaces y acción primaria con texto legible | 6,10:1 |
| Confirmación | `#245746` sobre blanco; mensaje de validación confirmada | 8,31:1 |
| Error | `#A5272B` sobre blanco; rechazo y aviso de fallo | 7,18:1 |

Son **tokens propuestos**, no colores oficiales: pares calculados con el algoritmo de contraste WCAG, pendientes de revisión con la identidad elegida. `#E77723` sobre blanco alcanza solo 2,97:1: **no usar para texto**. Mantener contraste de texto normal ≥4,5:1 y grande ≥3:1; verificar también texto dentro de botones, estados de foco, iconos informativos y superficies adicionales antes de aprobarlas. Color nunca reemplaza texto, icono o descripción de estado.

Tipografía: fuente de sistema legible, jerarquía de título, subtítulo, cuerpo y ayuda; evitar tamaños fijos que impidan ampliación. Espaciado base propuesto de 8 unidades (subdivisión 4), paneles con márgenes constantes y jerarquía por espacio antes que decoración. Iconos acompañan etiquetas; QR y datos críticos deben conservar suficiente tamaño y claridad al mostrarse en pantalla.

## 4. Arquitectura de información propuesta por rol

| Canal / rol | Navegación principal propuesta | Tarea dominante y alcance |
| --- | --- | --- |
| Blazor / pasajero | Buscar viaje · Mis tickets · Mis abonos · Estado del servicio · Cuenta | Buscar → revisar itinerario → pago simulado → presentar QR del tramo; consultar ubicación aproximada. |
| Blazor / administrador | Configuración · Abonos · Operaciones · Cuenta | Preparar paradas, tramos, servicios, tarifas y usuarios ficticios; asignar/renovar abonos y revisar pagos/validaciones. |
| React Native / pasajero | Buscar · Tickets · Abonos · Servicio · Cuenta | Mismas tareas del pasajero, con acceso rápido al QR del tramo adecuado. |
| React Native / cobrador | Validar · Cuenta | Elegir servicio/tramo actual, escanear y esperar respuesta remota; el resultado se presenta dentro del flujo de validación, sin proponer un historial adicional. |
| Dispositivo embarcado | Sin navegación de pasajero/cobrador definida aquí | Publicación autorizada de posiciones ficticias (RF-008); no inferir una pantalla de operación del ómnibus. |

Después del inicio de sesión, mostrar solo navegación pertinente al rol. Un acceso no autorizado debe comunicar el rechazo; ocultar opciones no constituye autorización. En móvil, usar navegación corta con etiquetas persistentes y acceso al QR sin ocultar tramo, vigencia y estado. En escritorio, navegación visible y contenido centrado de ancho legible; en pantallas angostas, apilar formulario y resultados sin perder la acción primaria.

## 5. Flujos prioritarios y decisiones visibles

1. **Buscar y comprar (US-003/004).** Origen + destino + fecha → buscar → lista de itinerarios (carga / sin resultados / error) → detalle ordenado de cada tramo, transbordo, horario ficticio, precio total y política de asiento → confirmar **pago simulado**. La demo Ombúes–Radial no asigna asiento; Radial–Colonia muestra el asignado automáticamente. Aprobado: mostrar dos tickets distintos, uno por tramo, con acceso individual al QR. Rechazado: ningún ticket emitido, explicación y reintento; fallo incierto: consultar estado antes de afirmar emisión, sin duplicar confirmaciones. Nunca solicitar tarjetas reales.
2. **Presentar y validar ticket (US-005/006).** Pasajero elige ticket identificado por tramo, servicio y estado → muestra su QR opaco, sin datos personales embebidos. Cobrador selecciona tramo actual → escanea → ve «Validando…» hasta respuesta de la API → «Ticket válido» o motivo: ya utilizado, vencido, tramo incorrecto o código inexistente. Un segundo escaneo no puede mostrarse como exitoso. Error de red o respuesta incierta: «No se pudo confirmar», ofrecer reintentar o consultar, **nunca éxito local ni validación sin conexión**.
3. **Dos abonos independientes (US-007/008).** Administrador asigna o renueva el par relacionado con igual cupo inicial → pasajero ve dos tarjetas separadas, cada una con par de paradas, vigencia, saldo propio y QR propio → presenta el del tramo actual → cobrador confirma con la API → solo el saldo correspondiente baja una unidad. Tramo incorrecto, agotado, vencido o suspendido: rechazo textual; no cambiar visualmente ninguno de los saldos sin confirmación remota. Identificar con claridad cuál de los dos abonos se está mostrando, también en el regreso.
4. **Estado del servicio (US-010/011/012; RF-008/009).** El dispositivo/celular embarcado autorizado publica posiciones del servicio de demostración; el pasajero consulta la **última posición conocida**, su fecha/hora de actualización y, solo con datos vigentes y referencias suficientes, llegada **aproximada** y estado básico de demora **aproximado** («En horario» / «Con demora»). Si excede el umbral académico, mostrar «Información desactualizada» y la última actualización; sin posición, mostrar «Sin datos»; sin referencias, «Estimación y demora no disponibles»; ante error, «No se pudo consultar» + reintentar. Nunca presentar ubicación continua garantizada ni inventar estimación.
5. **Administración (US-002/009).** Configuración ficticia → revisar campos y política de asiento → guardar con validación junto al campo → confirmación o error que conserva entradas. Para abonos, revisar pasajero, período y dos tramos antes de asignar/renovar. Operaciones permite consultar pagos simulados y validaciones con su resultado; la auditoría muestra actor y fecha de creación/actualización de paradas, tramos, servicios, tarifas y usuarios/roles ficticios, y de asignación, renovación o suspensión de abonos. Vacío y fallo son distintos. Acciones sensibles requieren confirmación explícita y respuesta remota.

## 6. Inventario de pantallas para wireframes

| Pantalla | Contenido mínimo / acción principal | Estados especiales |
| --- | --- | --- |
| Acceso (web y móvil) | Cuenta ficticia, entrada y salida por rol; iniciar sesión | Credenciales inválidas, espera, acceso denegado. |
| Búsqueda (pasajero) | Origen, destino, fecha; «Buscar viajes» | Resultados, vacío con cambio de criterios, error/reintento. |
| Itinerario y confirmación | Tramos ordenados, transbordo, asiento, horario y total ficticios; «Confirmar pago simulado» | Pendiente, rechazado, incierto, aprobado. |
| Mis tickets / detalle QR | Estado, tramo, servicio, fecha y QR individual; «Mostrar QR» | Sin tickets, utilizado, vencido, error/reintento. |
| Mis abonos / detalle QR | Dos saldos y vigencias independientes; «Mostrar QR de este tramo» | Sin asignación, agotado/suspendido, error/reintento. |
| Servicio (pasajero) | Última posición, actualización, llegada y demora aproximadas | Vigente, desactualizado, sin datos, sin estimación, error/reintento. |
| Validar (cobrador móvil) | Servicio/tramo seleccionado, lector QR; «Escanear» | Permiso de cámara denegado, espera, error de red. |
| Resultado de validación | Tipo y tramo, resultado textual dominante; «Escanear otro» | Éxito confirmado frente a rechazos específicos o estado no confirmado. |
| Configuración (administrador web) | Paradas, tramos, servicios, tarifas y usuarios ficticios; editar/guardar | Formulario inválido, carga, vacío, fallo. |
| Asignación de abonos (administrador web) | Pasajero, período, dos tramos, igual cupo inicial; confirmar | Errores por campo, confirmación o fallo sin éxito supuesto. |
| Operaciones (administrador web) | Pagos y validaciones con resultado; cambios administrativos delimitados por RF-007 con actor y fecha; consultar | Vacío, carga, error/reintento, acceso denegado. |

**Esbozo textual — pasajero móvil:** `[Tramo actual + estado] → [QR individual] → [Información de vigencia]`; abajo `[Cambiar a otro tramo]`. **Cobrador:** `[Servicio/tramo elegido] → [Escanear] → [Validando…] → [Resultado textual + siguiente acción]`. La disposición se prueba con tareas, no se traslada del HTML público.

## 7. Componentes, estados y accesibilidad

- **Componentes reutilizables propuestos:** encabezado con aviso «Demostración académica», selector de tramo, tarjeta de itinerario, ticket/abono con etiqueta de estado, panel QR, mensaje de estado remoto, botón de reintento, confirmación de operación y campo con error contextual. El contenido difiere por rol; no crear pantallas superfluas para reutilizar un componente.
- **Contrato visual remoto:** carga con rótulo y acción deshabilitada mientras se procesa; vacío con explicación y próxima acción; error con reintento cuando sea seguro; éxito solo tras respuesta confirmada. Si se interrumpe la conexión, conservar entradas, explicar lo desconocido y evitar duplicar envíos. Un dato anterior puede permanecer visible solo con su actualización y advertencia, nunca como validación vigente.
- **Móvil:** respetar áreas seguras, teclado y controles del sistema; objetivos táctiles cercanos a 48 × 48 unidades independientes de densidad cuando sea práctico, con separación y feedback de presión. Pedir permiso de cámara en contexto, explicar rechazo y brindar salida comprensible sin inventar validación manual. Desplazamiento y tamaños dinámicos no deben tapar CTA ni QR.
- **Web Blazor:** formularios con etiquetas persistentes, orden de tabulación lógico, foco visible, mensajes por campo y resumen cuando corresponda; Enter/envío predecible. En escritorio, columnas solo cuando mantengan lectura secuencial y precio/confirmación visibles; en ancho móvil, apilar controles. Estados y mensajes deben ser accesibles al teclado y a tecnologías de asistencia; no depender solo del color ni de un ícono.
- **Prueba de prototipo:** sin indicar dónde tocar, pedir a una persona que compre el itinerario de dos tramos, presente el segundo QR, rechace un ticket duplicado, use el abono del tramo correcto y distinga una ubicación vencida. Registrar en qué decisión se detiene y corregir jerarquía o rótulos antes de pulir el mockup.

## 8. Decisiones pendientes y criterios de aprobación

- **Pendiente de aprobación del equipo:** identidad propia, nombre visible, derechos de logo e imágenes, tokens definitivos y consigna oficial del curso. Hasta entonces, usar recursos originales o neutrales; no reutilizar material del sitio público.
- Confirmar con la consigna los límites de cada pantalla, parámetros académicos de publicación/desactualización y datos ficticios. No incorporar servicios, horarios, reservas ni información de contacto reales.
- Para aprobar wireframes: cada rol debe completar su tarea principal, cada QR debe tener tramo explícito, cada validación no confirmada debe ser distinguible del éxito y la ubicación desactualizada no debe aparentar tiempo real. Revisar contraste de las combinaciones finales y el recorrido con teclado/tacto antes de implementar UI.
