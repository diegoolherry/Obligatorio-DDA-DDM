# MVP-004 purchase contract validation

## Objective and scope
Implement only the approved purchase initiation response DTO and structural input validation, including null list elements and per-leg validation. This is not a functioning purchase flow.

## Authorization and constraints
- User approved `IValidatableObject` exclusively to inspect `Tramos` elements and apply their DataAnnotations.
- Reuse `ItinerarioDto`; do not add endpoints, services, persistence, business states, checkout, payments, holds or concurrency mechanisms.
- Preserve the existing Mobile string-ID correction; do not include it in this work-unit commit.
- Keep `docs/tasks.json` unchanged; MVP-004 remains incomplete and requires Diego's human review.
- No new libraries. Follow existing console-check projects.

## Tasks
- [x] PC-01: Observed failing input-validation checks, implemented annotations and per-element validation, added the response DTO, and verified the API build.
- [ ] PC-02: Independently verify the scoped contracts and tests, record limitations and the work-unit commit.

## Routing and acceptance
- PC-01: delegated direct; multiple non-trivial files and test execution require a bounded writer.
- PC-02: delegated verification and parent spot check; native RDD follows its own assessment.
- Reject null/empty lists, null elements, invalid orders and missing/blank service IDs, with indexed member paths for element errors.
- Accept valid selections without imposing new business rules; validate every non-null element.
- Response properties: `IntentoId: string`, `Itinerario: ItinerarioDto`, `HoldVenceEn: DateTime` documented UTC; no invented UTC conversion or ID generator.

## Verification
- `dotnet run --project tests/PaymentContracts/PaymentContracts.csproj`
- `dotnet build src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --nologo`
- `git diff --check`
- No API server, database or payment/provider calls. Console DTO checks do not prove HTTP model binding or business invariants.

## Delivery
- Forecast: approximately 250 authored lines; strategy `ask-on-risk`, below the 400-line planning heuristic.
- One coherent work-unit commit for API DTOs and their checks; no push or PR authorized.
- Native review: pending assessment; no approval claimed.
- Engram mirror: pending; prior writes blocked by host-session identity hook.

## Progress and next step
- PC-01 complete: initial RED run reported 12 failures of 16 checks (after restore); after implementation, the suite reported 27 checks and 0 failures. The API build passed with 0 errors and 2 NU1900 vulnerability-feed warnings because api.nuget.org was unreachable. `git diff --check` passed (no output); newly added files were also inspected directly.
- PC-02 independent verification passed: 27/27 checks, API build exit 0 with the known NU1900 warning, direct new-file readback and no remaining findings. Parent re-ran the 27 checks successfully. Work-unit commit and native assessment are pending.



