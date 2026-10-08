# Guía del repositorio

## Idioma de colaboración en GitHub

- Escribí en español los títulos y las descripciones de issues y pull requests, incluidos sus encabezados, criterios de aceptación, resúmenes y planes de verificación.
- Escribí en español los comentarios de colaboración y revisión de este proyecto.
- Esta convención reemplaza el inglés por defecto del agente para esos textos. Aplicala también al delegar su redacción a subagentes.
- Conservá sin traducir identificadores, nombres de archivos, comandos, etiquetas como `type:docs` y `status:approved`, y referencias de GitHub como `Closes #9` o `Refs #9`.
- Esta regla no cambia las convenciones del código ni de los mensajes de commit. Tampoco autoriza traducir o editar publicaciones existentes: eso requiere un pedido explícito.

## Skill del proyecto

Antes de planificar, implementar, depurar o revisar trabajo de Blazor, C#, API, React Native, TypeScript, Expo o UX móvil, cargá y seguí:

- `.agents/skills/ctc-project-conventions/SKILL.md` para el flujo compartido y UX móvil.
- `.agents/skills/ctc-dda-blazor/SKILL.md` para Blazor, C# y API.
- `.agents/skills/ctc-ddm-mobile/SKILL.md` para React Native, TypeScript, Expo y NativeWind/Tailwind.

Cargá siempre la compartida y la especializada pertinente; cargá ambas especializadas si el trabajo cruza capas. Las especializadas son las fuentes canónicas de reglas por capa; las referencias antiguas quedan como enlaces de compatibilidad. Consultá [uso en Pi y Codex](.agents/skills/ctc-dda-blazor/references/uso-hosts.md) sin crear copias ni modificar settings.

La consigna vigente y la configuración del repositorio prevalecen sobre la skill si hay conflicto. Informalo en lugar de asumir una solución, y actualizá la skill solo cuando el equipo apruebe la nueva convención.

## Skill opcional de aprendizaje

Solo ante un pedido explícito de mentoría socrática o aprendizaje guiado, cargá [socratic-code-mentor](.agents/skills/socratic-code-mentor/SKILL.md). No la actives por contexto académico, pedidos ordinarios de funcionalidades ni «solo arreglalo». El modo no autoriza ediciones ni reemplaza las skills obligatorias, el backlog completo, las pruebas de invariantes o la revisión cruzada; un pedido explícito de solución permite ayuda directa.

## Backlog compartido del proyecto completo

`docs/tasks.json` es la fuente de verdad del backlog del proyecto académico completo, incluidos el MVP y la entrega final. Mantiene un único array `tasks`; cada tarea usa un array `area` con etiquetas `API`, `Web`, `Mobile` y/o `Docs`. Las tareas transversales incluyen varias áreas, sin dividirse por capa. Los IDs legados `MVP-*` y `FIN-*` se conservan estables: no se renumeran al cambiar el alcance o la clasificación. Debe mantenerse como JSON válido y sus tareas deben conservar un `id` y `createdAt` inmutables.

