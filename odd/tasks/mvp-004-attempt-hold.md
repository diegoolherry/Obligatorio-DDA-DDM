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

## Remaining delivery parts

Continue the existing feature-branch chain locally; no PR or push is authorized. Remaining implementation is forecast to exceed 400 authored lines overall and will be split by cohesive behavior, not by file type. Earlier size exceptions apply only to their original two slices, not automatically to future cuts.

- [x] HTTP-01: expose existing passenger attempt creation/read contracts with focused tests (commit `2287a16`).
- [ ] PAY-01: implement authoritative TEST checkout, durable payment reconciliation, purchase/ticket issuance and compensation tracking. Resolve the undocumented confirmation-time boundary before implementing it.
- [ ] WEB-01: connect Web checkout and its recoverable states to the API.
- [ ] MOB-01: connect Mobile checkout and its recoverable states to the API.

All parts use delegated direct execution because they cross non-trivial code/test boundaries. Functional checks apply to each changed behavior; unchanged suites retain their earlier evidence. Completion of this checklist does not substitute for backlog acceptance, provider/MySQL verification, or human cross-review.

## HTTP attempt cut (functionally checked)
- User authorization now covers completing the remaining MVP-004 work with Mercado Pago CheckoutPro TEST credentials only, reusing the existing implementation and explaining each cut. This worker is limited to authenticated passenger HTTP create/read endpoints over the existing `IIntentosCompraService`, focused deterministic contract checks, and source-accurate API documentation; payment verification, purchase/ticket confirmation, Web, and Mobile remain separate pending cuts.
- Preserve all existing backend/schema history above. The schema review outcome remains pending/unrecoverable as reported by the user; do not retry review or treat prior checks as approval. GentleAI remains enabled; no optional review, database connection/modification, API startup, push, PR, staging, branch mutation, or commit is authorized for this worker.
- Route: delegated direct, because this coherent vertical slice touches controller, test harness, API reference, and active trackers. HTTP decisions are grounded in current service/DTOs and established authorization conventions; account identity comes only from the authenticated NameIdentifier claim.
- Acceptance for this cut: POST creates an attempt and returns `201 Created`; GET consults only by attempt ID for the current passenger; expected validation/missing/seat-conflict errors are safe; unexpected errors disclose no details; cancellation flows to the service; no payment, entity, or client-price contract is introduced.
- Verification observed: `dotnet run --no-restore --project tests/PaymentContracts/PaymentContracts.csproj` — 48/48; `dotnet build src/Api/API-Berruti/API-Berruti --no-restore` — 0 errors; `git diff --check` — pass. Both .NET commands emitted NU1900 because the NuGet vulnerability index could not be reached. No API startup or MySQL activity.
- RED: the first PaymentContracts run failed at compile time because the controller did not yet exist (CS0246), proving the new boundary test was active. GREEN: PaymentContracts 48/48; API `dotnet build --no-restore` completed with 0 errors; `git diff --check` passed. Both .NET commands reported NU1900 because the NuGet vulnerability index could not be reached; no package restore was requested.
- The existing repository `ConsultarAsync` filters by both opaque attempt ID and database user ID, so a missing or foreign attempt shares the same `KeyNotFoundException`/404 behavior. Controller unit tests verify the claim is passed; they do not execute HTTP middleware, TLS, MySQL, or the route URL generation pipeline.
- Backlog conceptual evidence retained: at most one confirmed purchase per durable attempt is a domain concept with payments preserved, not a physical database cardinality; TEST refund support remains unverified. Original RS-01/RS-02 history above remains preserved and does not establish full MVP-004 completion.
- Parent verification: independent boundary inspection found and corrected invalid literal newlines in backlog evidence; strict `JSON.parse` and `git diff --check` now pass. Parent repeated the changed PaymentContracts suite: 48/48. No unaffected suite was rerun.
- Work-unit commit: `2287a16` on `feat/mvp-004-attempt-http`, base `f599a70`; 230 authored additions/deletions. Native assessment for that committed range: `medium`, `review_due=false`, `under_budget`. It is not native approval; the unrelated earlier unrecoverable schema review remains pending. Rollback boundary is the new controller, its focused checks, API reference and task progress, with no domain/schema changes.
- Resolved decision for PAY-01a: the API confirmation timestamp and original hold deadline are both UTC; equality is expired. Earlier provider approval does not change the API-time cutoff.

