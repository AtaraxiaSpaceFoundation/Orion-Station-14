<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# System Patterns

## Purpose

Use this file to structure SS14 gameplay systems cleanly.

## Rules

- Keep behavior in systems, not components.
- Prefer `SubscribeLocalEvent` handlers that forward into reusable methods.
- Use `[Dependency]` fields for other systems.
- Keep shared, server, and client responsibilities split by ownership and prediction requirements.

## Public API Pattern

- `OnEvent(...)` routes
- `Try...(...)` is the reusable public action
- `Can...(...)` checks
- `Do...(...)` or the execution block mutates state

## Example Anchors

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `Content.Server/Wieldable/WieldableSystem.cs`
- `Content.Client/Wieldable/WieldableSystem.cs`

## Common Mistakes

- Business logic inside the event handler.
- New one-off helper methods instead of stable public system APIs.
- Shared code depending directly on client-only or server-only details.
