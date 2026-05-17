<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Skill Preflight And Refresh

Read this rule before starting work and after any context compaction or cleanup.

## Always Read

- Read every file in `.agents/rules/`.
- Read every skill in `.agents/skills/` that matches the task.
- Re-read the relevant rules and skills after auto-compaction, manual summarization, or a long pause.

## Always-On Skills

These skills should be loaded for almost every gameplay task in this repo:

- `ss14-naming-conventions`
- `ss14-ecs-prototypes`
- `ss14-upstream-maintenance`

## If Writing C#

Load these skills:

- `ss14-ecs-components`
- `ss14-ecs-entities`
- `ss14-ecs-prototypes`
- `ss14-ecs-systems`
- `ss14-events`
- `ss14-prediction`

## Conditional Skills

- Large or multi-file C# feature: `ss14-documentation-writing`
- Hot path, `Update()`, or high-frequency event logic: `ss14-standard-optimizations`
- Player-facing text or FTL: `ss14-localization-strings`
- Player-facing text in C# or localized component fields: `ss14-localization-code`
- Network events, `NetEntity`, replicated messages, or shared/server/client network flow: `ss14-netcode`
- `Appearance`, `GenericVisualizer`, or sprite-layer state work: `ss14-graphics-generic-visualizer-appearance`
- Sprites, RSI metadata, overlays, shaders, or custom visual effects: `ss14-sprite-overlays-shaders`
- Writing or selecting tests: `ss14-tests-authoring`
- Architecture explanation, onboarding, or first-feature guidance: `ss14-prototype-basics`, `ss14-ecs-basics`, `ss14-client-server-shared`
- Debugging, VV, logs, breakpoints, or runtime investigation: `ss14-debugging-workflow`, `ss14-common-api-patterns`
- Common gameplay helper APIs such as entity-system methods, spawning, audio, popups, or random: `ss14-common-api-patterns`
- Audio routing, sound assets, sound collections, ambient/music, or predicted sound feedback: `ss14-audio`
- Atmospherics, gases, fire, pressure, pipes, atmos devices, or atmos UI: `ss14-atmos`
- Transforms, coordinates, grids, maps, anchoring, movement, collision, fixtures, or physics: `ss14-transform-physics`
- PVS, visibility, network interest, PVS filters, PVS overrides, or PVS-sensitive client code: `ss14-pvs`
- Porting, license checks, or attribution: `ss14-porting-and-licensing`
- AI workflow and prompt/verification guidance for this repo: `ss14-ai-workflow`
- XAML windows, controls, code-behind, or client UI layout: `ss14-ui-xaml`
- BUI flows, UI keys, BUI state/messages, or `BoundUserInterface` classes: `ss14-ui-bui`
- EUI flows, `BaseEui`, `EuiStateBase`, `EuiMessageBase`, or admin/debug UI sessions: `ss14-ui-eui`
- Database models, EF Core contexts, migrations, persistence services, or schema compatibility: `ss14-databases-migrations`
- NPCs, HTN, pathfinding, steering, mob AI, AI debug overlays, or NPC prototypes: `ss14-npc-ai`
- Broad gameplay/resource routing: `ss14-gameplay-feature`, `ss14-prototypes-locale`

## Working Notes

- If the task is long or research-heavy, keep a temporary scratch file for findings you cannot afford to lose during compaction.
- Delete the scratch file before finishing unless the task explicitly asked for a saved artifact.

## Rider MCP

- If Rider MCP exists, prefer it over shell equivalents for search, analysis, edits, and build verification.
- If Rider MCP is unavailable, continue with normal tools instead of blocking.
