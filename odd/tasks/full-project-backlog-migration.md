# Full-project backlog migration

## Objective

Replace the MVP-named backlog path with one authoritative backlog for the complete academic project, while preserving every existing task and its history.

## Authorized scope

- Migrate `docs/mvp-tasks.json` to `docs/tasks.json`.
- Keep one `tasks` array for MVP and final-delivery work.
- Normalize task classification to `API`, `Web`, `Mobile`, and `Docs` without splitting cross-cutting tasks.
- Update live operational and documentation references to the new path and scope.
- Preserve all existing task IDs, creation timestamps, lifecycle state, ownership, dependencies, acceptance criteria, and evidence.

## Constraints

- Existing tasks are migrated, not deleted, recreated, completed, reassigned, or given new timestamps.
- Historical ODD records remain historical unless they are operational instructions.
- Current uncommitted payment-decision changes must be verified and preserved before backlog migration writes begin.
- `.codegraph/` is pre-existing and out of scope.
- No commit or push without explicit user authorization.
- Documentation-only migration: test-first development has no meaningful behavioral RED; use structural, reference, JSON, and diff verification instead.

## Tasks

- [x] **BLG-1 — Verify and preserve the pending payment documentation candidate**
  - Acceptance: independent checks confirm JSON validity, clean whitespace, and that only `MVP-010.evidence` changed within the existing backlog relative to `HEAD`.
  - Evidence: independent verifier PASS; `git diff --check` was clean, both JSON versions parsed, and whole-object comparison after restoring `MVP-010.evidence` matched `HEAD`.
- [x] **BLG-2 — Migrate the authoritative backlog without losing task history**
  - Acceptance: `docs/tasks.json` contains every existing task exactly once; immutable and lifecycle fields are preserved; classifications use only `API`, `Web`, `Mobile`, and `Docs`.
- [x] **BLG-3 — Update live references and repository guidance**
  - Acceptance: operational instructions, CI/scripts, and current documentation resolve to `docs/tasks.json`; historical records are not rewritten as if they had used the new path.
- [x] **BLG-4 — Verify the migration and prepare delivery evidence**
  - Acceptance: JSON/schema invariants, dependency integrity, reference resolution, changed-file whitespace, and repository-specific checks pass; skipped checks are explicit.

## Progress and evidence

- Current branch: `main`, synchronized with `origin/main` before this unit.
- Pre-existing working tree: modified `docs/design/data-model.md`, `docs/mvp-tasks.json`, and `docs/project-report.md`; untracked `.codegraph/` and `odd/tasks/payment-retry-decision.md`.
- BLG-1 complete: the pending payment documentation candidate is structurally valid and safely bounded to `MVP-010.evidence` within the backlog.
- Migration mapping complete: 14 tasks observed (10 MVP, 4 FIN); 13 `pending`, MVP-010 `in_progress`; live references and CI validation identified.
- BLG-2 complete: independent verifier PASS; 14 unique ordered IDs preserved, complete object equality against normalized HEAD plus exact pre-migration payment evidence passed; JSON has no duplicate keys, approved areas only, and clean whitespace. Only metadata.nombre and classification changed during migration. Writer's initial replacement script failed before writing; corrected script passed.
- BLG-3 complete: writer and independent verifier observed all seven live surfaces on the new path; 59 local links/anchors resolved; stale references are historical ODD only. JSON, strict project conventions, checker self-test, and whitespace checks passed. Remote CI has not run.
- All four migration tasks complete. Branch: `docs/full-project-backlog`.
- Verification limitation: no retained pre-BLG-3 byte snapshot of the data-model document exists; independent temporal byte preservation cannot be asserted for that document. Prior payment verification and current substantive consistency checks remain evidence.
- Native assessment after BLG-2/3 was unassessable because untracked files needed explicit scope; independent verification applied. Final independent verifier PASS: ordered IDs, exact classification mapping, dependency DAG, offset timestamps, null completion fields, opposite reviewers, JSON, 59 local links/anchors, strict conventions/self-test and whitespace.
- Native review `review-64a7b3f3fc677a16` approved the frozen candidate with four lenses. Exact acknowledgement completed, authority burned, target `sha256:67634680aa21a0c3a870a3be60785919293098138f5aeb51cb9114d618fb9beb`. Nonblocking readability advisory `R2-001` at `docs/tasks.json:287` is separate future work; no correction offered. This final tracker update records results after acknowledgement and is not part of that frozen candidate.
- First START returned expired-consent diagnostic without creating a lineage; fresh provider-offered START succeeded. No authority recovery/reset or delivery command was run.
- Unverified: remote CI, provider behavior, Mermaid rendering, human review, and temporal byte preservation of the pre-BLG-3 model document. No application behavior was changed; deterministic behavioral RED was not applicable.
- Additional hold documentation was independently verified before delivery: five configurable minutes from API creation, unchanged expiry on reload/retry, pending/unknown payment blocking after expiry, and authoritative compensation tracking. JSON, invariants, 57 local links/anchors, strict conventions, and whitespace passed. The earlier native approval does not cover this later hold-policy candidate.
- Next step: confirm remote/session authorization and approved issue linkage before push/PR. No push, PR, or merge performed.
- User subsequently authorized local commits and confirmed the additional hold policy. Work-unit commits: `dcf8460` (`docs(backlog): migrate to full-project task backlog`) and `1343490` (`docs(payments): record hold and retry agreement`). Shared files were staged by unit without discarding working-tree payment changes. `.codegraph/` remains outside delivery.
