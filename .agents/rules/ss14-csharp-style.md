<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 C# Style

Use this rule for new gameplay C# in `Content.Shared/`, `Content.Server/`, `Content.Client/`, and tests.

## Naming

- Event handlers use `On...`.
- Public action methods use `Try...` when they can fail.
- Check methods use `Can...`.
- Dependency fields use the underscore style such as `_popup`, `_audio`, `_hands`.

## Entity APIs

- Prefer `Entity<T?>` when the caller already has a uid/component pair.
- Call `Resolve(...)` early in public system methods that accept optional component pairs.
- Use `EntityUid?` for optional references instead of `EntityUid.Invalid`.

## Data And Prototypes

- Prefer `[DataField]` without string field names on new code unless compatibility requires a custom serialized key.
- Prefer `ProtoId<T>` or `EntProtoId` over raw prototype ID strings.
- Keep localized identifiers as `LocId` when they are stored in component or prototype-backed data.

## Interaction Flow

- Route handlers through `On... -> Try... -> Can... -> Do...`.
- Keep handlers thin and reusable.
- Do not mutate game state inside `Can...`.

## Dependencies

- Prefer `[Dependency]` fields over `IoCManager.Resolve(...)` in method bodies.
- Reuse nearby system APIs before inventing new events or helper layers.
