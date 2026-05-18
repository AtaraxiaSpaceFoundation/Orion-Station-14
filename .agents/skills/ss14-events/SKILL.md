<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-events
description: Create or review SS14 events and event-driven gameplay flow. Use when adding shared events, by-ref events, directed or relayed events, or deciding whether logic should be a public system method instead of a new event.
---

# SS14 Events

Use this skill when a task adds or changes event types, subscriptions, relays, or event-driven architecture.

## Workflow

1. Open `references/event-patterns.md`.
2. Prefer public system methods over method events when a direct API is clearer.
3. Use by-ref events for cancellable or mutable flows when that is already the project pattern.
4. Keep event names explicit and feature-scoped.

## Reference Map

- `references/event-patterns.md`
- `../ss14-ecs-basics/references/event-reading-guide.md`
- `../ss14-client-server-shared/references/networking-and-dirty.md`
