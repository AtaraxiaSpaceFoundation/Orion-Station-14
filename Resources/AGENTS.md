<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Resources Agent Notes

Read [../AGENTS.md](../AGENTS.md) first.

For `Resources` work always load:

- `ss14-naming-conventions`
- `ss14-ecs-prototypes`
- `ss14-upstream-maintenance`
- `ss14-prototypes-locale`
- `ss14-localization-strings`

Also load:

- `ss14-graphics-generic-visualizer-appearance` for `Appearance`, `GenericVisualizer`, RSI, or visual layer work
- `ss14-sprite-overlays-shaders` for RSI metadata, textures, sprite layers, overlays, or shader resources
- `ss14-audio` for `Resources/Audio`, sound collections, jukebox, lobby, or ambient sound resources
- `ss14-atmos` for gas, pipe, atmos machine, or fire-related prototypes
- `ss14-transform-physics` for fixture, body, collision, anchoring, or movement-related prototype data
- `ss14-npc-ai` for NPC, HTN, faction, steering, or mob AI prototypes

Use the narrowest existing subtree, keep parent prototypes in `base.yml` when you start a new parent tree, and validate YAML or RSI metadata after edits.
