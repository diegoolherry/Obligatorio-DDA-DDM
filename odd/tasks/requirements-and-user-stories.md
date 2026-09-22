# Feature: Requisitos e historias de usuario del MVP

## Objetivo

Documentar los requisitos funcionales y no funcionales del MVP académico y relacionar cada requisito funcional con historias de usuario verificables.

## Problema y motivo

La devolución docente confirmó la validación de boletos y abonos mediante QR y propuso incorporar ubicación del ómnibus, estimación de llegada y aviso de retrasos. El alcance actual ya contempla la validación, pero declara la geolocalización fuera del MVP; se necesita una especificación trazable que incorpore una primera versión básica y reserve mejoras avanzadas para el futuro.

## Alcance

- Crear `docs/requerimientos.md` con actores, supuestos, requisitos funcionales, requisitos no funcionales, reglas relacionadas y trazabilidad.
- Crear `docs/user-stories.md` con una o más historias por requisito funcional y criterios de aceptación en formato Dado/Cuando/Entonces.
- Mantener la denominación existente del actor `Cobrador`.
- Incluir en el MVP una geolocalización básica: publicación periódica, consulta de última posición, estimación aproximada y estado básico de retraso.
- Documentar como evolución futura la predicción avanzada, optimización y mayor precisión operativa.
- No modificar `docs/mvp.md` ni `docs/mvp-tasks.json` en este trabajo.

## Restricciones

- Proyecto académico inspirado en Berrutti; no representa un sistema oficial.
- No inventar reglas operativas reales, frecuencias definitivas ni garantías de precisión sin evidencia.
- Mantener consistencia con `docs/mvp.md` y señalar explícitamente la ampliación acordada.
- Escribir documentación técnica en español profesional según la convención existente.
- Seguir `skills/ctc-project-conventions/SKILL.md`, especialmente flujos, conectividad y estados de interfaz.
- TDD: no aplica; trabajo exclusivamente documental.
- Estrategia de entrega: `ask-on-risk`.
- Pronóstico: aproximadamente 350 líneas escritas, por debajo de la guía de división de 400 líneas.
- No crear commits sin autorización explícita del usuario.

## Tareas

- [x] **REQ-1 — Especificar requisitos funcionales y no funcionales**
  - Crear `docs/requerimientos.md` con identificadores estables, prioridades, resultados observables y restricciones medibles.
  - Aceptación: cada requisito es inequívoco, verificable y distingue alcance actual de evolución futura.
  - Corrección resuelta: el publicador es un dispositivo/celular embarcado y `RF-007` enumera los cambios administrativos cubiertos por la auditoría básica.
  - Verificación: estructura, identificadores, métricas, actores y coherencia revisados contra `docs/mvp.md`.

- [x] **REQ-2 — Redactar historias de usuario trazables**
  - Crear `docs/user-stories.md` y relacionar cada historia con un requisito funcional.
  - Aceptación: todos los requisitos funcionales tienen al menos una historia y criterios Dado/Cuando/Entonces que contemplan camino feliz y errores relevantes.
  - Corrección resuelta: `US-010` usa el dispositivo embarcado como actor y `US-009` delimita las operaciones auditadas.
  - Verificación: 9 requisitos funcionales, 12 historias y ninguna referencia faltante o desconocida.

- [x] **REQ-3 — Verificar consistencia documental**
  - Validar enlaces, formato Markdown, ausencia de duplicados y contradicciones no señaladas.
  - Aceptación: ambos documentos pueden revisarse sin reconstruir decisiones desde otras fuentes.
  - Verificación: lectura estructural independiente, control de espacios finales y comprobación automatizada de IDs.

- [ ] **REQ-4 — Publicar la unidad documental en `main`** *(en progreso)*
  - Crear un único commit convencional con los requisitos, historias y su seguimiento, y publicarlo en `origin/main` por solicitud explícita del usuario.
  - Aceptación: el commit queda verificado y `main` coincide con `origin/main` después del push.
  - Verificación: controles documentales, revisión nativa aplicable, identidad del commit y estado Git final.

## Progreso y evidencia

- Rama actual: `main`.
- Tarea actual: `REQ-4` en progreso por autorización explícita del usuario para publicar en `main`.
- Commits de unidades de trabajo: pendiente de creación.
- Líneas escritas acumuladas: 371 líneas nuevas sin commit, incluidos los 2 documentos y este tracker.
- Evidencia inicial: la validación QR ya existe en `docs/mvp.md` y en `MVP-005`/`MVP-006`; la geolocalización figura fuera del MVP y se incorpora aquí como ampliación básica expresamente elegida por el usuario.
- Verificación independiente final: sin defectos ni regresiones; 9 requisitos funcionales y 12 historias con cobertura completa; el dispositivo embarcado y las operaciones auditables quedaron explícitos; contratos QR preservados.
- Comprobaciones finales: ausencia de espacios finales y cobertura `RF-*`/`US-*` confirmadas por comandos Python; los documentos fueron leídos estructuralmente.
- Próximo paso: crear el commit documental, completar la revisión aplicable y publicar en `origin/main`; luego actualizar `docs/mvp.md` y `docs/mvp-tasks.json` en un trabajo separado.
