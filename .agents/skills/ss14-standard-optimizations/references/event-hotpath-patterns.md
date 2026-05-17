<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Event Hot Path Patterns

## Prefer

- thin handlers
- early returns
- existing refresh events over new polling loops
- avoiding allocations in high-frequency paths

## Example Anchor

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
