<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Fork Only Content

## Use `_Orion` When

- The behavior is genuinely Orion-specific.
- You are adding new Orion prototypes, locale, assets, or sidecar systems that should stay clearly separated from inherited content.
- A matching `_Orion` feature folder already exists.

## Respect Existing Non-Orion Fork Trees

This repository also contains inherited/vendor trees such as `_Goobstation`, `_EinsteinEngines`, `_Shitmed`, `_DV`, `_NF`, `_Mono`, `_RMC14`, `_White`, `_Lavaland`, `_Corvax*`, and others in code and resources. If a feature already lives in one of those trees, extend that owner instead of moving it to `_Orion` without a task-specific reason.

## Current Orion Anchors

- `Content.Shared/_Orion/`: shared Orion contracts, components, events, prototypes, and predicted systems such as `Bitrunning`, `Economy`, `Language`, `Mood`, `Morph`, `Recruitment`, `Research`, and `Medical`.
- `Content.Server/_Orion/`: server-authoritative Orion systems such as `Administration`, `Bitrunning`, `CorticalBorer`, `Economy`, `Mood`, `Morph`, `Objectives`, `Recruitment`, `Research`, `ServerProtection`, and `StationGoal`.
- `Content.Client/_Orion/`: Orion presentation/UI such as `Administration`, `Bitrunning/UI`, `Economy/UI`, `Lobby/UI`, `Morph/UI`, `Recruitment/UI`, `Research/UI`, overlays, stylesheets, and shared controls.
- `Resources/Prototypes/_Orion/`: Orion content data across many families, including actions, alerts, bitrunning, catalog, chemistry, economy, entities, guidebook, language, mood, reagents, recipes, research, roles, and traits.
- `Resources/Locale/en-US/_Orion/` and `Resources/Locale/ru-RU/_Orion/`: Orion localization.
- `Resources/Textures/_Orion/`, `Resources/Audio/_Orion/`, `Resources/Maps/_Orion/`, and `Resources/ServerInfo/_Orion/`: Orion assets, maps, and server/guidebook info.
