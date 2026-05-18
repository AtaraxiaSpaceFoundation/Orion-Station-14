<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-atmos
description: Work with SS14 atmospherics, gases, gas mixtures, fire, pressure, pipes, vents, pumps, scrubbers, air alarms, atmos overlays, and atmos-related prototypes or UI.
---

# SS14 Atmos

Use this skill when a change touches gas simulation, atmos devices, fire, pressure, or atmos UI.

Atmos is simulation-heavy and easy to desync or slow down. Keep server authority clear, reuse existing atmos device patterns, and avoid broad per-tile work unless the surrounding system already does it that way.

## Workflow

1. Locate the simulation owner.
- Server systems own gas, fire, pressure, pipe networks, and authoritative device behavior.
- Client systems should render overlays, pipe visuals, and UI from replicated or explicitly sent state.
- Shared code should stay limited to shared components, enums, UI messages, and data contracts.

2. Follow existing atmos units and data shapes.
- Treat temperature, pressure, moles, and gas ratios consistently with nearby code.
- Use existing `Gas`, `GasMixture`, pipe, device, and alarm patterns instead of inventing parallel data containers.
- Keep device settings prototype-driven or component-driven when content needs to tune them.

3. Be careful with hot paths.
- Atmos updates can touch many tiles or devices; avoid allocations, LINQ, broad entity queries, and repeated prototype lookups in update loops.
- Load `ss14-standard-optimizations` for per-tile, per-grid, or frequent-event changes.

4. Keep visuals and UI separate from simulation.
- Use appearance, overlays, BUI state, and localized UI text for presentation.
- Do not make UI actions mutate client state directly; route through existing BUI/server authority paths.

5. Validate the resource layer.
- Update prototypes, FTL, and UI labels together.
- Run YAML validation for prototype changes and build affected C# projects for code changes.

## Reference Map

- `../ss14-standard-optimizations/SKILL.md`
- `../ss14-ui-bui/SKILL.md`
- `../ss14-ui-xaml/SKILL.md`
- `../ss14-sprite-overlays-shaders/SKILL.md`
- `../ss14-prediction/SKILL.md`

## Good File Anchors

- `Content.Shared/Atmos/**`
- `Content.Server/Atmos/**`
- `Content.Client/Atmos/**`
- `Resources/Prototypes/**/Atmos/**`
- `Resources/Locale/**/atmos*`

## Common Pitfalls

- Moving server-authoritative simulation into client visuals or UI handlers.
- Adding expensive work inside atmos update loops.
- Forgetting localization for atmos UI, alarms, or device feedback.
- Changing pipe/device behavior without checking matching shared messages and client UI.
