# Guía del repositorio

## Skill del proyecto

Antes de planificar, implementar, depurar o revisar trabajo de Blazor, C#, API, React Native, TypeScript, Expo o UX móvil, cargá y seguí:

`skills/ctc-project-conventions/SKILL.md`

La consigna vigente y la configuración del repositorio prevalecen sobre la skill si hay conflicto. Informalo en lugar de asumir una solución, y actualizá la skill solo cuando el equipo apruebe la nueva convención.

## Backlog compartido del proyecto completo

`docs/tasks.json` es la fuente de verdad del backlog del proyecto académico completo, incluidos el MVP y la entrega final. Mantiene un único array `tasks`; cada tarea usa un array `area` con etiquetas `API`, `Web`, `Mobile` y/o `Docs`. Las tareas transversales incluyen varias áreas, sin dividirse por capa. Los IDs legados `MVP-*` y `FIN-*` se conservan estables: no se renumeran al cambiar el alcance o la clasificación. Debe mantenerse como JSON válido y sus tareas deben conservar un `id` y `createdAt` inmutables.

- Usá únicamente los estados `pending`, `in_progress`, `blocked` y `done`. Las transiciones válidas son `pending` → `in_progress` o `blocked`; `in_progress` → `blocked` o `done`; y `blocked` → `pending` o `in_progress` al resolver el impedimento. Una tarea `done` no se reabre: creá una tarea nueva si surge trabajo adicional.
- Antes de iniciar una tarea, comprobá que sus dependencias estén en `done`; cualquier excepción debe quedar justificada como bloqueo o decisión explícita en `evidence`.
- Al pasar por primera vez a `in_progress`, registrá `startedAt` con la hora actual del sistema en ISO 8601 y offset UTC explícito. No modifiques ese valor después.
- Al pasar a `done`, verificá todos los `acceptanceCriteria`, registrá en `evidence` los comandos, resultados o enlaces que lo demuestren y completá `completedAt` y `completedBy`. Antes de completarla, estos campos permanecen en `null` y no se inventa evidencia de finalización.
- Al bloquear una tarea, conservá el motivo concreto y qué la desbloquea en `evidence`; no marques trabajo incompleto como `done`.
- Obtené cada timestamp de la hora actual del sistema con un offset UTC explícito; no fabriques fechas, horas ni los separes en campos distintos.
- El `reviewer` debe ser la otra persona: Diego revisa las tareas de Enzo y Enzo revisa las tareas de Diego. La revisión cruzada debe confirmar los criterios de aceptación antes de marcar una tarea como `done`.

## Informe académico vivo

`docs/project-report.md` es la fuente Markdown del informe académico; la versión Word se genera después, no se mantiene como fuente paralela. Después de cambios importantes en arquitectura, requisitos o alcance, decisiones tecnológicas, cortes MVP terminados, evidencia de pruebas/calidad, despliegue/infraestructura, riesgos, cronograma o proceso del equipo, revisá el informe y actualizá las secciones afectadas. Enlazá evidencia verificable, distinguí decisiones y planes de resultados comprobados, marcá lo pendiente explícitamente y nunca inventes implementación, pruebas o despliegue completados. Conservá `docs/tasks.json` como autoridad para estados y finalización de tareas.
