# MVP-005: visualización QR y validación de ticket

Owner Diego, reviewer Enzo. First slice: mockups on branch `feature/mvp-005-01-mockups` (from `origin/main` e855b26, worktree `.worktrees/mvp005`). Implementation slices are planned after the team reviews the mockups.
High-risk units for later slices: atomic single-use consumption (item 4, concurrency) and the camera dependency (item 5).

## Specs

- S1 (backlog evidence): "Antes de implementar, diseñar los mockups de sus pantallas (incluido el permiso de cámara denegado) en OpenPencil con el sistema de diseño de FIN-010 (design/fin-010, decisión de Diego del 2026-11-16)."
- S2 (AC): "El pasajero puede consultar tickets vigentes o utilizados y mostrar el QR opaco de cada tramo."
- S3 (AC): "El cobrador recibe resultados claros para ticket válido, ya utilizado, vencido, tramo incorrecto y código inexistente."
- S4 (AC): "El primer escaneo válido marca el ticket como utilizado y un segundo escaneo del mismo ticket es rechazado."
- S5 (AC): "Un ticket de otro tramo no se consume."
- S6 (description): "Permitir que el pasajero consulte y muestre cada QR de ticket y que el cobrador seleccione el tramo actual, escanee el código y reciba el resultado de la validación desde la API. La validación debe consumir el ticket de forma atómica."

## Tasks

| ID | Specs | Route | Commit |
|---|---|---|---|
| T1 Mockups M14–M27 in `design/fin-010` (passenger filter/states, used-ticket detail, collector segment picker, camera permission pre-prompt and denied, scanner, validating, vencido, tramo incorrecto, código inexistente) + README + exports | S1, S2, S3, S5 | inline; refine 14/14 without fixes, M01–M13 unchanged, visual review of PNGs; lint not run | 3b436f3 |
| T2 API: ticket state, passenger tickets query, atomic validation endpoint with tests | S2–S6 | pending team review of T1 | |
| T3 Mobile passenger: ticket list with filter and QR detail | S2 | pending team review of T1 | |
| T4 Mobile collector: segment selection, camera scan (needs `expo-camera` approval), results | S3–S6 | pending team review of T1 | |

## Log

- L1 (Diego, 2026-11-24): «sigamos con alguna tarea de @docs/tasks.json» → elegido: «Arrancar MVP-005 por los mockups».
- L2 Exploration: FIN-010 already has M05 (Mis tickets), M06 (QR), M07 (offline), M08 (Validar), M09 (válido), M10 (ya utilizado), M11 (no se pudo confirmar). No document defines exact result wording; `ux-design.md:64` lists the categories and «Validando…»; `ux-design.md:91` asks for in-context permission, explained denial and an exit without invented manual validation. API has `Ticket` without state; no ticket/validation endpoints; Mobile has no ticket/QR/scan screens; `expo-camera` not installed.
- L3 Design proposals open for team review (README question 6): passenger filter labelled «Vigentes · Anteriores» (used + expired) instead of «Utilizados»; collector picks transfer segments of the service, not every stop pair from FIN-017. Result copy is a proposal.
- L4 (Diego, 2026-11-24): filtro «Anteriores»: utilizados y vencidos (como en M15); el cobrador elige «Los tramos de transbordo del servicio (como en M20)».