- Para descomponer una tarea, agregá hijos al mismo array `tasks` con IDs nuevos estables y el campo opcional `parentId` apuntando al ID original. Derivá la pertenencia de los hijos desde `parentId`; no mantengas una lista duplicada en el padre. Conservá los registros originales y su orden.
- Los originales que tienen hijos pasan a ser hitos de coordinación, sin perder sus criterios de aceptación: los criterios originales del padre siguen siendo autoritativos. No hay agregación automática de estados ni conteo de hijos como funcionalidades independientes completadas.
- El padre solo puede comenzar después de cumplir su puerta de dependencias original; tener hijos no la elimina. Cada hijo solo puede comenzar con sus propias dependencias en `done`, aunque el padre esté en curso. No agregues dependencias del padre hacia sus hijos ni de los hijos hacia el padre; `parentId` es jerarquía, no dependencia de ejecución.
- Para completar un padre, verificá explícitamente que todos sus hijos derivados estén en `done` y que se cumplan todos los criterios originales del padre, con evidencia y revisión cruzada. Completar hijos no cambia automáticamente el estado del padre.
- Usá únicamente los estados `pending`, `in_progress`, `blocked` y `done`. Las transiciones válidas son `pending` → `in_progress` o `blocked`; `in_progress` → `blocked` o `done`; y `blocked` → `pending` o `in_progress` al resolver el impedimento. Una tarea `done` no se reabre: creá una tarea nueva si surge trabajo adicional.
- Antes de iniciar una tarea, comprobá que sus dependencias estén en `done`; cualquier excepción debe quedar justificada como bloqueo o decisión explícita en `evidence`.
- Al pasar por primera vez a `in_progress`, registrá `startedAt` con la hora actual del sistema en ISO 8601 y offset UTC explícito. No modifiques ese valor después.
- Al pasar a `done`, verificá todos los `acceptanceCriteria`, registrá en `evidence` los comandos, resultados o enlaces que lo demuestren y completá `completedAt` y `completedBy`. Antes de completarla, estos campos permanecen en `null` y no se inventa evidencia de finalización.
- Al bloquear una tarea, conservá el motivo concreto y qué la desbloquea en `evidence`; no marques trabajo incompleto como `done`.
- Obtené cada timestamp de la hora actual del sistema con un offset UTC explícito; no fabriques fechas, horas ni los separes en campos distintos.
- El `reviewer` debe ser la otra persona: Diego revisa las tareas de Enzo y Enzo revisa las tareas de Diego. La revisión cruzada debe confirmar los criterios de aceptación antes de marcar una tarea como `done`.

## Informe académico vivo

`docs/project-report.md` es la fuente Markdown del informe académico; la versión Word se genera después, no se mantiene como fuente paralela. Después de cambios importantes en arquitectura, requisitos o alcance, decisiones tecnológicas, cortes MVP terminados, evidencia de pruebas/calidad, despliegue/infraestructura, riesgos, cronograma o proceso del equipo, revisá el informe y actualizá las secciones afectadas. Enlazá evidencia verificable, distinguí decisiones y planes de resultados comprobados, marcá lo pendiente explícitamente y nunca inventes implementación, pruebas o despliegue completados. Conservá `docs/tasks.json` como autoridad para estados y finalización de tareas.

## Navegación documental y uso por agentes

La [entrada del repositorio](README.md) organiza la lectura por propósito:

- **Tutoriales:** [primer arranque local](docs/tutorials/first-local-run.md), para aprender ejecutando el scaffold.
- **Guías prácticas:** [cliente/API](docs/how-to/client-api-configuration.md) y [problemas conocidos](docs/how-to/troubleshooting.md), para resolver una tarea concreta.
- **Referencia:** [requisitos](docs/reference/requirements.md), [historias](docs/reference/user-stories.md), [alcance final](docs/reference/final-scope.md) y [API implementada](docs/reference/api.md), para consultar contratos y límites.
- **Explicaciones:** [MVP](docs/explanation/mvp.md), [modelo](docs/explanation/data-model.md), [entidades](docs/explanation/entities-explained.md) y [UX](docs/explanation/ux-design.md), para entender contexto y decisiones.

Excepciones canónicas: [backlog](docs/tasks.json) e [informe académico](docs/project-report.md) conservan sus rutas y autoridad definidas arriba; no crees copias de estados ni una documentación paralela solo para agentes.

Antes de editar arranque/configuración, leé el tutorial, la guía pertinente y la configuración real. Antes de modificar contratos o dominio, leé las referencias, las explicaciones afectadas y los criterios/dependencias del backlog. Antes de actualizar evidencia o decisiones, leé el informe y su fuente verificable; mantené las skills obligatorias de arriba.

Conservá las restricciones no inferibles y las salvaguardas existentes. Registrá decisiones y justificaciones solo con respaldo humano o evidencia identificable; no inventes aprobación, alternativas descartadas ni razones para rellenar documentos. Distinguí comportamiento establecido por fuente, comprobación observada, reporte del usuario y plan pendiente. La documentación no sustituye las pruebas que protegen invariantes de autorización, pagos, QR, abonos o concurrencia; un texto o criterio de aceptación no acredita ejecución exitosa.
