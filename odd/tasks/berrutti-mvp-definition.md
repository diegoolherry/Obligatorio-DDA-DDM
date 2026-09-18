# Feature: Berrutti MVP Definition

## Objective

Create a clear Markdown definition of the academic MVP for purchasing, managing, and validating Berrutti tickets and travel passes through Blazor, an API, and React Native with Expo.

## Problem and why

The product idea and several real-world rules are known, but there is no shared scope document. Without one, two students could implement disconnected features or silently turn uncertain business rules into requirements.

## Scope

- Define actors, goals, functional scope, key flows, business rules, states, tentative domain model, system boundaries, acceptance scenarios, exclusions, risks, and open questions.
- Distinguish confirmed observations from academic simplifications and unverified assumptions.
- Keep the document independent of database, authentication, and QR library choices until the assignment rubric exists.
- Do not implement application code.

## Constraints

- Academic prototype only; no affiliation with or deployment for Berrutti.
- Payment is simulated and must not collect real financial data.
- Follow `skills/ctc-project-conventions/SKILL.md` and its UX workflow.
- TDD: disabled/unconfigured; this is documentation-only work.
- RDD: disabled/unmanaged.
- Delivery: `ask-on-risk`; forecast about 350 authored changed lines, below the chaining guideline.

## Tasks

- [x] **MVP-1 — Write and verify the MVP definition**
  - Create `docs/mvp.md` in professional Spanish.
  - Acceptance: a teammate can identify what to build, what not to build, how the demo succeeds, and which rules remain undecided.
  - Checks: compare against recorded product decisions; inspect Markdown structure; run `git diff --check`.

## Progress and evidence

- Branch: `codex/project-course-skill`
- Current task: complete
- Work-unit commits: MVP-1 `50e88ac` (`docs: define Berrutti academic MVP`)
- Running authored changed lines: 380
- MVP-1 evidence: `docs/mvp.md` separates confirmed observations, academic decisions, and open questions; contains 19 scannable sections, 13 business rules, 10 acceptance scenarios, explicit exclusions, and a definition-of-done checklist. Structure checks and the project convention checker passed.
- Next step: compare this baseline with the official assignment when it is published.
