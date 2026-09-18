---
name: ctc-project-conventions
description: Apply the course conventions for Blazor, C#, APIs, React Native, TypeScript, Expo, and mobile UX in this repository. Use when planning, implementing, debugging, or reviewing .razor, .cs, .ts, or .tsx files, forms, components, services, hooks, lists, HTTP states, or Figma-to-native work.
---

# CTC Project Conventions

## Activation Contract

1. Read `references/shared-workflow.md`.
2. Read `references/dda-blazor.md` for Blazor/C# work or `references/dam-react-native.md` for mobile work.
3. Read `references/mobile-ux.md` before creating or changing a user flow or screen.
4. Treat the course rubric and current project configuration as authoritative when they conflict with this skill; report the conflict and update the skill only with user approval.

## Hard Rules

- Preserve strict responsibility boundaries: UI renders and dispatches events; services/actions own business or data operations; models/types define contracts.
- Register Blazor services through dependency injection and inject them; never construct a service in a Razor component.
- Use typed contracts. Do not introduce TypeScript `any` to silence errors.
- Never mutate React state directly or edit a shared Blazor model accidentally; create a new or copied value.
- Model loading, empty, error, success, and retry where remote data is involved.
- Use native React Native elements, not HTML elements, and keep visible text inside `Text`.
- Add no dependency merely to avoid understanding or implementing a small course concept.
- Keep each change small, compilable, and explainable by both students.

## Decision Gates

- **Blazor:** decide page versus reusable component, then UI versus service responsibility, before writing code.
- **React Native:** decide props versus local state; use Context only for genuinely global state and effects only for external systems.
- **Collections:** use LINQ in C#; use stable keys and `FlatList` for mobile collections that can grow.
- **Forms:** define the typed model and validation rules before UI fields.
- **UX:** define the user goal, happy path, failure paths, permissions, and connectivity behavior before visual polish.
- **API:** keep clients dependent on explicit request/response contracts, not server entities. Until API course material or the rubric defines more, avoid inventing project policy.

## Execution Steps

1. Inspect nearby code and identify the applicable reference rules.
2. Write the user flow and interface states for behavior that crosses screens or the network.
3. Implement the smallest coherent vertical slice with typed boundaries.
4. Validate using the project build/tests plus `scripts/check_project_conventions.py` when available.
5. Report assumptions, skipped checks, and any intentional deviation from the course conventions.

## Output Contract

For implementation or review, state the affected layer, files changed, checks run, and unresolved assumptions. Explain architectural choices in course terms rather than naming patterns without justification.

## References

- `references/shared-workflow.md`
- `references/dda-blazor.md`
- `references/dam-react-native.md`
- `references/mobile-ux.md`
- `assets/blazor-form-pattern.razor`
- `assets/react-native-async-list-pattern.tsx`

