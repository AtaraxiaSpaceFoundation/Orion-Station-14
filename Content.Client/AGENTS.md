<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Content.Client Agent Notes

Read [../AGENTS.md](../AGENTS.md) first.

For `Content.Client` work always load:

- `ss14-naming-conventions`
- `ss14-upstream-maintenance`
- `ss14-prediction`

Also load:

- `ss14-ui-xaml` for XAML windows, controls, code-behind, or client UI layout
- `ss14-ui-bui` for bound UI classes, BUI state/messages, or predicted BUI flows
- `ss14-ui-eui` for EUI admin/debug sessions
- `ss14-localization-strings` and `ss14-localization-code` for UI or popup text
- `ss14-netcode` when client work depends on replicated messages or `NetEntity`
- `ss14-graphics-generic-visualizer-appearance` for appearance-driven visual work
- `ss14-sprite-overlays-shaders` for overlays, shaders, RSI, or visual-layer work
- `ss14-audio` for client audio, ambient sounds, lobby music, or UI sounds
- `ss14-atmos` for atmos overlays, pipe visuals, or atmos UI
- `ss14-pvs` for visibility, PVS scale, or code that handles entities leaving PVS
- `ss14-transform-physics` for client movement, transform, or physics presentation

Prefer XAML-first UI, client-side presentation, and reading already-networked state before duplicating UI state.
