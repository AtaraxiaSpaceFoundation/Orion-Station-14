<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# NetEntity And Replicated State

## `NetEntity`

- Use `NetEntity` in network payloads instead of raw `EntityUid`.
- Convert at the shared system boundary where the project pattern already does so.

## Example Anchors

- `Content.Shared/Actions/ActionEvents.cs`
- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`

## State Rules

- Shared replicated data belongs in shared networked components.
- Use `AutoGenerateComponentState` and `AutoNetworkedField` for synced fields where the pattern fits.
- Dirty replicated state whenever authoritative values change.

## Common Mistakes

- Reusing server-only state as if the client can already read it.
- Adding reference-heavy fields to replicated state without checking prediction safety.
- Using a network event when a replicated field or existing shared state would be simpler.
