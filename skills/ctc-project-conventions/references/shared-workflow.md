# Shared implementation workflow

## Before coding

- Start from the user's goal and the observable result, not from framework components.
- Trace the happy path and relevant failures. For networked flows include latency, no connection, server failure, empty data, success, and retry.
- Define contracts first: C# models/DTOs and TypeScript types must describe the same API meaning without exposing persistence entities.
- Prefer one small vertical slice that can be demonstrated over several disconnected layers.

## Responsibility boundaries

| Concern | Owner |
| --- | --- |
| Rendering and user events | Razor component or React Native component |
| Reusable UI state/behavior | Child component or custom hook |
| Business/data operation | Blazor service or mobile action/service |
| Remote boundary | API client and explicit DTOs |
| Validation | Typed model plus UI feedback |

Keep names intention-revealing. Use PascalCase for public C# types, properties, and methods; camelCase for local TypeScript values and private component state; prefix private C# fields with `_` when the surrounding project follows the class examples.

## Working rhythm

1. Compile or run from a known-good state.
2. Change one concept at a time.
3. Read the first relevant error, not only the last line.
4. Verify the responsible layer: build, service/action, dependency injection or imports, event, then state/rendering.
5. Return to the last working state instead of stacking speculative fixes.

Do not add abstractions, packages, or global state until a concrete repetition or requirement justifies them.

