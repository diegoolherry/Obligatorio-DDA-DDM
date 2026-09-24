# Feature: Guía visual y UX del MVP

## Objetivo

Crear `DESIGN.md` en la raíz como guía de identidad visual y experiencia de usuario para Blazor y React Native, inspirada en las páginas públicas que señaló el usuario sin presentarse como marca o producto oficial de Berrutti.

## Problema y motivo

Los requisitos y flujos ya están documentados, pero el equipo no dispone de criterios visuales, jerarquía de pantallas ni patrones de estados reutilizables. El usuario eligió explícitamente guía visual/UX y el nombre `DESIGN.md`.

## Alcance y restricciones

- Único documento de producto nuevo: `DESIGN.md`. Este archivo de seguimiento se mantiene por el flujo ODD.
- Tomar de las seis páginas oficiales solamente evidencia contextual y referencias visuales verificables; separar observaciones de decisiones propuestas. No copiar el logo, fotografías, textos largos, horarios, contactos, CSS ni presentarse como canal oficial.
- Traducir el MVP académico a navegación por rol/canal, flujos, componentes, estados remotos, accesibilidad y paleta propuesta de contraste comprobado. Las estimaciones y precios son ficticios.
- No definir arquitectura, endpoints, fórmulas, bibliotecas, UI implementada ni funciones fuera del MVP. La consigna futura prevalece.
- Preservar los cambios previos sin commit en `docs/mvp.md`, `docs/mvp-tasks.json` y `odd/tasks/mvp-geolocation-alignment.md`; no tocar `.codegraph/`.
- TDD no aplica: documentación exclusivamente. Comprobaciones: fuentes enlazadas, consistencia con requisitos y `git diff --check`. Estrategia `ask-on-risk`; pronóstico 180–250 líneas nuevas.
- Commit local autorizado por el usuario; no crear push ni PR sin autorización explícita.

## Tareas

- [x] **DES-1 — Extraer referencias y delimitar identidad**
  - Aceptación: evidencias de las seis páginas y del MVP están trazadas y diferenciadas de propuestas; límites académicos y de marca explícitos.
  - Verificación: revisión de fuentes y de alcance de `docs/mvp.md`/`docs/requerimientos.md`.
- [x] **DES-2 — Redactar guía visual y de UX**
  - Aceptación: `DESIGN.md` cubre roles/canales, navegación, flujos clave, componentes/estados, tokens propuestos, accesibilidad y referencia a fuentes sin confundir funciones del sitio público con el MVP.
  - Verificación: documento de 88 líneas leído; enlaces a seis páginas públicas y tres fuentes internas presentes; los pares de contraste publicados fueron recalculados y coinciden.
- [x] **DES-3 — Validar consistencia y cierre documental**
  - Aceptación: enlaces, alcance, legibilidad y `git diff --check` pasan; los supuestos pendientes quedan visibles.
  - Verificación: revisión independiente encontró y se corrigieron dos omisiones (demora también aproximada y auditoría RF-007 detallada); segunda revisión sin defectos. `git diff --check` pasó para archivos versionados con advertencias LF→CRLF previas; escaneo directo del nuevo `DESIGN.md` sin tabs ni espacios finales, UTF-8 y salto final.
- [ ] **DES-4 — Registrar la unidad documental en un commit** *(en progreso)*
  - Aceptación: la unidad documental tiene un commit convencional en rama de trabajo, contiene exclusivamente `DESIGN.md` y este seguimiento, y la identidad del commit queda registrada aquí sin incluir cambios previos no relacionados.
  - Autorización: el usuario pidió expresamente crear el commit; no se presume autorización para push o PR.

## Progreso y evidencia

- Rama al inicio: `main`, con cambios documentales anteriores sin commit. No se sobrescriben.
- Fuentes: `index.html`, `historia.html`, `servicios.html`, `horarios.html`, `presupuesto.html`, `contacto.html` de `berruttiturismo.com.uy`; lectura pública realizada, incluyendo HTML de inicio y logo para observación visual. El CSS `skin.css` contiene una variante naranja `body.blue`, pero no demuestra que esa variante esté activa en el HTML de inicio; no convertirla en identidad oficial.
- El logo observado contiene rojo, naranja, negro y blanco; no se reutilizará en el producto académico.
- El sitio publica horarios como imagen y ofrece consultas de presupuesto/contacto: no implica que esas funciones ni datos reales entren en este MVP.
- DES-1 verificado: seis páginas consultadas, sitio público separado del MVP, logo observado pero no reutilizable; mapa de roles y flujos contrastado con requisitos por exploración independiente. Propuesta cromática calculada: `#14324A` sobre blanco 13.25:1; `#A74220` sobre blanco 6.10:1; `#245746` sobre blanco 8.31:1. Son colores propuestos, no tokens oficiales.
- `DESIGN.md` redactado y verificado sin implementar UI. La auditoría delimita RF-007 y la llegada/demora se rotulan aproximadas. Los enlaces externos se consultaron durante la investigación; la verificación final comprobó sintaxis y presencia de enlaces, no disponibilidad remota nueva.
- Rama de trabajo creada: `docs/visual-design-guide`. La guía y este seguimiento se preparan para un commit local autorizado; sin push ni PR. El trabajo previo en `docs/mvp.md`, `docs/mvp-tasks.json` y `odd/tasks/mvp-geolocation-alignment.md` permanece intacto.
- Próximo paso: confirmar el índice limitado a estos dos archivos y registrar la unidad documental; después conservar el hash en este seguimiento.
