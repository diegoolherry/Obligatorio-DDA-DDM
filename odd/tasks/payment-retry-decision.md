# Documentar la decisión de reintentos de pago

## Objetivo y alcance autorizado
Registrar el acuerdo del usuario: un intento durable de compra agrupa intentos de pago conservados; el rechazo definitivo permite reintentar, pero un pago pendiente o de resultado desconocido exige resolución autoritativa antes de otro intento. La API verifica aprobación y disponibilidad antes de generar una sola compra confirmada y sus tickets de forma idempotente.

Superficies: docs/design/data-model.md, docs/mvp-tasks.json (solo evidence de MVP-010), docs/project-report.md.
No implementar código ni definir esquema físico, checkout, mapeo del proveedor, duración/política de hold o compensación por aprobación tardía. Revisión humana de Enzo pendiente. No renombrar ni reorganizar backlog; la propuesta del usuario sigue en discusión. Preservar IDs, estados, responsables, dependencias y fechas. No commit ni push sin autorización explícita.

## Tareas
- [x] PAY-DOC-1 — Registrar y armonizar la decisión en las tres superficies. Estado: done. Aceptación: reglas y límites visibles; ningún otro atributo del backlog modificado; sin afirmaciones de implementación o revisión humana.
- [x] PAY-DOC-2 — Verificar consistencia, JSON y diff. Estado: done. Aceptación: comparación con HEAD acredita que solo evidence de MVP-010 cambió en backlog, JSON válido y whitespace limpio; verificación documental independiente completada y leída por el padre.

## Evidencia
Explorador identificó secciones §2/§4/§5 del modelo, §1/§4/§6 del informe y evidence de MVP-010. Primer intento expiró; segundo completó sin writes. Git status del padre: main sincronizada con origin/main y únicamente .codegraph/ sin seguimiento antes del trabajo.
TDD no aplica: documentación pasiva sin comportamiento ejecutable; se verificará estructura y consistencia.

## Resultado del escritor
Tres superficies documentadas (+36/−18 líneas). JSON y comparación con HEAD PASS: únicamente evidence de MVP-010 difiere. git diff --check PASS y enlaces/anclas nuevos PASS. Sin implementación ni revisión humana inventadas. Mermaid y proveedor externo no comprobados. ASSESS no pudo producir riesgo por archivos sin seguimiento; resultado unassessable, exige verificador independiente. No se modificó autoridad de revisión.

## Verificación independiente
PASS. `git diff --check` no produjo salida; `json.loads` validó el backlog actual y el de `HEAD`; una comparación de objetos, restaurando únicamente `MVP-010.evidence`, confirmó que no cambió ningún otro atributo del backlog. Las reglas concordantes se observaron en `docs/design/data-model.md:163-171` y `docs/project-report.md:53`. Mermaid y el comportamiento del proveedor no se verificaron porque esta unidad es documentación conceptual.

## Ampliación autorizada: hold y aprobación tardía
El usuario confirmó que el otro orquestador terminó de editar modelo e informe. Registrar solo en docs/design/data-model.md y docs/project-report.md: hold por asiento/salida/intervalo de 5 minutos configurables desde creación en API; recarga/reintento no reinician plazo; vencimiento libera asiento sin declarar rechazo de pago. Pago pendiente/desconocido sigue bloqueando nuevo intento. Aprobación después del vencimiento no confirma compra ni emite tickets; gestionar devolución y no afirmar completada hasta confirmación autoritativa. Soporte TEST del proveedor, esquema físico, transacciones e integración siguen pendientes. Incluir escenarios de revisión para Enzo como resultados esperados, no pruebas ejecutadas.
Las restricciones previas sobre hold eran el alcance histórico de PAY-DOC-1/2 y esta ampliación las sustituye únicamente para esas dos superficies. No tocar docs/tasks.json, AGENTS.md, workflow, README, final-scope ni seguimiento del otro orquestador; preservar su migración existente.

- [x] PAY-DOC-3 — Documentar hold, pagos tardíos y escenarios de revisión. Estado: done. Aceptación: reglas coherentes en ambas superficies y referencias a pendientes históricos calificadas; sin implementación/revisión humana inventadas.
- [x] PAY-DOC-4 — Verificar la ampliación y preservar cambios previos. Estado: done. Aceptación: diff incremental limitado a ambas superficies, whitespace/enlaces locales válidos y verificación documental proporcional.

## Evidencia de PAY-DOC-3
Escritor completó modelo (+34/−18) e informe (+6/−4) respecto al contenido CURRENT previo, preservando migración. git diff --check de ambas superficies PASS; 37 referencias locales válidas. docs/tasks.json conserva SHA256 667ce4404867fb2f5bcc14a2f75e3a931f7d3b643888a9e4bdbe383d78b5978f antes/después. Primer script produjo SyntaxError antes de escribir; comandos separados posteriores pasaron. Soporte TEST, renderizado Mermaid y comportamiento runtime no comprobados. ASSESS nuevamente unassessable por archivos sin seguimiento; requiere verificación independiente.

## Evidencia de PAY-DOC-4
Verificación independiente PASS sin hallazgos bloqueantes. git diff --check de ambas superficies sin salida; 37 referencias locales sin fallos. Hash de docs/tasks.json coincide con baseline del escritor. Escenarios y acuerdos coherentes; aprobación tardía no se equipara a notificación tardía. Aislamiento incremental contra snapshot CURRENT solo acreditado por escritor: el verificador no recibió ese snapshot y no lo comprobó independientemente. Padre había repetido git diff --check con resultado PASS. No pruebas runtime, renderizado Mermaid, enlaces externos ni capacidades del proveedor verificadas.
STATUS ligado de revisión nativa retornó blocked/invalid_request: inventario untracked cambió; mutation_performed false y autoridad no evaluada. No se ejecutó recuperación, nueva lineage ni acknowledgement. No se afirma aprobación nativa.

## Siguiente paso
Documentación y verificación estructural completadas. Revisión nativa queda pendiente/bloqueada por inventario cambiado; revisar continuidad de autoridad por ruta nativa sin sustituir alcance con migración ajena. Enzo debe revisar siete escenarios y decisiones abiertas; soporte de devolución TEST y diseño físico siguen pendientes. La verificación independiente de reintentos pasó; la revisión nativa review-8957b34d83a22d5b fue iniciada sobre un candidato anterior y sigue abierta sin acknowledgement observado por esta sesión. No reutilizar esa autoridad para aprobar el candidato ampliado o la migración ajena. Commits/push no autorizados ni realizados. La migración de backlog pertenece al otro orquestador.
