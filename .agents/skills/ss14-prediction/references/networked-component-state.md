<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Networked Component State

## Rules

- replicated predicted state belongs in shared components
- use `NetworkedComponent`, `AutoGenerateComponentState`, and `AutoNetworkedField` where appropriate
- dirty authoritative changes immediately

## Example Anchors

- `Content.Shared/Wieldable/Components/WieldableComponent.cs`
- `Content.Shared/Anomaly/Components/InnerBodyAnomalyComponent.cs`
