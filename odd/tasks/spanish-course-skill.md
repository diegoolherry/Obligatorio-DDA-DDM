# Feature: Spanish Course Skill

## Objective

Translate the project-local course conventions skill into professional Spanish while preserving its behavior, structure, references, templates, and validation support.

## Problem and why

The skill was created from Spanish-language course material but its instructions and references are in English. That weakens its usefulness for the students and misaligns it with the classroom terminology it is intended to preserve.

## Scope

- Translate the human-readable skill entry point, references, and repository activation guidance into Spanish.
- Translate example UI text in the assets where it supports the Spanish course context.
- Preserve file paths, frontmatter keys, command-line interfaces, code identifiers, and checker behavior.
- Do not change product behavior or the MVP document.

## Constraints

- Preserve valid YAML frontmatter and stable skill name.
- Keep code identifiers technical and consistent; translate only user-facing example strings.
- TDD: disabled/unconfigured; validate structure and checker behavior.
- RDD: disabled/unmanaged.
- Delivery: `ask-on-risk`; forecast about 300 authored changed lines, below the chaining guideline.

## Tasks

- [x] **SCS-1 — Localize the course skill**
  - Translate the skill documentation and example UI copy, then validate the result.
  - Acceptance: the skill is readable in Spanish without changing its activation behavior or validation script.
  - Checks: official skill validator, checker self-test and strict scan, manual search for remaining English instructional headings, `git diff --check`.

## Progress and evidence

- Branch: `codex/project-course-skill`
- Current task: complete
- Work-unit commits: pending
- Running authored changed lines: pending commit measurement
- SCS-1 evidence: translated the entry point, four references, repository guidance, checker messages, and example UI copy; preserved skill name, file paths, YAML keys, code identifiers, and script flags. The checker compilation, self-test, strict scan, and whitespace check passed.
- Next step: use the Spanish skill as the convention source for subsequent implementation.
