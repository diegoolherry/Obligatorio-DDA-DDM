# MVP-004 departure inventory

## Objective
Prepare the approved operational `Salida` identity and demonstration seat inventory without connecting to or modifying MySQL.

## Approved scope
- `Salida`, `SalidaId`, prefix `SA-` (repository scan found no conflicting identity), existing 1..50000 durable-counter policy.
- Composite seat identity `(SalidaId, NumeroAsiento)`; exactly 46 seats in the Radial-Colonia demonstration.
- Optional `Servicio.SalidaId`; preserve existing `SalidaCatalogoId` and catalog `HoraSalida.SalidaId` semantics.
- Additive EF migration and generated SQL ONLY. Never apply migrations, run reset/seed, start the API, inspect private settings or connect to any database.
- No vehicle/configuration UI, interval availability, reservation, hold, payment or purchase endpoints.
- Preserve existing capacity46 edits and previous Mobile correction; Mobile is outside this unit.

## Tasks
- [x] DI-01: Test-first models, relationships, demo inventory, reused SA counter support and generated offline migration/SQL; implementation commit `9754cbc`.
- [x] DI-02: Independent verification, parent spot check and local work-unit commit; granted native review closed by exact acknowledgement.

## Routing
- DI-01 delegated direct: multi-file implementation, migration generation and tests.
- DI-02 delegated verification: schema/identity changes warrant independent review; native RDD remains user-owned.

## Acceptance and checks
- New operational Salida has a real prefixed string ID and local trip date; no invented vehicle assignment.
- Only the demo second service explicitly links to its demo Salida; legacy services remain nullable/unlinked.
- Demo inventory contains distinct numbers1..46 for the same Salida; first service remains30/no-seat.
- Composite key and Restrict FKs preserve distinct departure inventories and avoid cascade data deletion.
- Source reset workflow remains coherent for a future separately authorized reset but is never executed here.
- `dotnet run --project tests/OperationalAdministration/OperationalAdministration.csproj`
- `dotnet run --project tests/AuthenticationSessions/AuthenticationSessions.csproj -- --unit`
- `dotnet run --project tests/PaymentContracts/PaymentContracts.csproj`
- API `dotnet build`; offline `dotnet ef migrations add` and bounded `migrations script` only.
- Generated SQL, EF metadata and migration artifacts inspected; no MySQL runtime guarantees claimed.
- Check staged/new files explicitly, UTF-8 and whitespace; ordinary diff omits untracked files.

## Delivery and recovery
- Observed approximately181 authored lines for this cut, excluding EF-generated migration/designer/snapshot/SQL and prior Mobile changes; existing purchase-contract branch has269 authored lines, for approximately450 cumulative lines.
- Delivery strategy: `ask-on-risk`. Chain strategy: `feature-branch-chain` (selected by the user). The authored branch forecast is approximately450 lines, already beyond the ~400-line review budget; the generated migration, designer, model snapshot and SQL add unavoidable cohesive generated diff. Do not compress, hand-edit, or split generated artifacts to chase the budget. A `size:exception` recommendation remains pending before any future publication; it is not approved.
- Planned chain and current boundaries:

  | Position | Branch / base | Contents and status |
  | --- | --- | --- |
  | Tracker | `feat/mvp-004`, from `main` at `1e152af` | Created locally; final integration branch. |
  | Cut 1 | `feat/mvp-004-purchase-contracts`, based on tracker | Commits `d64b610`, `ceabbd9`, `a757bf9`; current HEAD `a757bf9`. |
  | Cut 2 | `feat/mvp-004-departure-inventory`, based on cut 1 at `a757bf9` | Created locally; implementation `9754cbc`, verified and reviewed. |
  | Planned follow-up cuts | Not created | Attempt/hold → TEST provider → ticket confirmation → clients, in that order; these are responsibility boundaries only, not implementation, schema, or provider approvals. |

  ```text
  main@1e152af
    └── feat/mvp-004 (local tracker)
         └── feat/mvp-004-purchase-contracts@a757bf9 (d64b610 → ceabbd9 → a757bf9)
              └── feat/mvp-004-departure-inventory@9754cbc (reviewed inventory)
                   └── planned: attempt/hold → TEST provider → ticket confirmation → clients
  ```

- The parent created the local branches and scoped implementation commit without rewriting existing commits. No push, remote operation, or PR was performed. Any future tracker PR must be draft/no-merge and requires explicit remote authorization. The existing dirty Mobile itinerary string correction is preserved and excluded from this inventory cut; keep it for a later appropriate Mobile slice.
- No task marked complete without observed checks and its work-unit commit.
- Engram mirror pending because host-session identity hook has rejected writes.
- Backlog task states and weekly report remain unchanged; no human cross-review or full MVP completion inferred.

## Progress
- Local tracker `feat/mvp-004` and inventory branch `feat/mvp-004-departure-inventory` were created without rewriting existing commits; inventory is based on cut 1 at `a757bf9`.
- Staged checking detected EF's generated SQL final blank line. The byte-identical generated script is preserved rather than manually edited; source whitespace is checked normally and only the generated script's EOF-blank warning is exempted using a command-local check option.
- DI-01/DI-02 verified and committed in `9754cbc`; this closes only the offline inventory cut, not MVP-004 or MySQL integration.
- Test-first evidence: new scenario/allocator/metadata assertions failed at runtime before implementation (5 RED checks); after implementation, OperationalAdministration passed 130 structural checks and AuthenticationSessions passed 48/48 unit checks, including SA range and EF metadata checks. PaymentContracts passed 27 checks; API build passed with 0 errors.
- Generated offline EF migration `20261116181813_InventarioSalidaDemo` from base `20261111163728_CatalogoHorarios` and SQL `scripts/database/inventario-salida-demo.sql`. Migration inspection found only new inventory tables, nullable `servicio.salida_id`, and the necessary counter allowlist check replacement. No database was connected to, migrated, seeded, reset, or modified.
- Known environment warning: NU1900 vulnerability-index access unavailable; EF tool `10.0.9` is older than runtime `10.0.11`. New generated/source files passed UTF-8, LF-at-EOF and U+FFFD checks; `git diff --check` passed.
- Existing `seed46`/`test46` changes were retained in the implementation commit; the earlier Mobile itinerary correction remains preserved and uncommitted.
- Independent verification passed without blocking findings: 130 operational, 48/48 authentication unit and 27 payment checks; API build exit 0. EF reported no pending model changes, regenerated SQL was byte-identical, and 17 changed/new files passed direct encoding/whitespace inspection. Parent repeated 48/48 authentication checks successfully. MySQL execution remains unverified and expressly out of scope.
- Native review `review-b90dab6203aa07ee` was granted for `9754cbc` against `a757bf9`. The reliability review returned approved with non-blocking advisory `R3-001`; the exact acknowledgement returned `gentle-ai.review-acknowledged/v1`, `action: acknowledged`, `authority: burned`. No further lifecycle operation is required for this consumed target.

## Separate follow-up: downgrade safety
- Advisory `R3-001` identifies a future rollback issue: after the reset seed inserts the `SA-` counter, migration `Down` restores the old US-/RO-only prefix check without resolving the SA row. A downgrade may therefore fail after earlier DDL has already run.
- No downgrade, seed or MySQL operation was executed. The finding is non-blocking for the reviewed cut and does not reopen it. A safe downgrade design remains separate later work; do not blindly delete a durable counter or alter existing data. No correction was authorized or implemented by this review.
