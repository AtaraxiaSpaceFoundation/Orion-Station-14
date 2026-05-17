<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ecs-components
description: Create or modify SS14 ECS components in C#. Use when adding or reviewing component classes, component data fields, networked component state, serialization fields, or deciding what belongs in a component versus a system.
---

# SS14 ECS Components

Use this skill when touching `Component` classes in `Content.Shared`, `Content.Server`, or `Content.Client`.

## Workflow

1. Open `references/component-checklist.md`.
2. Open `references/datafield-and-protoid-style.md` when serialized fields or prototype IDs change, especially to separate serialized config from runtime-only fields and to choose between `ProtoId<T>` and `EntProtoId`.
3. Open `references/component-networking.md` for replicated state.
4. Open `references/component-example-inner-body-anomaly.md` for a richer shared component example.
5. Keep the component data-only and move gameplay behavior to a system.
6. Match the component to the correct assembly.
7. If the component is shared and replicated, add networking attributes only when the state truly needs to sync.
8. Check nearby prototypes and localization if new serialized fields affect content.

## Reference Map

- `references/component-checklist.md`
- `references/datafield-and-protoid-style.md`
- `references/component-networking.md`
- `references/component-example-inner-body-anomaly.md`
- `../ss14-ecs-basics/references/ecs-primer.md`
- `../ss14-common-api-patterns/references/entitysystem-functions.md`
