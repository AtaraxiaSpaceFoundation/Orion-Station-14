<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-graphics-generic-visualizer-appearance
description: Work with SS14 visual state driven by `Appearance` and `GenericVisualizer`. Use when editing appearance enums, sprite-layer visibility rules, prototype-driven visual toggles, or shared/client visual state that maps gameplay state to RSIs.
---

# SS14 GenericVisualizer And Appearance

Use this skill when the task changes prototype-driven visuals without building a custom client visualizer from scratch.

## Workflow

1. Open `references/generic-visualizer-patterns.md`.
2. Open `references/appearance-data-flow.md` when the task crosses shared state and client visuals.
3. Open `references/visual-prototype-anchors.md` for concrete repo examples.
4. Keep state ownership clear: gameplay state in components, visual toggles in appearance/visualizer layers.

## Reference Map

- `references/generic-visualizer-patterns.md`
- `references/appearance-data-flow.md`
- `references/visual-prototype-anchors.md`
- `../ss14-prototypes-locale/references/prototype-locale-checklist.md`
