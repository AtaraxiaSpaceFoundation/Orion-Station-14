<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Event Patterns

## Purpose

Use this file when deciding whether a new behavior should be an event, a relayed event, or a direct public system method.

## Rules

- Prefer public system methods over method events when a direct call is simpler and clearer.
- Use by-ref events for cancellable or mutable flows.
- Keep events feature-scoped and explicitly named.
- Put shared events in `Content.Shared/` if both sides must understand them.

## Naming

- `Attempt...Event` for cancellable attempts
- `...edEvent` for completed actions
- `Get...Event` for data collection or modifier aggregation

## Common Mistakes

- Creating a new event when only one system will ever call the behavior.
- Hiding public gameplay API inside ad-hoc event traffic.
- Putting server-only event types in shared assemblies.
