# MVP foundation subtasks

## Objective and authorized scope

Apply the approved decomposition of MVP-001 into five executable child tasks in the existing authoritative tasks array. Diego owns all five; Enzo reviews all five. No application implementation or task execution is authorized. The user subsequently authorized committing and publishing this documentation unit; no merge is authorized.

## Constraints

- Preserve every existing task field and ordering, including MVP-001 status, ID, creation metadata, acceptance criteria and evidence.
- Use optional parentId only on new child records. Derive children from parentId; no duplicate child list or automatic parent completion.
- New children start pending with null execution/completion fields and real system-clock createdAt values with explicit UTC offset.
- Every child explicitly depends on MVP-010; setup precedes typed contracts; contracts precede each independently executable role flow. No dependency on the parent that could create a cycle.
- Include authorization checks within each role-flow acceptance criteria; no endpoint, authentication library or implementation mechanism invented.
- Update operational guidance and current status summaries; distinguish 14 original milestones/tasks from 5 new executable children and avoid double-counting progress.
- Preserve .codegraph/ and all other pre-existing artifacts.
- Documentation/backlog-only change: no meaningful behavioral RED; verify JSON, preservation, hierarchy/dependency graph, timestamps, links, counts and whitespace instead.

## Tasks

- [x] SUB-1 — Add five child records and harmonize guidance and live summaries.
  - Acceptance: five pending child tasks owned by Diego/reviewed by Enzo; original records unchanged; parentId relationship and explicit parent completion rules documented; counts remain accurate.
- [x] SUB-2 — Independently verify the decomposition and prepare results.
  - Acceptance: complete original-object preservation, 19 unique IDs, valid dependencies/hierarchy with no cycles, explicit timestamp offsets, null lifecycle fields, accurate references/counts, project conventions and whitespace checks; all limitations recorded.

## Allowed implementation surfaces

- docs/tasks.json
- AGENTS.md
- README.md
- docs/project-report.md

## Progress

SUB-1 and SUB-2 complete. Baseline main: f92769c. Branch: docs/mvp-foundation-subtasks. Only pre-existing .codegraph/ was untracked before this unit. No commits or publishing performed.

Writer self-checks PASS: original 14 records preserved, five new children, JSON, IDs, hierarchy/dependency DAG, lifecycle nulls, counts, local links/anchors and whitespace. Clock command: `python -c "from datetime import datetime; print(datetime.now().astimezone().isoformat())"`; observed `2026-10-05T19:43:34.283590-03:00`, used for all five child createdAt fields. App tests/external links not applicable or omitted; human design review remains pending. Native assessment was unassessable due untracked scope, so independent verification applied.

Independent verifier PASS: metadata and all 14 original objects/order exactly unchanged; five children correctly scoped, assigned and gated; 19 records, 18 pending/one in_progress/no done; hierarchy and dependency DAG valid; 40 local links including 11 anchors passed; strict conventions/self-test and whitespace PASS. Timestamp provenance relies on the writer's observed clock evidence; external link not checked. App runtime tests inapplicable and Enzo review pending.

Native review `review-7e8f4c2dd095d881` approved the frozen candidate. Exact acknowledgement completed and burned authority for target `sha256:4dd5ba76925d285a3eff1ab8bc91ff362a34061b7675a8696e5a9dd5b57e7ba3`. This final bookkeeping update follows acknowledgement and is not part of that frozen candidate. The user subsequently authorized commit and remote publication. Commit/push/PR evidence follows; no merge is authorized.
