# Feature: MVP Task Distribution

## Objective

Create a shared, machine-readable MVP backlog for Diego and Enzo and define repository rules that keep task ownership and timestamps accurate.

## Scope

- Derive implementation tasks from `docs/mvp.md`.
- Assign vertical slices between Diego and Enzo with cross-review.
- Record stable IDs, status, dependencies, acceptance criteria, ownership, timestamps, and completion evidence.
- Update `AGENTS.md` with rules for maintaining the backlog.
- Do not implement the application itself.

## Constraints

- Keep the backlog valid JSON.
- Use ISO 8601 timestamps with the repository team's local UTC offset.
- Never fabricate completion metadata or mark work done before verification.
- Preserve the existing project-skill requirement in `AGENTS.md`.
- The initial planning request did not authorize a commit; the user later explicitly authorized committing and pushing all current changes to `main`.

## Tasks

- [x] **PLAN-1 — Create the shared MVP backlog**
  - Add the JSON task list derived from the MVP.
  - Acceptance: tasks cover the recommended construction order, have balanced vertical ownership, explicit dependencies, cross-review, and verifiable acceptance criteria.
  - Checks: parse the JSON and inspect task coverage against `docs/mvp.md`.

- [x] **PLAN-2 — Define backlog maintenance rules**
  - Extend `AGENTS.md` with timestamp, status, ownership, evidence, and verification rules.
  - Acceptance: future contributors can update tasks without rewriting creation metadata or inventing completion data.
  - Checks: inspect the instructions for consistency with the JSON schema and run `git diff --check`.

## Progress and evidence

- Branch: `main`
- Current task: complete and delivered to a local commit.
- Work-unit commits: PLAN-1 and PLAN-2 `9aae46b` (`docs: add shared MVP task backlog`).
- Existing unrelated working-tree change: `.gitignore` is staged and remained untouched.
- PLAN-1 evidence: `docs/mvp-tasks.json` contains eight ordered vertical slices, an even 4/4 assignment, opposite reviewers, valid dependency IDs, explicit acceptance criteria, and nullable execution/completion evidence. Independent JSON/schema and MVP-coverage verification passed.
- PLAN-2 evidence: `AGENTS.md` defines valid status transitions, dependency handling, immutable creation metadata, timestamp rules, blockers, evidence, and cross-review. Independent consistency inspection and `git diff --check` passed.
- Verification timestamp: `2026-09-21T17:49:05.635464-03:00`; all creation timestamps use the same `-03:00` offset and preceded verification by about four minutes.
