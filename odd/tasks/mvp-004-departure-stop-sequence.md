# MVP-004 departure stop sequence

## Objective
Implement the approved ordered stop occurrences per operational Salida and explicit service boarding/alighting references. Update the live hold policy to fifteen configurable minutes without implementing holds in this foundational slice.

## Authorization and boundaries
- User explicitly approved the stop sequence and service bounds, and changed the configurable hold duration from five to fifteen minutes. Do not attribute this update to a new joint team approval.
- Preserve all prior commits and the dirty Mobile contract correction.
- No MySQL connection, schema application, reset, seed execution, data backfill, API startup or push.
- No purchase/hold/payment endpoints, seat allocation algorithm, price policy, or concurrency implementation in this slice.
- Existing unlinked or partially prepared legacy services remain representable; seat reservation will later require a validated complete mapping.

## Tasks
- [ ] SS-01: Observe runtime RED checks, implement model/mapping/demo sequence and generate additive offline migration plus SQL.
- [ ] SS-02: Verify independently, update current policy documentation and record scoped local commit/native review outcome.

## Design
- ParadaSalida occurrence key `(SalidaId, Orden)` with a ParadaId reference; repeated physical stops may have different orders.
- Nullable service `OrdenEmbarque` and `OrdenDesembarque`, two composite FKs to occurrences within the same Salida, Restrict deletion.
- Both bounds absent remains valid for legacy rows; supplied bounds require SalidaId, both endpoints and strict increasing order. No data inference or backfill.
- Demo SA-1: order1 Radial, order2 Colonia, second service interval `[1,2)`; first no-seat service unchanged.
- Foreign keys validate occurrence membership, not correspondence to Tramo endpoints or actual availability. Those server-side checks remain mandatory before future reservations.

## Verification
- `dotnet run --project tests/OperationalAdministration/OperationalAdministration.csproj`
- `dotnet run --project tests/AuthenticationSessions/AuthenticationSessions.csproj -- --unit`
- `dotnet run --project tests/PaymentContracts/PaymentContracts.csproj`
- API build, offline EF migration/script generation, pending-model check, SQL inspection/regeneration.
- Tests cover repeated stops, separate departures, demo bounds, composite metadata and legacy nullability. No MySQL behavior claimed.
- Validate JSON and preserve backlog IDs, createdAt, states and completion evidence; weekly report and historical task journals remain historical.

## Delivery
- Strategy: feature-branch-chain, already selected by the user.
- Branch `feat/mvp-004-departure-stop-sequence`, base `feat/mvp-004-departure-inventory` at `7d7302e`.
- Forecast approximately250 authored lines, excluding EF-generated artifacts. Generated-diff size exception remains pending before any future PR publication; no PR or push authorized.
- Commit code, tests and docs together, excluding prior Mobile correction. Native review applies only to this new cut, never reopens the consumed inventory target.
- Engram mirror pending if the host-session hook continues rejecting writes.

## Progress
- Stop-sequence source, tests, documentation updates and offline artifacts are prepared. SS-01/SS-02 remain unchecked until independent review, parent local commit and the native-review outcome.
- Final post-refinement verification: `dotnet build src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --nologo --no-restore` passed with 0 errors; OperationalAdministration passed 136 structural checks; AuthenticationSessions passed 50/50 unit checks; PaymentContracts passed 27 checks. The prior `dotnet ef migrations has-pending-model-changes --project src/Api/API-Berruti/API-Berruti --no-build` reported no pending model changes; this refinement touched only documentation and tests.
- Generated offline EF migration `20261116191031_ParadasSalidaServicio` from `20261116181813_InventarioSalidaDemo`; generated `scripts/database/paradas-salida-servicio.sql`. Migration inspection found additive nullable service-bound columns, the new occurrence table, indexes, range CHECK and restrictive FKs only; no data backfill or DML beyond EF migration-history SQL. No migration was applied.
- Recovery incident note from the parent handoff: a prior worker attempted the prohibited EF `migrations remove --force` operation; a subsequent MySQL connection attempt failed with `Unable to connect`. The parent reported no successful database connection. Local migration artifacts were removed and regenerated with offline `migrations add`. This recovery made no database connection attempt and ran no update/reset/seed; do not describe the entire cut as having had no connection attempt.
- Known tooling warnings: NU1900 vulnerability-index access is unavailable; EF tools 10.0.9 are older than runtime 10.0.11. MySQL execution and runtime behavior remain unverified and out of scope.
- Canonical hold policy is now 15 configurable minutes by explicit user update, replacing the prior joint conceptual five-minute value without a new joint approval; historical MVP-010/05-minute transfer evidence remains unchanged.
- Pre-freeze findings resolved: `data-model.md` now labels the complementary model's current hold duration as 15 minutes and points to §2 while retaining the five-minute history; operational tests no longer use anonymous sample rows for occurrence identity; AuthenticationSessions now exercises real `ParadaSalida` EF tracking for repeated physical stops, another departure at the same order, and duplicate tracked keys in a separate context. This proves EF identity-map behavior only, not MySQL uniqueness. Metadata assertions do not execute the SQL CHECK; this limitation is documented.
- Fresh post-refinement verification: OperationalAdministration passed 136 structural checks; AuthenticationSessions passed 50/50 unit checks; PaymentContracts passed 27 checks; the API build passed with 0 errors. `docs/tasks.json` parses (36 tasks). UTF-8, final LF, U+FFFD, trailing-whitespace and `git diff --check` validations passed for inspected files.
- Independent recheck remains pending; SS-01/SS-02 stay unchecked until parent review, commit and native-review outcome.
- Existing dirty Mobile correction is preserved and excluded from this cut. No tasks are complete; commit and native review remain pending. No branch switch, staging, commit, push or PR was performed.
