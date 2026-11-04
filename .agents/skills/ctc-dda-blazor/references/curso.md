# DDA: patrones mínimos y alcance de la evidencia

## Cómo usar esta evidencia

Las fuentes de presentaciones y videos siguientes proceden de extractos NotebookLM aportados por el usuario. No se inspeccionaron independientemente los PPTX ni los videos originales. Para esas fuentes, «enseñado/aplicado» significa reportado en esos extractos, no corroborado directamente. Un resumen sin localización precisa se identifica como tal. Como fuente adicional, se inspeccionó directamente el código de Maquinaria-Mobile-API, aportado por el usuario como ejemplo del profesor; su atribución docente es reportada, y su versión queda fijada en la referencia enlazada.

Clasifica cada decisión: **enseñado/aplicado**, **solo mencionado**, **exigido por proyecto sin evidencia docente** o **no verificado**. Antes de implementar algo dependiente de las dos últimas categorías, explica al equipo la opción más simple, su responsabilidad, riesgos y alternativa; solicita aprobación explícita. Para algo solo mencionado, no inventes cobertura práctica: si necesita implementación no demostrada, aplica la misma consulta. No reemplaces DTO, autenticación, QR o pagos reales requeridos por simulaciones para evitar esa puerta.

## Mapa de fuentes y decisiones

| Fuente aportada / localización | Evidencia reportada y límite operativo |
| --- | --- |
| `Semana 01 — Presentación lunes — Introducción a Blazor(1).pptx`, diap. 6/7 | Plantilla y CLI .NET 10. Se reporta BlazorWebApp con InteractiveServer/global como configuración del curso; verifica la configuración real. No afirma que .NET 8 sea intrínsecamente defectuoso. |
| `01_Presentación — Binding, eventos y formularios.pptx`, semana 3, diap. 11/12 | Referencias a métodos frente a llamadas: usa `@onclick="Guardar"`; lambda si necesita argumentos. |
| Mismo archivo, diap. 20 | No dupliques manejadores del **mismo evento**. `@bind` usa normalmente `onchange`; no es automáticamente incompatible con `@oninput`. Revisa el evento elegido, incluido `@bind:event`. |
| Mismo archivo, diap. 29/30 | Copia objetos para evitar aliasing al agregar/editar. Reasignar el formulario con `new` no muta por sí solo el objeto ya guardado; el riesgo es compartir y modificar la misma instancia. |
| Resumen aportado sobre formularios, sin diapositivas precisas | Ejemplos de `EditForm`, DataAnnotations, `DataAnnotationsValidator`, `ValidationMessage` y `OnValidSubmit`. Usa modelo tipado y validación visible; no atribuyas una localización inexistente. |
| `01_Presentación — CRUD local y Services en Blazor.pptx`, semana 4, diap. 13/32 | Servicio con `List<T>`, DI y LINQ. ID local: colección vacía → 1; si no, `Max + 1`, nunca `Count + 1`. No es estrategia de DB ni garantía concurrente. No elimines elementos durante `foreach` de la misma `List<T>`. |
| `01_Presentación — Rutas con parámetros y EventCallback .pptx`, semana 5, diap. 9/11/12 | Usa `OnParametersSet` para reaccionar a parámetros URL; maneja resultados `null`. |
| Mismo archivo, diap. 25/33 | Tarjetas visuales hijas reciben parámetros y emiten `EventCallback`; el padre decide llamadas al servicio en ese patrón. No es prohibición universal de DI en todo hijo. |
| `GMT20260825-174711_Recording_1920x1200.mp4`, aprox. 04:24–05:30 | Requisito .NET 10 del curso, reportado por el usuario; contrasta consigna y proyecto. |
| Mismo video, aprox. 00:40–02:15 | MySQL; credenciales `root/root` son demo local, no política de secretos ni configuración para la aplicación. |
| Mismo video, aprox. 43:20–44:10 | `SaveChangesAsync` persiste cambios rastreados por el contexto; no implica que toda modificación arbitraria quede rastreada. |
| `GMT20260826-133657_Recording_1920x1200.mp4`, aprox. 12:30–13:40 | `AddScoped` con servicio/repositorio concreto. No deduzcas interfaces o repositorios genéricos obligatorios. |
| Resumen de API aportado, sin localización precisa | Controller → Service → Repository → DbContext, async, `ActionResult`, estados HTTP y `try/catch` reportados como enseñados. No atribuyas todos estos conceptos a un minuto específico. |
| [Maquinaria-Mobile-API, commit y fragmentos](prefixed-string-ids.md): `EquipmentController.Post`, `Equipment.Id`, `EquipmentInput` | Código inspeccionado directamente; ejemplo docente según el usuario. ID real `string`, prefijo `eq-`, extracción con `Substring(3)`, `int.Parse` y `DefaultIfEmpty(0).Max() + 1` sobre IDs consultados por EF. DTO de entrada con DataAnnotations y sin ID enviado por el cliente. No demuestra asignación concurrente segura, no reutilización histórica ni contratos DTO de respuesta. |

## Excepciones: ejemplo Equipment aportado

**Fuente:** fragmentos completos de `EquipmentController` y `EquipmentsService` pegados por el usuario en esta conversación y atribuidos por él a la última clase. Se inspeccionó el texto aportado, no un repositorio de esa versión; no hay commit, fecha de clase ni minuto verificables. No confundirlo con la versión enlazada de Maquinaria-Mobile-API. El usuario autorizó incorporar este criterio a la skill; eso no autoriza modificar automáticamente contratos de la API.

