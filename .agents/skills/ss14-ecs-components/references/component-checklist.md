<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Component Checklist

## Purpose

Use this checklist when creating or reviewing a component class.

## Rules

- Components are data-only. Put behavior in a system.
- Use the narrowest correct assembly:
  - shared for replicated or predicted data
  - server for authority-only state
  - client for presentation-only state
- Prefer `EntityUid?` for optional entity references.
- Prefer `ProtoId<T>` or `EntProtoId` instead of raw prototype ID strings.
- Prefer `[DataField]` without a string name on new code unless compatibility requires a custom serialized key.
- Use `LocId` for localized message IDs stored in component data.

## Networking

- Shared networked components should typically use:
  - `[NetworkedComponent]`
  - `[AutoGenerateComponentState]`
  - `[AutoNetworkedField]` on synced fields
- Do not mark purely server-only or purely client-only components as networked.

## Example Anchors

- `Content.Shared/Wieldable/Components/WieldableComponent.cs`
- `Content.Shared/Abilities/Mime/MimePowersComponent.cs`

## Common Mistakes

- Putting helper methods or business logic inside the component.
- Adding network sync to fields that never need to be seen by clients.
- Storing raw prototype IDs or localized literal strings in new component fields.
