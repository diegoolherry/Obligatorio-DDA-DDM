# MVP-001 — Local authentication integration recovery
Objective: prepare a complete local recovery candidate without publishing, merging or modifying existing branches.
Pinned evidence: remote main722e14d35c1e9be881c00299f97b9d5b0d3893d6; complete sourcebb0452abe6535a7b9e65def1f4f3c0347be547a6.

## Specs

- S1 — Original user request, verbatim: "merge cada unos ahora que sigue"; clarification selected "Ya integré todos, incluido #37 a main". This is a human merge report, not permission for additional remote actions.
- S2 — User answered "si" to "¿Autorizás que prepare esa recuperación local, sin publicar ni hacer merge todavía?". Only local recovery preparation and its necessary checks/documentation are authorized.

## Tasks

- T1 [S1-S2] — Confirm source/target provenance and prepare a new local recovery branch from the intact complete source, preserving all original branches and history. Route: inline local Git preparation. Status: in_progress. Risk: item 5 (integration/delivery). Commit: pending.
- T2 [S2] — Simulate integration without updating main or performing a merge; verify complete source preservation and run focused existing offline authentication checks on code proven identical to the candidate. Route: inline structural and deterministic checks. Status: pending. Commit: pending.
- T3 [S2] — Independently verify recovery boundaries, source equivalence, simulation and evidence without remote mutations or new audits of unchanged authentication logic. Route: independent verifier for item5. Status: pending. Commit: pending.
- T4 [S2] — Prepare the local handoff and proposed replacement integration PR, record observed proof/limits and remaining authorization/academic gates. Route: inline documentation only. Status: pending. Commit: pending.

## Log

- L1 — Original user request, verbatim: "merge cada unos ahora que sigue"; selected clarification: "Ya integré todos, incluido #37 a main".
- L2 — Agent explained that tracker37 merged before children, main contains only the coordinator document and the code is intact; asked "¿Autorizás que prepare esa recuperación local, sin publicar ni hacer merge todavía?". User answered, verbatim: "si". No push, PR creation, issue reopen/body edit, branch deletion/reset/force, main update or merge is authorized by this outcome.
- L3 — Read-only GitHub verification: PR37merged2026-11-03T23:43:37Z, before38-51(23:43:55Z–23:48:13Z). Main722e14d equals the coordinator-only tracker merge. Of91changed source paths from8f1e7e4tobb0452a,0final blobs match main; AuthenticationController, WebLogin and MobileAuthenticationService are absent. Tracker document matches5f03b18. The original complete feature commits remain intact locally.
- L4 — Fresh target observation after authorization: main still722e14d; issue36 nowCLOSED but retainsstatus:approved. Do not infer why it closed, academic acceptance, or permission to reopen it. Existing external SQL administrative/fixtures/TLS/browser/iPhone/Enzo blockers remain. API stays off; no private connection files, real SQL or seeds in recovery checks.
- L5 — Local approach: start a NEW fix/mvp-001-auth-integration-recovery branch at complete sourcebb0452a, preserve all original feature commit ancestry and all existing refs. Add only recovery tracking/report documentation in a normal single-parent commit, not a merge commit. Use git merge-tree solely as an integration simulation (objects only, no branch/main update). Prove all application/test/script code equals the complete source and that the simulated result preserves main's coordinator document. Existing primary worktree already has byte-identical application/test/script code and dependencies; focused checks may run there only after explicit equality proof, without copying private config or reinstalling packages. No new behavior requires a behavioral RED; observed main missing91source changes is the structural RED and full candidate/tree identity is the GREEN criterion.
