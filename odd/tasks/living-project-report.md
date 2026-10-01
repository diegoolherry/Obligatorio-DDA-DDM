# Feature: Living project report

## Objective

Create an evolving Markdown source for the future Word project delivery and establish a repository rule that keeps it aligned with important verified changes.

## Scope and constraints

- Create `docs/project-report.md` as the maintained source document.
- Update `AGENTS.md` with a precise maintenance rule for changes that affect documented facts.
- Base the initial structure on the previous Engineering Software third delivery while adapting it to this project's existing documentation and incremental state.
- Apply the cognitive document design principles: lead with the current outcome, progressive disclosure, short sections, tables, checklists, and review-oriented evidence.
- Link existing canonical documents instead of duplicating unstable detail unnecessarily.
- Mark incomplete claims as pending; never present planned architecture, deployment, testing, or functionality as completed.
- Keep Word export outside this task. The Markdown source will be converted near the academic delivery.
- Do not change `docs/mvp-tasks.json` because this is delivery-document maintenance rather than an MVP implementation slice.
- No application tests apply. Verify Markdown structure, internal links, consistency against existing documents, and `git diff --check`.
- No commit or push without explicit user authorization.

## Tasks

- [x] **REPORT-1 — Draft the living report**
  - Acceptance: `docs/project-report.md` provides a useful current-state summary and an incremental structure for problem, requirements, project plan, decisions, feasibility, team, tools, iterations, quality, testing, configuration management, deployment, conclusions, and references.
  - Verification: the drafted outline was read back against the previous third delivery and current repository documents; it preserves canonical links and marks unimplemented work explicitly.
- [x] **REPORT-2 — Add the maintenance rule**
  - Acceptance: `AGENTS.md` states when the report must be reviewed and updated, requires evidence-based wording, and preserves Markdown as the source before Word conversion.
  - Verification: readback confirms the rule covers architecture, scope, technology, MVP completion, testing, deployment, risks, schedule and team process without weakening backlog authority.
- [x] **REPORT-3 — Verify consistency and reviewability**
  - Acceptance: links resolve, current decisions are accurate, pending items are explicit, and Markdown checks pass.
  - Verification: independent review found no blockers and confirmed the report categories, nine `pending` MVP tasks, relative links/anchors and the `AGENTS.md` maintenance contract. One ambiguity was corrected so the verified local `root` connection is distinct from pending application/EF Core integration. Re-verification passed direct whitespace checks for both untracked Markdown files and `git diff --check -- AGENTS.md`; only the existing LF→CRLF warning remains.

## Progress and evidence

- Started at `2026-09-25T00:58:01-03:00` after explicit user authorization to create both files.
- Source reference inspected read-only: previous third-delivery Word document for the Software Engineering course (local, not versioned; private path omitted).
- Applicable skill: `cognitive-doc-design` (installed locally; private path omitted).
- REPORT-1 and REPORT-2 were implemented by the bounded writer and read back by the parent. Writer checks found 17 repository-file links resolving and all nine MVP tasks still `pending`.
- Native risk assessment was unavailable because untracked files require explicit review scope; the returned plan required an independent verifier.
- REPORT-3 completed at `2026-09-25T01:03:39-03:00`: independent verification and focused re-verification passed with no blockers. Git reports an LF→CRLF conversion warning for `AGENTS.md`, not a whitespace error.
- Work-unit commit: `c0feb242cfceb7e554ab94523dac18ae990f977e` (`docs: add living project report`) contains `AGENTS.md`, the reconciled MySQL decision in `docs/mvp.md`, `docs/project-report.md`, and this tracking document. The user explicitly authorized direct delivery to `origin/main`; `.codegraph/` remains excluded.
