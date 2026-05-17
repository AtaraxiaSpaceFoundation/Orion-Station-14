<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-npc-ai
description: Work with SS14 NPC systems, HTN behavior, pathfinding, steering, AI debug overlays, mindless mobs, hostile or friendly mob behavior, and NPC-related prototypes.
---

# SS14 NPC And AI

Use this skill when a change affects non-player decision making, pathing, steering, or NPC debug tools.

NPC behavior is server-authoritative gameplay with client debug presentation. Keep AI decisions data-driven where possible and avoid per-tick scans that scale with every mob.

## Workflow

1. Locate the AI layer.
- Server systems own decisions, targeting, path requests, and authoritative movement intents.
- Shared code owns shared components, events, and debug/network contracts.
- Client code owns overlays, EUI/debug windows, and visualization.

2. Prefer data-driven behavior.
- Use existing HTN, prototype, component, and action patterns before adding special-case code.
- Keep tunable behavior in prototypes or component fields.
- Avoid hardcoded entity IDs, faction names, or role assumptions in generic AI systems.

3. Respect pathfinding and steering costs.
- Avoid broad entity queries, allocations, and expensive path recalculation in frequent updates.
- Reuse existing steering/pathfinding systems and throttling patterns.
- Load `ss14-standard-optimizations` for update-heavy AI work.

4. Keep prediction expectations realistic.
- NPC decisions are server-authoritative.
- Client visuals/debug overlays should consume replicated or explicit debug state, not run their own gameplay AI.

5. Validate behavior in context.
- Build affected projects.
- Prefer an in-game test with multiple NPCs when behavior or performance changes.
- For debug overlays or EUI, test cleanup when toggled off.

## Reference Map

- `../ss14-standard-optimizations/SKILL.md`
- `../ss14-transform-physics/SKILL.md`
- `../ss14-ui-eui/SKILL.md`
- `../ss14-sprite-overlays-shaders/SKILL.md`
- `../ss14-netcode/SKILL.md`

## Good File Anchors

- `Content.Shared/NPC/**`
- `Content.Server/NPC/**`
- `Content.Client/NPC/**`
- `Resources/Prototypes/**/NPC/**`

## Common Pitfalls

- Running expensive target/path logic every tick for every NPC.
- Putting server AI decisions in client debug systems.
- Hardcoding one mob or faction into a generic AI path.
- Forgetting to remove debug overlays or network subscriptions when toggles close.
