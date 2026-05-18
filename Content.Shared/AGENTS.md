<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Content.Shared Agent Notes

Read [../AGENTS.md](../AGENTS.md) first.

For `Content.Shared` work always load:

- `ss14-naming-conventions`
- `ss14-ecs-prototypes`
- `ss14-upstream-maintenance`
- `ss14-ecs-components`
- `ss14-ecs-entities`
- `ss14-ecs-systems`
- `ss14-events`
- `ss14-prediction`
- `ss14-netcode`

Also load:

- `ss14-localization-code` when shared code emits player text or stores `LocId`
- `ss14-graphics-generic-visualizer-appearance` when shared gameplay state drives `Appearance` or `GenericVisualizer`
- `ss14-audio` when shared components or events carry sound specifiers or predicted audio intent
- `ss14-atmos` when shared components, UI messages, or enums belong to atmos features
- `ss14-transform-physics` for shared coordinates, movement, collision, anchoring, or physics contracts
- `ss14-pvs` for PVS-sensitive shared/network contracts
- `ss14-npc-ai` for shared NPC, HTN, steering, pathfinding, or debug contracts

Shared code owns replicated state, shared events, and prediction-aware logic. Do not add direct client-only or server-only dependencies here.
