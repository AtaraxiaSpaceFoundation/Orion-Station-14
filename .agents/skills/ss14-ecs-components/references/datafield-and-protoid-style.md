<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# DataField And ProtoId Style

## Rules

- Prefer `[DataField]` without a string name on new serialized config fields unless compatibility requires a custom serialized key.
- Keep runtime-only or `readonly` cache fields free of `[DataField]`; `[DataField]` is for serialized content/config.
- Prefer `ProtoId<T>` for non-entity prototype types and `EntProtoId` for entity prototype IDs instead of raw strings.
- Use `LocId` for stored localization identifiers.

## Example Anchor

- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`

## Common Mistakes

- adding raw string prototype IDs to a new component
- serializing temporary runtime caches
- mixing localized literal text and `LocId` in the same pattern
