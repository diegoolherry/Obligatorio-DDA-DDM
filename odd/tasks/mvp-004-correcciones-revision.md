# MVP-004: correcciones de la revisión cruzada

Correcciones de la revisión de Diego sobre el stack #121–#128, en una PR nueva encima de #128
(rama `mvp-004-main/09-correcciones-revision`). No cambia el diseño aprobado salvo lo indicado.

## Specs

- S1 — Estrategia elegida: "PR nueva arriba de #128 (recomendado)": "Rama nueva sobre #128 con un commit por corrección. Se mergea todo el stack de una vez, así main nunca ve los estados rotos de #122–#124. No hay que reescribir las ramas de Enzo."
- S2 — Hold: "15 minutos (como está ahora)": "Se mantiene el código y se registra la aprobación conjunta en evidence y project-report.md." Alinear también el texto de MVP-004 en `docs/tasks.json` y `docs/project-report.md`, que todavía dicen 5.
- S3 — Alcance: "Bloqueantes + bugs (recomendado)": "Compilación de MySqlPurchase, botón de reintento, 400 que filtra mensajes, mojibake, external_reference con GUID, recuperación del intento al volver a la pantalla (Web y Mobile), tests §2 post-vencimiento y limpieza de docs."
  - S3a — `tests/MySqlPurchase` compila de nuevo: `Program.cs(198,23): error CS7036 ... parámetro requerido "hasher" de "CuentasRepository"`.
  - S3b — Web: `CompraItinerario.razor:74` `disabled="@Ocupado || Vencio"` se renderiza como texto literal; el botón "Revisar cotización para reintentar" debe habilitarse según `Ocupado || Vencio`. Tras una creación ambigua o un error sin intento, el usuario debe poder volver a la lista de itinerarios sin recargar.
  - S3c — `ConciliacionesPagoController.cs`: `catch (InvalidOperationException) → BadRequest(exception.Message)` no debe exponer el mensaje interno; mapear como `PagosController` (500 saneado) según la guía de excepciones del curso.
  - S3d — Mojibake (UTF-8 doblemente codificado, p. ej. "estÃ¡") eliminado de `ConfirmacionesCompraRepository.cs` y de cualquier otro archivo del stack.
  - S3e — `external_reference` enviado a Mercado Pago deja de ser el contador local `PG-n` y pasa a un valor único global (GUID ya persistido del pago, p. ej. `ClaveCreacion`), para que dos bases locales con la misma cuenta TEST no colisionen. La búsqueda de recuperación usa ese mismo valor y su `begin_date` tolera desfase de reloj.
  - S3f — Recuperación: al volver a la pantalla de compra (Web y Mobile), el cliente consulta a la API si el pasajero tiene un intento con pago no terminal y lo retoma para poder verificar el pago; un pago aprobado no queda sin confirmar ni sin seguimiento de devolución por recargar o cambiar de pantalla. Requiere un endpoint autenticado del pasajero (contrato nuevo, aditivo).
  - S3g — Tests §2 post-vencimiento: (a) un pago pendiente/desconocido sigue bloqueando otro pago después de vencido el hold; (b) el reintento tras rechazo definitivo se niega con el hold vencido. Como tests automatizados del repositorio.
  - S3h — Limpieza de docs: quitar `docs/how-to/mvp-004-pull-requests.md` (manifiesto de PRs locales, desactualizado) y sus enlaces; quitar de `docs/how-to/purchase-test-checks.md` los detalles internos de herramientas de agentes (lineage, ACK, `repair --preflight`, review nativo); dejar `evidence` de MVP-004 en tres líneas según `AGENTS.md`.
- S4 — Fuera de alcance (decisiones pendientes, no implementar): tickets de un pago devuelto, límite de intentos por usuario, política de `Down()` en migraciones.

## Tasks

| ID | Specs | Route | Commit |
|---|---|---|---|
| T1 | S3a, S3c, S3d, S3e, S3g | worker API | — |
| T2 | S3f (API: endpoint de intento pendiente) | worker API (después de T1) | — |
| T3 | S3b, S3f (Web) | worker Web (después de T2) | — |
| T4 | S3f (Mobile) | worker Mobile (después de T2) | — |
| T5 | S2, S3h | inline | — |
| T6 | S1: verificación final (build + suites en la punta), PR #129 | inline | — |

## Log

- L1 (Diego, 2026-11-19): "lo tenemos q corregior nosotros, como procedemos a hacerlo?" — respuestas: Estrategia "PR nueva arriba de #128 (recomendado)"; Hold "15 minutos (como está ahora)"; Alcance "Bloqueantes + bugs (recomendado)".
- L2: punto de partida `refs/pr/128` = `origin/mvp-004-main/08-verificacion-final`. Hallazgos de revisión en `.local/mvp004-review/borrador-comentarios.md` (no versionado).
