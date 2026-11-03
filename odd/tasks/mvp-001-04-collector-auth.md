# MVP-001-04 — Collector authentication
Objective: reuse existing authentication for the collector without SQL changes or API startup.
Delivery: one bounded local source/test/docs commit; runtime/device proof and Enzo review remain separate.

## Specs

- S1 — User: "dale mientra vos seguis, dsp verifico eso. Al final de MVP 001 revisamos todo."
- S2 — User selected "Una pantalla compartida (recomendado)" for passenger/collector Mobile entry. Reuse the current form; permissions remain API-authoritative.
- S3 — Existing criterion, verbatim: "La cuenta ficticia de conductor/cobrador puede iniciar y cerrar sesión en Mobile; tras el cierre, una operación protegida requiere autenticarse nuevamente."
- S4 — Existing criterion, verbatim: "La evidencia verifica que la API permite una operación protegida autorizada al cobrador y deniega una no autorizada a ese rol, además del acceso sin sesión; no basta con ocultar controles en Mobile."
- S5 — Existing criterion, verbatim: "Mobile comunica carga, éxito, credenciales rechazadas, fallo de red o servidor y reintento sin mostrar éxito cuando la autenticación falla."
- S6 — Preserve pending SQL gate in passenger feature S21/T18/L102; no live SQL, private credential reads, seeds, API startup, device test or academic completion in this unit.

## Tasks

- T1 [S2-S6] — Implement a read-only collector authorization probe and typed Mobile action on the shared screen, with regression tests and affected documentation. Route: inline. Status: done. Evidence: Mobile17/17 + typecheck exit0; API45/45 and contracts50/50 exit0. Risk: item 2 (role authorization); item 3 (shared Mobile request dispatch). Commit: `a198d8b697b3bd930138bc77dac81fa76e46454e`.
- T2 [S2-S6] — Run focused deterministic checks, a single base/current shared-service differential and independent security verification. Route: parent + independent verifier. Status: done. Evidence: independent mw1617hy-2-bpnz API45/45, contracts50/50, Mobile17/17 and typecheck exit0; no concrete blockers. Commit: `a198d8b697b3bd930138bc77dac81fa76e46454e`.
- T3 [S1,S3-S5] — Verify real collector Mobile/API/MySQL/device behavior and obtain Enzo cross-review before completion. Route: human environment/review. Status: blocked (waiting for administrative SQL proof, prepared fictitious collector account, device/TLS availability and Enzo review). Commit: runtime evidence only, pending.

## Log

- L1 — Original request, verbatim: "dale mientra vos seguis, dsp verifico eso. Al final de MVP 001 revisamos todo."
- L2 — User: "dale eso que falta guardalo en memoria o en donde quede mejor"; SQL gate and final review saved in memory852.
- L3 — User: "preguntamelo en español"; shared-screen selection: "Una pantalla compartida (recomendado)".
- L4 — 2026-11-03T17:50:13.067053-03:00: dependencies MVP-010 and MVP-001-02 observed done. Read-only exploration and parent spot check confirm general login/logout, no collector probe, passenger-only Mobile label/action. No need to duplicate authentication or create business operations. Existing passenger and administrator probes preserved. API stays off. No live collector proof claimed.

- L5 — Mobile RED15/17 (missing method/shared label), then GREEN17/17 and typecheck exit0. API first test attempt failed compilation due test string escaping (not behavioral RED); corrected test then RED44/45 on absent collector endpoint. Endpoint added: API45/45 and contracts50/50 exit0 in job3. No network/DB/startup. Source-level UI assertion is structural only. ASSESS escalation2 returned independentVerifier:true, RDDoff/nativeunassessable (untracked declaration); no native continuation executed. Parent assessment defaulted original cwd rather than worktree; it is not a candidate review or evidence for this diff. Independent scoped verification follows.

- L6 — 2026-11-03T17:57:15.063690-03:00: independent verifier mw1617hy-2-bpnz conforms offline S2-S6, API45/45/contracts50/50/Mobile17/17/typecheck exit0, all once; source/index unchanged. Existing passenger/admin implementation and metadata preserved. Parent single differential of existing login/passenger/logout with collector identity passed exact requests/results/state; JSON22 records/immutables/dependencies/fences and whitespace checks passed. Node experimental warning only. T2 done, T3 remains blocked for actual environment/device and Enzo review; no academic completion. Source slice based on41f4024; local commit only, no publishing.

- L7 — Source slice archived locally in a198d8b697b3bd930138bc77dac81fa76e46454e (162+/11-, base41f4024), no push. Post-commit JSON parse exposed a bookkeeping defect: CRLF-sensitive evidence append retained a closing quote and lost a line break. Fixed immediately; json.loads and deep22-task comparison vs41f4024 passed, other21tasks and immutable fields identical, evidence prefix preserved, only collector status/startedAt/evidence changed. Follow-up documentation fix archives valid JSON and source commit mapping; no SQL or application logic affected. Preserve failed parse as evidence; do not publish intermediate invalid snapshot.
