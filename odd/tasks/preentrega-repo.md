`10c6bf4` |`50bc805`, `56030ca` |`5d6855e` |`851a6ff` |`1b353bd` |# Preentrega del repositorio (domingo 2026-11-22)

Puesta a punto del repositorio para la preentrega: el entregable es solo el repo. Rama `chore/preentrega-repo` desde `origin/main` (`3f122e5`).

## Specs

- S1 — Bloqueantes: "Reescribir project-report.md, arreglar RouteCatalog y MobileLocation, y escribir la guía de Mercado Pago TEST de punta a punta."
  - S1a — `docs/project-report.md` deja de contener notas internas de agentes (ids de subagentes, "gentle-ai-verify", "harness", "scratch", "RDD", "ego", links a `odd/`) y refleja el estado real: MVP-001..004, MVP-009, FIN-001/006/008/010/011/012, decisiones (JWT, hold de 15 minutos, baja lógica), evidencia con links a PRs, riesgos y pendientes (MVP-005..008, FIN-013). Con fecha de última actualización.
  - S1b — `tests/RouteCatalog` vuelve a compilar y pasar (lo rompió MVP-004: `Models/Operational/ResultadoConciliacionPago.cs` depende de `DTOs/Operational`).
  - S1c — `tests/MobileLocation`: el test "end to end with the real session: Bearer is attached by the session and 403 keeps it" (`location.test.mjs:147`) pasa, corrigiendo el código o el test según cuál esté mal.
  - S1d — Guía de Mercado Pago TEST de punta a punta para alguien nuevo: cuentas de prueba vendedora y compradora, token del vendedor, `ExpectedSellerId` = id del dueño del token, `MLU`, claves en el `.env` privado de la API, arranque de API y Web, tarjetas de prueba, y lo observado en el E2E del 2026-11-19.
- S2 — Docs desactualizadas + tutorial + README: "Actualizar lo que dice \"no implementado\", sumar al tutorial el paso de la base y la demo completa, y ordenar el README y las guías retiradas."
- S3 — Higiene: "Renombrar los 5 scripts SQL, ampliar .gitignore y corregir las rutas rotas del backlog."
- S4 — Backlog: "Cerrar MVP-004 (done)": "Enzo revisó y mergeaste todo: marco done con completedAt, completedBy y la evidencia del E2E." FIN-005 sin cambios de estado.
- S5 — Fuera de alcance: sacar `odd/` del repo (no elegido).

## Tasks

| ID | Specs | Route | Commit |
|---|---|---|---|
| T1 | S1b, S1c | worker tests | `1b353bd` |
| T2 | S1a | worker informe | `851a6ff` |
| T3 | S1d, S2 | worker docs | `5d6855e` |
| T4 | S3 (backlog paths, .gitignore), S4 | worker backlog | `50bc805`, `56030ca` |
| T5 | S3 (renombrar scripts SQL y sus referencias) | inline, después de T3 | `10c6bf4` |
| T6 | Verificación final (build, suites, links, JSON) y PR | inline | — |

## Log

- L1 (Diego, 2026-11-19): "perfecto, el domingo 22 tenemos q hacer una preentrega, con el repo esta bien, fijate si habria q mejorar algo"
- L2 (Diego): alcance elegido: bloqueantes; docs desactualizadas + tutorial + README; higiene SQL/.gitignore/rutas. Backlog: "Cerrar MVP-004 (done)". Consigna: "Solo el repo".
- L3: auditoría previa sobre `3f122e5`: build API/Web OK; suites offline OK salvo RouteCatalog (no compila, roto por MVP-004; en `60f9142` daba 201/201) y MobileLocation (13/14, ya fallaba en `60f9142`).
- L4: los workers de T2, T3 y T4 se cortaron por límite de uso; se rescató su trabajo parcial y se relanzaron T3 y T4.
- L5 (T6): API/Web build OK; AuthenticationContracts 50/50, OperationalControllerValidation 4/4, RouteCatalog 201/201, PurchaseHoldSchema 22/22, PurchaseHolds 83/83, PaymentContracts 378/0, WebPurchase 213/213, WebAuthentication 182/182, OperationalAdministration 150, Geolocation PASS, AuthenticationSessions 68/68, ApiLocalEnv 45/45, MobilePurchase 21/21, MobileAuthentication 26/26, MobileLocation 14/14, tsc OK; tasks.json válido; 25 .md sin links rotos. migrations.sql conserva su nombre (lo fija un test y local-database-reset.md).
