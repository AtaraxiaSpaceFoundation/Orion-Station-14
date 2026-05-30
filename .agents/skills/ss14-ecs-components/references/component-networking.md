<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Component Networking

## Use This When

- the component is shared
- state must replicate to the client
- prediction reads the field on both sides

## Standard Shape

- `[NetworkedComponent]`
- `[AutoGenerateComponentState]`
- `[AutoNetworkedField]` on synced fields

## Example Anchors

- `Content.Shared/Wieldable/Components/WieldableComponent.cs`
- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`

## Do Not

- mark a purely server-only or client-only component as networked
- sync fields the client never reads
- forget to dirty authoritative changes
