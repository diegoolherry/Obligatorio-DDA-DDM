# PUB06 — Reubicación documental y reparación de enlaces

Este corte organiza siete documentos según Diataxis sin cambiar su contenido
académico, sus reglas de dominio ni los resultados de las fundaciones.

## Límite de publicación

- Base: `861e295`, fundación Web del PR #21.
- Referencia de colaboración: Refs #16; tipo `type:docs`.
- Unidad única: siete traslados y reparación de navegación vigente.
- Los trabajos locales DD01–DD03 no acreditan publicación de esta unidad.
- Commit, push y PR permanecen pendientes a cargo del coordinador; sin merge.

## Destinos y excepciones canónicas

| Grupo | Documentos |
| --- | --- |
| Explicación | [MVP](../../docs/explanation/mvp.md), [modelo](../../docs/explanation/data-model.md), [entidades](../../docs/explanation/entities-explained.md), [UX](../../docs/explanation/ux-design.md) |
| Referencia | [requisitos](../../docs/reference/requirements.md), [historias](../../docs/reference/user-stories.md), [alcance final](../../docs/reference/final-scope.md) |
| Excepciones | [backlog](../../docs/tasks.json) e [informe](../../docs/project-report.md) permanecen en `docs/`. |

## Protección del contenido

Los destinos derivan de la base actual, no de documentación final de otro corte.
Solo se ajustan enlaces relativos, rutas canónicas vigentes y el árbol del README.
Se preservan encabezados, fragmentos, Mermaid, propuestas y límites académicos.
El backlog cambia únicamente dos cadenas de metadata; su array queda intacto.
Los nombres históricos de tareas y evidencias no son navegación nueva.
AGENTS, skills, workflow, fuentes Mobile/API/Web y vendor no se modifican.
La excepción previa de EOF de NavMenu no se amplía; el original queda intacto.

## Comprobaciones actuales y entrega pendiente

El control estructural compara los bytes de origen deshaciendo las sustituciones.
La comprobación JSON conserva objetos, estados, timestamps e historia del backlog.
El control local de enlaces y fragmentos no consulta recursos externos.
Whitespace y cinco firmas de privacidad se revisan sobre los candidatos explícitos.
La verificación independiente terminó con exit 0: siete traslados (1249 líneas),
31 sustituciones de rutas autorizadas y todo el contenido restante exacto frente a HEAD;
11 JSON válidos, 19 tareas intactas y únicamente dos cadenas de metadata modificadas.
Los 170 enlaces locales y 56 fragmentos pasan; 21 enlaces externos no se consultaron.
Whitespace y cinco firmas limitadas de privacidad sobre 12 candidatos: sin hallazgos.
Se corroboraron 85 archivos protegidos contra HEAD y 83 raw contra el PRE anterior;
las dos excepciones del PRE son el recibo Web histórico y el EOF NavMenu autorizado.
Ocho archivos Mobile equivalen al original solo normalizando CRLF; los otros seis
son raw iguales. API y Web conservan los bytes originales salvo el EOF ya autorizado.
El CSS coincide raw con el original; LICENSE coincide con HEAD y su hash conocido.
No se recuperó el inventario raw inicial completo de 150/134 archivos: no se afirma
preservación raw global del original ni de todos los outputs ignorados.
No corresponde RED funcional: son traslados pasivos, sin frontera ejecutable.
No se ejecutan builds, servidores, navegador ni pruebas de dominio o proveedor.

## Próximo corte y revisión independiente

Las cuatro guías y su navegación pertenecen a un hijo posterior, no a este corte.
ASSESS devolvió `unassessable` con RDD desactivado para este clon por autorización:
se aplicó su plan conservador y la verificación independiente descrita arriba pasó.
Esto no equivale a aprobación nativa; el switch global permanece activo.
La revisión nativa, CI, revisión humana y entrega Git no se presumen aprobadas.
No se completan tareas académicas ni se atribuye aprobación de ADR o de dominio.
