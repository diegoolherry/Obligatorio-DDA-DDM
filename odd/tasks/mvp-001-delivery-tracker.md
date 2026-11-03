# MVP-001 authentication delivery tracker
Purpose: coordinate draft-only review of the existing cross-layer authentication implementation.
Authority: [issue #36](https://github.com/diegoolherry/Obligatorio-DDA-DDM/issues/36); academic states remain in `docs/tasks.json`.

## Delivery constraints

- Use nonclosing `Refs #36`; the human selected no automatic issue closure.
- Preserve the approved feature-branch-chain: first child targets this tracker, later children target their immediate predecessor.
- Open one tracker and fourteen children as drafts. No merge, force push, deployment or database operation is authorized.
- The human accepted the review cost of seven cohesive units above the advisory 400-line budget; no `size:exception` label is authorized.
- Tests/docs stay with behavior. Historical offline evidence is not browser, device or real MySQL acceptance.
- SQL administrative proof, real-role fixtures/TLS/browser/iPhone and cross-review remain pending. Do not complete academic tasks from this tracker.

## Immutable source boundaries

| Unit | Head | Review lines | Scope |
| --- | --- | --- | --- |
| 01 | `f4727dc` | 901 | Persistencia local de cuentas y contadores |
| 02 | `91e8d2c` | 87 | Contratos compatibles de sesiones opacas |
| 03 | `6daaf95` | 824 | Ingreso, sesiones opacas y autorización en API |
| 04 | `79d8975` | 618 | Ingreso Web por HTTPS y sesión en memoria |
| 05 | `f291386` | 607 | Ingreso Mobile con sesión aislada en memoria |
| 06 | `1527642` | 237 | Preparación manual de la demostración ficticia |
| 07 | `44464d4` | 277 | Carga local privada de configuración de API |
| 08 | `7c7abc9` | 404 | Evidencia offline y límites de aceptación |
| 09 | `318ea56` | 636 | Diagnóstico pausado de durabilidad de sesiones |
| 10 | `90b772f` | 184 | Evidencia de sesiones entre reinicios de API |
| 11 | `c339372` | 556 | Inspección MySQL de solo lectura |
| 12 | `41f4024` | 140 | Propuesta acotada de corrección de clave de pruebas |
| 13 | `6005c3c` | 174 | Comprobación de cobrador y acceso Mobile compartido |
| 14 | `bb0452a` | 115 | Comprobación administrativa en el ingreso Web |

This bootstrap changes only this coordination document, not the product, backlog or runtime configuration.
