<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Interaction Flow

Use the repo-standard interaction flow for actions, verbs, in-hand use, BUI actions, and other entity-driven gameplay APIs.

## Required Shape

- `OnEvent(...)`: event entry point
- `TryDoSomething(...)`: public action API
- `CanDoSomething(...)`: checks
- `DoSomething(...)` or execution section in `TryDoSomething(...)`: mutation

## Handler Rules

- Keep `On...` handlers thin.
- Handle `args.Handled`, cancellation flags, or obvious routing conditions there.
- Forward to `Try...` instead of embedding full gameplay logic in the handler.

## Try Rules

- `Try...` is the reusable public API.
- `Try...` should call `Can...` itself instead of assuming the caller already checked.
- Return `bool` success/failure whenever the action is a standard gameplay attempt.

## Can Rules

- `Can...` checks conditions only.
- Do not mutate component or entity state inside `Can...`.
- It is acceptable to emit popup feedback when `quiet == false`.
- Prefer `bool quiet = false` when the caller may want a silent check.

## Example Anchor

See `Content.Shared/Wieldable/SharedWieldableSystem.cs` for a real in-repo example of:

- `OnUseInHand(...)`
- `TryWield(...)`
- `CanWield(...)`
- predicted popup and audio usage around the action

## Anti-Patterns

- Thick `OnEvent(...)` handlers that cannot be reused from verbs, admin actions, or other systems.
- `Can...` methods that decrement ammo, change visuals, or otherwise mutate game state.
- `Try...` methods that skip checks and trust the caller.
- Returning only strings or error codes instead of a simple `bool` action result.
