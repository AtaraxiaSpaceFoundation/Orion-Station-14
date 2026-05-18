<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Shared Server Client Split

## Shared

- prediction-aware interaction logic
- networked state and shared events
- code both sides must understand

## Server

- authoritative commits
- server-only side effects
- round or persistence logic

## Client

- presentation
- overlays and windows
- client-only polish after replicated state arrives

## Example Anchor

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `Content.Server/Wieldable/WieldableSystem.cs`
- `Content.Client/Wieldable/WieldableSystem.cs`
