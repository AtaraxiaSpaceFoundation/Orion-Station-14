<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# LocId And Component Fields

## Use `LocId` For Stored Keys

- Prefer `LocId` when component or prototype-backed data stores a reusable localization key.
- This is especially useful for popup text, examine text, deny messages, and feature-configured labels.

## Example Anchor

- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`

## Related Field Style

- Keep serialized keys on `[DataField]` members.
- Pair `LocId` with `ProtoId<T>` or `EntProtoId` instead of raw strings when the same component also stores prototype references.

## Do Not

- Store fully formatted player text in serialized fields when the feature only needs a localization key.
- Mix raw English text and `LocId` fields in the same pattern without a strong reason.