## PAY-01a — pure confirmation policy
- User explicitly resolved the confirmation-time boundary: the API evaluates its own UTC confirmation time against the original hold deadline; deadline equality is expired. Provider approval time is not a substitute. Availability must also be confirmed. Approved-but-late or unavailable payment enters refund follow-up; pending/unknown waits, rejection/refund means no issuance, and refund-pending remains follow-up.
- Scope: add a reusable pure policy and minimal payment lifecycle states, focused tests, and a model explanation. Do not wire endpoints, provider mapping, persistence, EF, QR, or MySQL in this cut. No new packages or network restore.
- Acceptance: only approved + API confirmation strictly before deadline + confirmed availability confirms; all other states fail closed into wait, no-issuance, or refund-follow-up as applicable. Both timestamps must be UTC. Tests cover deadline boundary, availability, state routing, invalid enum, and UTC validation.
- Route: delegated direct. Verification required: `dotnet run --no-restore --project tests/PaymentContracts/PaymentContracts.csproj`; `dotnet build src/Api/API-Berruti/API-Berruti --no-restore`; `git diff --check`. Preserve prior HTTP 48-check evidence; unrelated suites are not repeated.
- RED observed: PaymentContracts failed to compile because the policy and state types were absent (CS0103/CS0246). GREEN observed: PaymentContracts 60 checks, 0 failures; API build 0 errors; `git diff --check` passed. .NET commands emitted NU1900 because the NuGet vulnerability index was unavailable. No restore was requested. No runtime integration, provider mapping, refund operation, or persistence is claimed. Existing HTTP proof remains `2287a16`.

## PAY-01b — Checkout Pro Orders TEST adapter
- The provider boundary is being split into two independently buildable work units. PAY-01 remains incomplete; no payment endpoint, persistence, purchase/ticket issuance, compensation tracking, Web/Mobile flow, provider call, API startup, or database behavior is included.
- PAY-01b1 (this worktree cut): add only authoritative GET status verification, lazy TEST credential configuration/DI, a typed snapshot and mocked HTTP contract tests. Approval requires `status=processed`, `status_detail=accredited`, and exact full paid amount. Official refund details map to refunded/follow-up; undocumented or missing status/detail combinations remain unknown. Reject `.` and `..` IDs before HTTP.
- PAY-01b2 (preserved in this worker's in-memory snapshot for a follow-up): POST Checkout Pro order creation with exact amount/reference/idempotency payload and validated hosted URL; submit full-refund request with stable idempotency key without claiming refund completion. Parent will request restoration after PAY-01b1 is accepted.
- Route: delegated direct. The source files and docs are in the allowed surfaces. Verification: PaymentContracts `dotnet run --no-restore`; API `dotnet build --no-restore`; `git diff --check`. No unrelated suites rerun.
- RED observed for the status regression: 5 checks failed when the previous broad status-only mapping was exercised against refund, partial-refund, missing/unknown detail, and mismatched status/detail cases. GREEN for the corrected full adapter before slicing: PaymentContracts 109 checks, 0 failures; API build 0 errors; `git diff --check` passed. Both .NET commands emitted NU1900 because the NuGet vulnerability index was unavailable; no restore was requested.
- Official status details were checked against Mercado Pago's [Checkout Pro Orders order-status table](https://www.mercadopago.com.uy/developers/en/docs/checkout-pro-orders/payment-management/status/order-status?scope=prod) and [GET order API reference](https://www.mercadopago.com.uy/developers/es/reference/online-payments/checkout-pro/get-order/get). The earlier status-only approval mapping was incorrect; never infer approval from `processed` and amount alone.
- PAY-01b1 final scope is 391 authored changed lines (including docs/tracker; the pre-existing Mobile change is excluded). It is independently buildable with GET status verification, configuration/DI, typed snapshot and mocked HTTP tests only. GREEN after partition: PaymentContracts 94 checks, 0 failures; API build 0 errors; `git diff --check` passed. The two .NET commands emitted NU1900 due to unavailable NuGet vulnerability metadata; no restore was requested.
- PAY-01b2 restoration delta is exactly 163 authored changed lines from the current PAY-01b1 state across order creation/refund methods, interface/model extensions, mocked tests and docs; tracker verification notes will add a small evidence-only update. The saved full implementation/test snapshot is in this worker's `functions.exec` session store as `checkoutpro-full-fixed-snapshot`; restore it only on the parent's follow-up, then keep the current PAY-01b split tracker and add the POST/refund evidence. No scratch file is used.
- Preserve the unrelated dirty Mobile itinerary file; no branch, stage, commit, review lifecycle, provider credential, provider execution, or MySQL operation is authorized here.
