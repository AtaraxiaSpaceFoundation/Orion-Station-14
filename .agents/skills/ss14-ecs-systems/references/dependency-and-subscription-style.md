<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Dependency And Subscription Style

## Dependencies

- Prefer `[Dependency]` fields.
- Keep dependency names explicit and underscore-prefixed.
- Reuse nearby system APIs before adding a new helper layer.

## Subscriptions

- Subscribe in `Initialize()`.
- Keep `On...` handlers thin.
- Route to reusable public methods instead of embedding all logic in the handler.

## Example Anchor

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
