<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ecs-entities
description: Work with entities, entity-system APIs, and entity/component access patterns in SS14 C#. Use when designing public system method signatures, choosing between EntityUid and Entity<T>, or resolving components safely in gameplay code.
---

# SS14 ECS Entities

Use this skill when a task is about entity ownership, entity-system method signatures, `Resolve(...)`, `TryComp(...)`, or safe entity/component access.

## Workflow

1. Open `references/entity-api-patterns.md`.
2. Prefer `Entity<T?>` when the call site already has the component pair.
3. Resolve optional components early in the method.
4. Keep owner, user, used, target, and performer naming consistent with nearby systems.

## Reference Map

- `references/entity-api-patterns.md`
- `../ss14-ecs-basics/references/ecs-primer.md`
- `../ss14-common-api-patterns/references/entitysystem-functions.md`
