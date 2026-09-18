# Mobile UX and Figma handoff

## Design sequence

Work in this order: problem, user flow, wireframe, mockup, prototype. A screen is not complete because it looks polished; it must advance a user goal or support a clear decision.

- Define context, goal, constraints, and a verifiable success condition.
- Draw the happy path and important error/offline paths before screens.
- Give each screen a clear purpose and one dominant action.
- Prototype tasks rather than the whole application, then test without telling the user where to tap.
- Organize Figma with frames, pages, reusable components, variants, and Auto Layout. Translate frames to `View`, text to `Text`, Auto Layout direction to Flexbox, and variants to props/state.

## Mobile interface rules

- Respect safe areas and system controls.
- Use a consistent spacing scale based on 4 or 8.
- Make interactive targets about 48 by 48 density-independent units when practical.
- Maintain visible pressed, loading, success, disabled, and error feedback.
- Do not communicate status with color alone. Target at least 4.5:1 contrast for normal text and 3:1 for large text.
- Ask only for necessary form data; use the matching keyboard; preserve values after an error; validate near the field without interrupting every keystroke.
- Make permissions explicit and design for delayed, interrupted, or absent connectivity.

## Handoff checklist

- Main task is understandable without explanation.
- Loading, empty, error, offline, and success states exist where applicable.
- Components and spacing are reusable and consistent.
- Destructive actions require confirmation or a practical undo path.
- Design decisions can be explained from user, task, and context evidence.

## Source basis

Derived from the supplied `Figma_to_React_Native.pdf` and `Mobile_UX_UI_Blueprint.pdf`.

