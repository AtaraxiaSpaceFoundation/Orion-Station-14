<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prediction And Cross-Assembly

## Purpose

Use this file when a gameplay change crosses shared, server, client, or UI code and must still feel immediate to the player.

## Prediction Placement Rules

- Put predicted logic in `Content.Shared/`.
- Keep authoritative state changes on the server, but mirror the data shape in shared networked components.
- Only mark shared components as `NetworkedComponent`.
- Dirty networked state every time the authoritative value changes.
- Use `DirtyField(...)` when only one field changed and field deltas are already the project pattern.

## Typical Predicted Flow

1. Shared component stores predicted or networked state.
2. Shared system exposes the interaction entry point and shared event types.
3. Server system validates and commits the action.
4. Client system or UI reads replicated state and applies local presentation.
5. Prototypes and locale provide the data and user-facing strings.

## BUI Flow

For bound UIs, expect some combination of:

- shared component state or shared BUI state type in `Content.Shared/`
- server-side BUI or system handling messages in `Content.Server/`
- client window, `.xaml`, `.xaml.cs`, or bound UI class in `Content.Client/`

Prefer reading already-networked component state on the client instead of inventing duplicate BUI state when the existing pattern supports it.

## APIs To Prefer

- `PopupPredicted`, `PopupClient`
- `PlayPredicted`
- predicted entity spawn and delete helpers
- predicted BUI messaging such as `SendPredictedMessage`
- `IGameTiming.IsFirstTimePredicted` for local repeated execution guards

## Common Mistakes

- Leaving an interaction server-only even though the player expects instant feedback.
- Writing predicted code that calls server-only APIs.
- Forgetting that client-side predicted code may run more than once.
- Changing a networked field without dirtying it.
- Adding `NetworkedComponent` to a client-only or server-only component.
- Packing gameplay logic into a component because the change looked "small".

## Routing Heuristics

- If the task is about rules, spawning, jobs, events, persistence, or admin authority, start in `Content.Server/`.
- If the task is about state, events, prediction, or components that both sides must understand, start in `Content.Shared/`.
- If the task is about windows, overlays, alerts, animation, or visual polish, start in `Content.Client/`.
- If a feature touches player-facing text, content tuning, sounds, or markups, open `Resources/` in the same pass.

## Validation Matrix

- Gameplay logic only: `dotnet build --configuration DebugOpt --no-restore /m`
- Unit/content behavior: `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj`
- Integration or end-to-end roundflow: `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj`
- Prototype or locale edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- RSI edits: `py -3 Schemas/validate_rsis.py Resources`
- Predicted or UI behavior: perform an in-game pass when possible and say explicitly when you could not

## Related References

- `../../ss14-client-server-shared/references/shared-and-prediction.md`
- `../../ss14-client-server-shared/references/networking-and-dirty.md`
- `../../ss14-netcode/references/network-event-patterns.md`