### Enseñado/aplicado en el fragmento

- El Controller recibe `IEquipmentService` por constructor. El Service recibe `IEquipmentRepository`; las implementaciones de esa interfaz de repositorio no están incluidas.
- El Service valida ID, DTO y datos antes de persistir. Usa `throw` para interrumpir la operación cuando no puede cumplirla; no devuelve éxito después del rechazo.
- El Controller envuelve la llamada asíncrona al Service en `try` y traduce excepciones específicas antes de `catch (Exception)`.

| Caso del ejemplo | Excepción del Service | Respuesta del Controller |
| --- | --- | --- |
| ID obligatorio, DTO ausente o campos vacíos | `ArgumentException` | `400 BadRequest(ex.Message)` |
| Equipamiento no encontrado | `KeyNotFoundException` | `404 NotFound(ex.Message)` |
| Código de equipamiento repetido al crear/actualizar | `InvalidOperationException` | `409 Conflict(ex.Message)` |
| Error inesperado | `Exception`, capturada al final | `500`, `"Error interno del servidor"` |

El ejemplo también muestra DTOs de entrada/salida usados por las firmas y mapeo manual `ToReadDto`/`ToEntity`, `Trim` y normalización del código de equipamiento. No muestra logging, `finally`, autenticación, transacciones ni control concurrente del código repetido. Las comprobaciones previas no demuestran ausencia de carreras.

### Aplicación al proyecto: salvaguardas, no contenido docente adicional

- Conserva Controller → Service → Repository: reglas de negocio en Service, acceso a datos en Repository y traducción HTTP en Controller. No añadas capturas que oculten un fallo y permitan continuar como si hubiera éxito.
- Mapea por significado, no solo por tipo: una `InvalidOperationException` por inconsistencia interna no es el conflicto de código duplicado del ejemplo. No devuelvas `ex.Message` de errores internos, proveedores o credenciales; solo mensajes deliberadamente seguros de errores esperados.
- Conserva validación MVC/DataAnnotations y contratos existentes. No reemplaces los `401` de credenciales/sesiones inválidas ni los `403` de permisos insuficientes por `400`, `404` o `409`; tampoco normalices credenciales por imitar el `Trim` del equipamiento.
- Para propuestas nuevas, distingue `500` inesperado de `503` por indisponibilidad reconocida. Si el contrato vigente difiere, documenta el cambio propuesto y solicita decisión antes de alterar respuestas consumidas. Un `catch` general no demuestra que toda excepción sea indisponibilidad.
- Usa `finally` solo cuando haya limpieza necesaria. Respeta `using`/`await using` y recursos administrados por DI; no cierres el DbContext desde el Controller. El fragmento no exige añadir bloques vacíos ni demuestra ese manejo de recursos.
- Si hace falta capturar para limpiar y propagar, usa `throw;` para conservar el origen. Esto es una salvaguarda técnica, no una práctica demostrada por el fragmento.
- Antes de implementar un cambio de errores, prueba por la interfaz pública los casos aplicables: estado HTTP, cuerpo y encabezados contractuales, ausencia de datos sensibles y ausencia de cambios persistidos ante rechazos. No presentes estas pruebas como ejecutadas ni agregues casos CRUD a endpoints que no existen.

## Aplicación mínima, no arquitectura adicional

- Sigue el modo Blazor configurado; separa páginas con ruta, componentes visuales y servicio CRUD. Registra servicios mediante DI; no construyas un servicio en Razor. Justifica un singleton que comparta estado entre usuarios.
- Usa LINQ y comprueba `FirstOrDefault`/`null`. Copia el objeto de edición si cancelar debe preservar el original; confirma cambios explícitamente.
- Para formularios, enlaza entradas con `@bind-Value` y maneja envío válido. Evita ejecutar el método al renderizar en lugar de referenciarlo como manejador.
- En API, conserva async y resultados/status explícitos. Un `try/catch` enseñado no exige capturar todo ni ocultar errores; el manejo concreto se decide según el contrato.
- Selecciona el proveedor MySQL según paquetes y versiones reales compatibles. Los extractos no autorizan imponer automáticamente una variante ni instalarla.

## Lo que los extractos no demuestran

Los extractos NotebookLM no demuestran DTO; la fuente [Maquinaria-Mobile-API](prefixed-string-ids.md) muestra un DTO de entrada simple con DataAnnotations. El fragmento Equipment aportado después sí muestra uso de DTOs de entrada/salida, mapeo manual e inyección mediante interfaces específicas de Service y Repository; no incluye las definiciones de esas interfaces ni la implementación del Repository. No extiendas esa evidencia a repositorios genéricos, implementaciones de persistencia o mapping automático. AutoMapper, autenticación y relaciones EF complejas **no tienen evidencia docente demostrada** en estas fuentes. DTO y autorización siguen siendo requisitos del proyecto, no opcionales; las extensiones no demostradas requieren la consulta indicada arriba. QR, pagos y concurrencia también necesitan explicación y aprobación de su mecanismo cuando excedan los ejemplos. No introduzcas abstracciones o paquetes por comodidad.

Las advertencias/sugerencias de una fuente no constituyen prohibiciones universales. Ante conflicto con la consigna o configuración vigente, informa ambas fuentes y sigue la autoridad del proyecto, sin modificar estas reglas sin aprobación.
