# DDA: patrones mínimos y alcance de la evidencia

## Cómo usar esta evidencia

Todas las fuentes y localizaciones siguientes proceden de extractos NotebookLM aportados por el usuario. No se inspeccionaron independientemente los PPTX ni los videos originales. «Enseñado/aplicado» significa reportado en esos extractos, no corroborado directamente. Un resumen sin localización precisa se identifica como tal.

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

## Aplicación mínima, no arquitectura adicional

- Sigue el modo Blazor configurado; separa páginas con ruta, componentes visuales y servicio CRUD. Registra servicios mediante DI; no construyas un servicio en Razor. Justifica un singleton que comparta estado entre usuarios.
- Usa LINQ y comprueba `FirstOrDefault`/`null`. Copia el objeto de edición si cancelar debe preservar el original; confirma cambios explícitamente.
- Para formularios, enlaza entradas con `@bind-Value` y maneja envío válido. Evita ejecutar el método al renderizar en lugar de referenciarlo como manejador.
- En API, conserva async y resultados/status explícitos. Un `try/catch` enseñado no exige capturar todo ni ocultar errores; el manejo concreto se decide según el contrato.
- Selecciona el proveedor MySQL según paquetes y versiones reales compatibles. Los extractos no autorizan imponer automáticamente una variante ni instalarla.

## Lo que los extractos no demuestran

DTO, AutoMapper, interfaces de repositorio, autenticación y relaciones EF complejas **no tienen evidencia docente demostrada** en lo aportado. DTO y autorización son requisitos del proyecto, no opcionales; mapping manual y clases concretas pueden ser propuestas mínimas, no decisiones ya aprobadas. QR, pagos y concurrencia también necesitan explicación y aprobación de su mecanismo cuando excedan los ejemplos. No introduzcas abstracciones o paquetes por comodidad.

Las advertencias/sugerencias de una fuente no constituyen prohibiciones universales. Ante conflicto con la consigna o configuración vigente, informa ambas fuentes y sigue la autoridad del proyecto, sin modificar estas reglas sin aprobación.
