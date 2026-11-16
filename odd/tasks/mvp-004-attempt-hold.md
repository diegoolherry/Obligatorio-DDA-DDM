# MVP-004 purchase attempt and temporary seat hold

## Objective and preserved work
Deliver durable attempts and fifteen-minute configurable assigned-seat holds in the user-selected feature-branch chain. The complete previously tested backend remains preserved at `feat/mvp-004-attempt-hold`, commit `c1e3605`; no existing commit is rewritten.

## Authorization and boundaries
- User approved short transactions coordinated by Salida, the local two-cut division, and `size:exception` for both cohesive review cuts. The exception does not raise the native reviewer context limit or authorize a push/PR.
- No MySQL connection, schema application, API startup, reset, seed execution or push. Never use EF migrations remove, --force, database update, EnsureCreated or Migrate.
- Preserve the existing dirty Mobile type correction; exclude it from these commits.
- No HTTP/client/payment/ticket endpoint, no invented no-seat quota or new creation-idempotency contract.
- The fifteen-minute deadline is fixed at creation, not renewed by consultation. Fake concurrency/metadata checks never prove MySQL behavior.

## Tasks
- [ ] AH-01: Preserve and deliver the approved backend implementation and its existing behavioral tests in independently verifiable chained cuts.
- [ ] AH-02: Record scoped commits, independent checks and native review outcomes without claiming full MVP completion.

## Review slices: one honest partition
- [ ] RS-01: Schema, IC durable identity, all associated EF artifacts, and focused offline metadata/migration checks.
- [ ] RS-02: Runtime service/repository/configuration and the complete original PurchaseHolds behavioral suite.

| Cut | Branch and base | Scope |
| --- | --- | --- |
| Preserved original | feat/mvp-004-attempt-hold at c1e3605 | Original implementation and proof; unchanged reference, not reviewed |
| RS-01 | feat/mvp-004-attempt-hold-01-schema from da5282c | Models, mappings, IC prefix, migration/designer/snapshot/SQL, schema checks |
| RS-02 | feat/mvp-004-attempt-hold-02-runtime from RS-01 | Remaining original runtime/configuration/tests and backend explanation |

## Review boundary and size decision
- Native START for the original 2,920-line candidate returned `lens_context_budget_exceeded`, `mutation_outcome: not_started`; no review authority or approval was created. Do not retry the unchanged candidate or abandon nonexistent authority.
- RS-01 contains approximately1,811 existing changed lines plus new focused checks; RS-02 approximately1,109 existing changed lines. Generated artifacts remain intact. The user explicitly accepted the size exception for both cuts.
- Neither estimate guarantees native context fit. Each new target requires a fresh native transition and its own consent. If this single cohesive partition still cannot fit, stop and report rather than truncate evidence or repeatedly repartition.

## Verification
- RS-01: API build; new `tests/PurchaseHoldSchema` console checks; OperationalAdministration, AuthenticationSessions --unit and PaymentContracts. Model/schema checks do not call MySQL.
- RS-02: repeat RS-01 checks plus all original PurchaseHolds behavior checks. Compare original production and behavioral-test bytes against `c1e3605` after restoration.
- Existing complete-original evidence: PurchaseHolds74/74, OperationalAdministration136, AuthenticationSessions50/50, PaymentContracts27/27, API0errors. These are historical results for c1e3605, not fresh RS-01 results.
- New tests are regression/structural proof for preserved code, not a new behavior change; original implementation already observed RED/GREEN. No artificial RED or production edit is required for reslicing.
- IC is initialized only by migration Up. Automatic Down refuses before SQL to preserve durable counters. The prior duplicate seed and mapping/date guards remain fixed in the saved original.

## Progress
- RS-01 independent verification found no blocking issues: schema checks12/12, OperationalAdministration136, AuthenticationSessions50/50, PaymentContracts27/27, API0errors and no pending EF model changes. All nine storage files match c1e3605 canonical Git blobs. No MySQL behavior or native context fit was demonstrated.
- Plan created on the new schema branch before materializing preserved source. No new review has begun. Both new candidate checks and commits are pending; Engram mirror remains pending if its host-session hook rejects writes.
- RS-01 schema checks are now present in `tests/PurchaseHoldSchema` and pass 12/12. They inspect EF design-time metadata and migration operations offline; they do not connect to MySQL or claim provider-enforced constraints. The placeholder connection remains closed before and after model access.
- RS-01 verification observed: `dotnet run --project tests/PurchaseHoldSchema/PurchaseHoldSchema.csproj` — 12/12; `dotnet run --project tests/OperationalAdministration/OperationalAdministration.csproj` — 136 checks; `dotnet run --project tests/AuthenticationSessions/AuthenticationSessions.csproj -- --unit` — 50/50; `dotnet run --project tests/PaymentContracts/PaymentContracts.csproj` — 27 checks; API build — 0 errors; `dotnet ef migrations has-pending-model-changes --project src/Api/API-Berruti/API-Berruti --no-build` — no pending model changes. NuGet vulnerability-index requests emitted NU1900 because `api.nuget.org` was unavailable; commands still completed successfully.
- RS-01 implementation checks are observed, but its work-unit commit and fresh native review outcome remain pending. The prior 2,920-line original candidate's native context-budget refusal created no authority; neither the accepted size exceptions nor these results establish native context fit for either new cut.
- AH-01 is not complete: the full backend and behavioral tests remain preserved at `c1e3605` on `feat/mvp-004-attempt-hold`, not present on this schema branch. RS-02 runtime/configuration restoration and its complete behavioral suite remain pending. Do not infer runtime classes are absent from the original implementation; they are simply outside this branch's cut.
