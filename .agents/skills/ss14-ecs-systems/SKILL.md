<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ecs-systems
description: Create or modify SS14 entity systems in C#. Use when implementing gameplay logic, subscriptions, dependencies, public system APIs, or cross-assembly system placement for shared/server/client mechanics.
---

# SS14 ECS Systems

Use this skill when touching `EntitySystem` classes or moving behavior out of components.

## Workflow

1. Open `references/system-patterns.md`.
2. Open `references/component-system-example.md` for a concrete pattern.
3. Open `references/dependency-and-subscription-style.md` for wiring style.
4. Open `references/shared-server-client-split.md` when the mechanic spans assemblies.
5. Open `references/try-can-do-pattern.md` for the standard public-action flow.
3. Place the system in the correct assembly.
4. Keep handlers thin and route actions through reusable public APIs.

## Reference Map

- `references/system-patterns.md`
- `references/component-system-example.md`
- `references/dependency-and-subscription-style.md`
- `references/shared-server-client-split.md`
- `references/try-can-do-pattern.md`
- `../ss14-gameplay-feature/references/open-space-gameplay-map.md`
- `../ss14-ecs-basics/references/ecs-primer.md`
- `../ss14-ecs-basics/references/simple-system-walkthrough.md`
