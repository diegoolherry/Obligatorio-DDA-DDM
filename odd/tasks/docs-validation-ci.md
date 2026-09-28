# Documentation validation CI

## Goal

Add a minimal automatic pull-request check for documentation changes so the data-model draft PR can be merged only after an observed passing result. This is an infrastructure/process prerequisite, not approval of the data model.

## Tasks

- [x] Add a GitHub Actions PR workflow that validates backlog JSON and changed-file whitespace without installing dependencies. Local evidence: `python -m json.tool docs/mvp-tasks.json > /dev/null` and `git diff --check` exited 0; the pre-commit check against `main HEAD` had an empty diff.
- [x] Verify workflow in [PR #4](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/4), linked to approved issue #3; `validate` completed `SUCCESS`, then PR #4 merged as `8dcd919b`.
- [ ] Update data-model [PR #2](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/2) against main, observe its passing check, then merge the draft without marking MVP-010 done.

## Scope and routing

Authorized by user's selection to add a minimal check then merge. Approved scope issue: [#3](https://github.com/diegoolherry/Obligatorio-DDA-DDM/issues/3). Separate branch `ci/docs-validation` from main; one writer for workflow and process documentation. Allowed prospective source surfaces: `.github/workflows/docs-validation.yml`, `docs/project-report.md`. Do not touch `.codegraph/`. Parent handles issues, commits, PR and merge. TDD: no application test runner; runtime harness is the GitHub PR workflow itself. Local checks: JSON parse and diff whitespace. Delivery strategy single PR, forecast under 100 authored lines. Rollback boundary: this workflow and its process note only. Human cross-review of the domain model remains pending.
