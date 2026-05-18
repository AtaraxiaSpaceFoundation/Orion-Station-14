<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-sprite-overlays-shaders
description: Work with SS14 sprites, RSI metadata, sprite layers, overlays, custom visualizers, shaders, lighting-like client effects, and visual assets under `Resources/Textures` or client visual systems.
---

# SS14 Sprites, Overlays, And Shaders

Use this skill when a change affects visual assets or client-side rendering.

Prefer data-driven sprite and appearance patterns before writing custom rendering code. When custom overlays or shaders are necessary, keep them client-only, narrow, and cheap.

## Workflow

1. Choose the simplest visual path.
- Prototype sprite layers and `GenericVisualizer` are preferred for normal state-driven visuals.
- Custom client visualizers are for behavior that prototypes cannot express cleanly.
- Overlays and shaders are for screen/world rendering effects that are truly rendering concerns.

2. Keep visuals client-owned and state-driven.
- Gameplay state belongs in shared/server components.
- Client visuals should read appearance, networked state, or explicit visual events.
- Do not make rendering systems mutate authoritative gameplay state.

3. Preserve asset conventions.
- Keep RSI `meta.json` ordering and indentation consistent with repo rules.
- Put fork-only textures under `_Orion` when applicable.
- Check license and attribution for new or ported art.

4. Watch render hot paths.
- Avoid allocations, broad entity scans, and expensive per-frame lookups in overlays.
- Cache resources and query only the data needed for drawing.
- Load `ss14-standard-optimizations` for overlays, shaders, or frequently updated visual systems.

5. Validate visuals.
- Run RSI validation for texture metadata changes.
- Build client code for overlay/shader changes.
- Prefer an in-game or screenshot pass when changing visible rendering.

## Reference Map

- `../ss14-graphics-generic-visualizer-appearance/SKILL.md`
- `../ss14-standard-optimizations/SKILL.md`
- `../ss14-porting-and-licensing/SKILL.md`
- `../ss14-prototypes-locale/SKILL.md`

## Good File Anchors

- `Content.Client/**/*Overlay*.cs`
- `Content.Client/**/*Visualizer*.cs`
- `Content.Client/**/*Shader*.cs`
- `Resources/Textures/**`
- `Resources/Prototypes/**`

## Common Pitfalls

- Writing a custom overlay for a simple sprite-layer toggle.
- Putting visual-only code in shared or server assemblies.
- Adding RSI states without validating `meta.json`.
- Doing heavy entity queries or allocations every frame.
