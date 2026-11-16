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
| RS-01 | feat/mvp-004-attempt-hold-01-schema, commit 1efbb27, from da5282c | Models, mappings, IC prefix, migration/designer/snapshot/SQL, schema checks |
| RS-02 | feat/mvp-004-attempt-hold-02-runtime from 1efbb27 | Remaining original runtime/configuration/tests and backend explanation |

## Review boundary and size decision
- Native START for the original 2,920-line candidate returned `lens_context_budget_exceeded`, `mutation_outcome: not_started`; no review authority or approval was created. Do not retry the unchanged candidate or abandon nonexistent authority.
- RS-01 commit `1efbb27` contains 1,811 preserved storage changed lines, 141 focused-check additions and 45 tracker additions (1,997 total). Restored RS-02 source/docs/tests comprise 13 paths, 1,046 additions and 9 deletions (1,055 changed lines), before this tracker update. Generated artifacts remain intact. The user explicitly accepted `size:exception` for both cuts.
- Neither estimate guarantees native context fit. Each new target requires a fresh native transition and its own consent. If this single cohesive partition still cannot fit, stop and report rather than truncate evidence or repeatedly repartition.

## Verification
- RS-01: API build; new `tests/PurchaseHoldSchema` console checks; OperationalAdministration, AuthenticationSessions --unit and PaymentContracts. Model/schema checks do not call MySQL.
- RS-02: repeat RS-01 checks plus all original PurchaseHolds behavior checks. Compare canonical Git blobs for original production and behavioral-test content against `c1e3605` after restoration; working-tree line endings may normalize.
- Existing complete-original evidence: PurchaseHolds74/74, OperationalAdministration136, AuthenticationSessions50/50, PaymentContracts27/27, API0errors. These are historical results for c1e3605, not fresh RS-01 results.
- New tests are regression/structural proof for preserved code, not a new behavior change; original implementation already observed RED/GREEN. No artificial RED or production edit is required for reslicing.
- IC is initialized only by migration Up. Automatic Down refuses before SQL to preserve durable counters. The prior duplicate seed and mapping/date guards remain fixed in the saved original.

## Progress
- RS-01 independent verification found no blocking issues: schema checks12/12, OperationalAdministration136, AuthenticationSessions50/50, PaymentContracts27/27, API0errors and no pending EF model changes. All nine storage files match c1e3605 canonical Git blobs. No MySQL behavior or native context fit was demonstrated.
- Plan created on the schema branch before materializing preserved source. RS-01 is committed at `1efbb27c9d8313e04e57e5dd61820599f2ebcb3e`, parent `da5282c`. RS-02 restoration is present on its child branch; its commit and both fresh native outcomes remain pending. Engram mirror remains pending while the host-session hook rejects writes.
- RS-01 schema checks are now present in `tests/PurchaseHoldSchema` and pass 12/12. They inspect EF design-time metadata and migration operations offline; they do not connect to MySQL or claim provider-enforced constraints. The placeholder connection remains closed before and after model access.
- RS-01 verification observed: `dotnet run --project tests/PurchaseHoldSchema/PurchaseHoldSchema.csproj` — 12/12; `dotnet run --project tests/OperationalAdministration/OperationalAdministration.csproj` — 136 checks; `dotnet run --project tests/AuthenticationSessions/AuthenticationSessions.csproj -- --unit` — 50/50; `dotnet run --project tests/PaymentContracts/PaymentContracts.csproj` — 27 checks; API build — 0 errors; `dotnet ef migrations has-pending-model-changes --project src/Api/API-Berruti/API-Berruti --no-build` — no pending model changes. NuGet vulnerability-index requests emitted NU1900 because `api.nuget.org` was unavailable; commands still completed successfully.
- RS-01 implementation checks and commit are observed. Its read-only native assessment attempted while RS-02 files were untracked was unassessable because untracked content requires declaration; no START or authority was created. Reassess RS-01 on its clean branch after RS-02 is committed. Do not infer low risk. The original context-budget refusal created no authority; accepted size exceptions and functional checks do not establish native context fit.
- RS-02 now restores all 13 remaining original runtime/configuration/test/doc paths. Fresh offline verification passes: PurchaseHoldSchema 12/12, PurchaseHolds 74/74, OperationalAdministration 136, AuthenticationSessions 50/50, PaymentContracts 27/27; API build has 0 errors; EF reports no pending model changes. Every command exited 0. NU1900 reflects unavailable NuGet vulnerability-index access; EF also warns that tools 10.0.9 precede runtime 10.0.11.
- Preservation proof: all 22 original changed paths excluding this tracker (9 schema plus 13 runtime) match `c1e3605` canonical blobs using `git hash-object --path=<path> -- <path>` against `git rev-parse c1e3605:<path>`, including untracked files. The original branch still resolves to `c1e36055c0b9c767656bfec4b9eb4b6ee9933131`. No production logic, original behavioral tests or generated EF artifacts were altered for slicing.
- Restored runtime files pass strict UTF-8 decoding with no U+FFFD or trailing whitespace; `git diff --check` passes. Generated SQL remains unchanged, including its existing final blank line. The unrelated Mobile correction remains untouched and excluded.
- AH-01/AH-02 and RS checkboxes remain pending commit/native proof and cross-review; this is not full MVP completion. No MySQL execution, API startup, PR, push, staging, branch mutation or native lifecycle command occurred during RS-02 verification. Next: parent commits the runtime cut, then performs clean-branch native assessment using exact provider transitions.
