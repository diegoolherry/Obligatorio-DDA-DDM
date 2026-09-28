# README del MVP

## Objetivo

Explicar a quienes llegan al repositorio qué proyecto académico se está diseñando, qué archivos y carpetas existen hoy y qué componentes/carpetas se proponen para el MVP terminado, sin afirmar implementación inexistente.

## Tareas

- [x] Inspeccionar el árbol rastreado y fuentes de requisitos, backlog, diseño e informe; distinguir estado actual de propuesta.
- [x] Escribir README.md en español con entrada rápida, estructura actual, estructura objetivo explícitamente ilustrativa y enlaces a fuentes canónicas.
- [x] Verificar contenido, enlaces y estado Git; registrar evidencia y entregar en main según pedido del usuario. Verificación independiente: JSON válido, 10 enlaces existentes y sin espacios finales; revisión nativa de bajo riesgo aprobada y reconocida (`review-d1f0c0a736f42e25`). Commit `3b24b5b2120113b56dcc55adc15f3db3b2ac5203` entregado por fast-forward y confirmado igual a `origin/main`. No se probaron aplicaciones inexistentes.

## Alcance y ruta

Superficies autorizadas: README.md y este registro ODD. Rama de unidad `docs/mvp-readme`; entrega pedida directamente a main, sin PR ni push de la rama. Forecast ~120–180 líneas redactadas; una unidad coherente y una revisión enfocada. Escritura delegada por preparación documental; verificación independiente según evaluación nativa. TDD: N/A para documentación, sin frontera de ejecución de aplicación. Verificaciones propuestas: enlaces Markdown locales, JSON del backlog, `git diff --check`, lectura de estructura rastreada y estado de repo. El README no reemplaza `docs/mvp-tasks.json` ni `docs/project-report.md`. Límite de reversión: este README y su registro, sin tocar `.codegraph/` ni código de producto. Verificación de escritura (rama `docs/mvp-readme`): `git diff --check` sin errores; `python -m json.tool docs/mvp-tasks.json > /dev/null` exitoso; script Python de solo lectura: 10 enlaces relativos existentes, 9 `pending`, 1 `in_progress`, MVP-010 con revisor Enzo, sin código de producto rastreado ni credenciales/rutas absolutas detectadas; `git status --short --branch` muestra README y este registro sin rastrear, junto con `.codegraph/` preexistente ajeno al cambio. Supuestos: estructura objetivo ilustrativa y nombres no fijados; estado del backlog es una instantánea. Límite de reversión: README.md y este registro, sin tocar documentación canónica ni archivos locales ajenos. Evidencia de commit/entrega: `3b24b5b2120113b56dcc55adc15f3db3b2ac5203` en `main` y `origin/main`; esta nota de cierre se entrega en un segundo commit documental.
