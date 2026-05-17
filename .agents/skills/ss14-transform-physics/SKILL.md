<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-transform-physics
description: Work with SS14 transforms, coordinates, grids, maps, anchoring, containers, movement, collision, fixtures, physics bodies, thrown entities, or spatial queries.
---

# SS14 Transform And Physics

Use this skill when a change moves entities, changes collision, depends on map/grid coordinates, or edits physics behavior.

Spatial code is fragile because transform, map grids, containers, and physics all interact. Prefer the established entity systems over direct component mutation.

## Workflow

1. Choose the right coordinate space.
- Use entity coordinates for relative positions and map coordinates for world/map-space decisions.
- Convert deliberately at system boundaries; do not compare coordinates from different maps or grids without conversion.
- Handle nullspace, deleted entities, and entities leaving PVS or containers.

2. Use systems for spatial mutations.
- Prefer `SharedTransformSystem`, `SharedPhysicsSystem`, container systems, and movement systems over direct field writes.
- Anchoring, parenting, and grid changes should go through the existing APIs so broadphase, fixtures, and dirty state stay coherent.
- Do not make shared predicted movement depend on server-only systems.

3. Keep collision data data-driven.
- Prefer prototype fixture and body settings when content should tune collision.
- If code changes fixture masks, layers, density, or collision state, check the nearby component and prototype patterns.

4. Respect server authority and prediction.
- Server systems own authoritative physics outcomes.
- Shared predicted code may provide local responsiveness, but must converge with server state.
- Avoid random impulses or non-deterministic client-only motion in predicted paths.

5. Validate with focused coverage.
- Build affected shared/server/client projects.
- For movement or collision bugs, prefer an in-game pass or a targeted integration test when practical.

## Reference Map

- `../ss14-prediction/SKILL.md`
- `../ss14-netcode/SKILL.md`
- `../ss14-standard-optimizations/SKILL.md`
- `../ss14-ecs-systems/SKILL.md`

## Good File Anchors

- `Content.Shared/**/Movement/**`
- `Content.Shared/**/Physics/**`
- `Content.Server/**/Movement/**`
- `Content.Server/**/Physics/**`
- `Resources/Prototypes/**`

## Common Pitfalls

- Mutating transform or physics component fields directly.
- Mixing map and entity coordinates without an explicit conversion.
- Forgetting containers, anchoring, or grid changes when moving entities.
- Adding client-only movement that cannot reconcile with server state.
