# Feature: Alinear el alcance básico de geolocalización del MVP

## Objetivo

Actualizar la definición del MVP y el backlog compartido para incorporar la geolocalización básica ya acordada en `docs/requerimientos.md` y `docs/user-stories.md`.

## Problema y motivo

`docs/mvp.md` excluía toda geolocalización y `docs/mvp-tasks.json` no contenía una tarea para RF-008 y RF-009. Ambos documentos ahora describen la ampliación académica sin prometer seguimiento operativo real.

## Alcance y restricciones

- Modificar `docs/mvp.md` y agregar una sola tarea `MVP-009` a `docs/mvp-tasks.json`.
- Conservar intactos los identificadores, fechas de creación y estados de las tareas existentes. La nueva tarea inicia `pending` y depende de la fundación y los servicios ficticios.
- Documentar publicación desde un dispositivo o celular embarcado autorizado y asociado al servicio; consulta de última posición, actualización/desactualización, llegada aproximada y demora básica.
- Mantener parámetros académicos configurables, datos ficticios y estados sin datos/error. No especificar hardware, fórmulas, frecuencias, mapas ni garantías reales.
- No cambiar las reglas de QR ni marcar tareas del backlog como terminadas sin revisión cruzada. El usuario autorizó el commit y push documental a `main`; no publicar `.codegraph/`.
- TDD: no aplica porque el cambio es exclusivamente documental; validación mediante controles estructurales y contraste con los requisitos.
- Estrategia de entrega: `ask-on-risk`. Resultado: 61 inserciones y 9 eliminaciones en los dos documentos de alcance; este seguimiento se incluye en la unidad documental.

## Tareas

- [x] **GEO-DOC-1 — Alinear la definición del MVP**
  - Aceptación: el resultado esperado, actores, alcance, demostración, reglas, calidad, exclusiones, datos, orden de construcción y definición de terminado no contradicen RF-008/009 ni US-010..012.
  - Verificación: lectura cruzada del diff y comprobación independiente contra `docs/requerimientos.md` y `docs/user-stories.md`; corregidos los tres huecos que señaló la primera revisión.
- [x] **GEO-DOC-2 — Incorporar la tarea al backlog**
  - Aceptación: `MVP-009` sigue el esquema existente, nace `pending`, preserva todas las tareas anteriores y cubre publicación/consulta/degradación sin inventar precisión.
  - Verificación: `python -m json.tool docs/mvp-tasks.json > /dev/null` pasó; comparación programática con `HEAD:docs/mvp-tasks.json` confirmó que metadatos y las ocho tareas anteriores permanecen iguales. Comprobados ID único, dependencias, reviewer cruzado, fecha ISO con offset y campos de ciclo nulos.
- [x] **GEO-DOC-3 — Verificar consistencia final**
  - Aceptación: no quedan contradicciones sobre geolocalización básica y se registran resultados reproducibles.
  - Verificación: verificador independiente confirmó secuencia MVP-008/009, rechazo explícito de servicio inexistente y parámetros visibles/configurables; `git diff --check` pasó (solo advertencias LF→CRLF). `git diff --stat` de los documentos: 2 archivos, 61 inserciones y 9 eliminaciones.
- [x] **GEO-DOC-4 — Registrar y publicar la unidad documental**
  - Aceptación: los dos documentos y este seguimiento están en un commit convencional de la rama `docs/mvp-geolocation-alignment`, integrado a `main` y sincronizado con `origin/main`; `.codegraph/` queda fuera.
  - Evidencia: `c87c9162f5fa5d1eb07075fca92b173c39b7bfe2` (`docs: align MVP with basic bus geolocation`) incluye exactamente `docs/mvp.md`, `docs/mvp-tasks.json` y este seguimiento; `git diff --cached --check` pasó. Push a `origin/main` confirmó `3284d4a..c87c916` y `git ls-remote origin refs/heads/main` devolvió el mismo hash. Este registro de evidencia se publica en un commit documental posterior.

## Progreso y evidencia

- Trabajo documental terminado sin pruebas de aplicación: los servicios y parámetros siguen pendientes de acuerdo antes de implementar.
- Evaluación nativa de riesgo no disponible (salida vacía); se hizo comprobación independiente y lectura estructural. Tras una observación adicional, el paso 9 de la demo explicita publicaciones periódicas y contraste de su intervalo con el parámetro académico. No se inició una revisión nativa ni se afirmó su aprobación.
- Estado inicial: `main` sincronizada con `origin/main`; `.codegraph/` ya estaba sin seguimiento y no pertenece a este trabajo.
- El usuario autorizó explícitamente el commit y push documental. Rama de trabajo: `docs/mvp-geolocation-alignment`; la unidad está publicada en `origin/main` y `.codegraph/` quedó fuera. La tarea de implementación `MVP-009` continúa `pending` y sus criterios necesitan revisión cruzada de Enzo antes de pasarla a `done`.
- Próximo paso: después del registro de evidencia y sincronización final, acordar dispositivo y parámetros académicos antes de iniciar `MVP-009` (dependencias `MVP-001` y `MVP-002` primero).
