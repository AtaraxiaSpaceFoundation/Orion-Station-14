<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Component Example: Inner Body Anomaly

## Anchor

- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`

## Why It Is Useful

- mixes `EntProtoId`, `LocId`, `SpriteSpecifier`, and `ProtoId<T>` in one real shared component
- shows a networked shared component with a focused access attribute
- keeps data in the component and behavior elsewhere

## What To Copy

- field-type choices
- localized identifier storage
- replicated visual fields only where the client needs them

## What Not To Copy Blindly

- exact gameplay data just because the field mix looks convenient
