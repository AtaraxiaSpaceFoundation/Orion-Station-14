<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Resources Map

## Purpose

Use this file to place prototype, localization, texture, audio, and map edits in the correct `Resources/` subtree for the `open-space` fork.

## Main Resource Roots

- `Resources/Prototypes/`: data definitions for entities, reagents, recipes, objectives, roles, maps, loadouts, sound collections, and other game content.
- `Resources/Locale/<culture>/`: Fluent localization files, usually starting with `en-US`.
- `Resources/Textures/`: sprites, RSIs, decals, and other visual assets.
- `Resources/Audio/`: sound files plus attribution or organization by feature area.
- `Resources/Maps/`: map YAML and related data.

## Prototype Folder Families

The repo already uses a broad set of top-level prototype families. Common ones include:

- `Entities/`: most entity prototype trees, including items, mobs, structures, customization, and machine content.
- `Reagents/`, `Recipes/`, `Nutrition/`: chemistry and crafting data.
- `Objectives/`, `Roles/`, `GameRules/`, `Loadouts/`, `Traits/`: roundflow, jobs, objectives, and character setup.
- `Catalog/`, `Datasets/`, `EntityLists/`: reusable data pools and lookup content.
- `Polymorphs/`, `NPCs/`, `Procedural/`, `Maps/`: runtime content generation and special behaviors.
- `SoundCollections/`, `Shaders/`, `StatusIcon/`: reusable presentation data.
- `_Orion/`: fork-specific prototypes.
- `_Corvax/`: additional fork/vendor subtree already present in this repo.

Use the most specific existing subtree instead of inventing a new top-level folder.

## Locale Folder Reality

Locale folder names do not always mirror prototype folder names one-to-one.

- Prototype folders often use PascalCase or plural forms such as `AlertLevels`, `SoundCollections`, `Entities`.
- Locale folders usually use lowercase or kebab-case such as `alert-levels`, `popup`, `station-events`, `item-recall`, `vending-machines`.
- Some domains are concept-based instead of folder-name-based, for example prototype data in `Entities/` may localize under `items`, `medical`, `markings`, or another nearby feature directory.

Do not assume the FTL path from the prototype path. Search nearby locale folders and follow the local convention.

## Fork-Specific Open-Space Content

Current `_Orion` resource content already includes:

- `Resources/Prototypes/_Orion/Entities/Mobs/Customization/Markings/`: fork-specific marking prototypes.
- `Resources/Locale/en-US/_Orion/commands/`: custom admin or server command strings.
- `Resources/Locale/en-US/_Orion/markings/`: localized names for fork-specific markings.

When adding more fork-specific content, prefer extending the `_Orion` subtree for both prototypes and locale so the fork delta stays discoverable.

## Prototype Placement Heuristics

- Spawnable or entity-backed content usually belongs somewhere under `Entities/`.
- Reusable sound bundles belong under `SoundCollections/`.
- Character selection or role data often belongs under `Loadouts/`, `Roles/`, or `Traits/`.
- Chemistry data usually spans `Reagents/`, `Recipes/`, and locale under `chemistry` or a nearby gameplay domain.
- Mapping entities may still live under `Entities/`, while the actual station or salvage map assets live under `Maps/`.

## Code-To-Resource Pairing

- C# component or system changes often require prototype changes in a same-named or nearby domain folder.
- Player-facing strings in code, prototypes, and UI require FTL under `Resources/Locale/en-US/`.
- Sprite or appearance work usually needs both prototype references and assets under `Resources/Textures/`.
- Audio changes should prefer a sound collection prototype when multiple entities or systems reuse the same sound set.

## Validation Hooks

- Prototype or locale edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- RSI edits: `py -3 Schemas/validate_rsis.py Resources`
- Map edits: rely on schema validation in CI and keep map-only changes isolated when practical
