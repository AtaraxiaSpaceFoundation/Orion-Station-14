<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Orion Fork Gameplay Map

## Purpose

Use this file to route an Orion Station 14 gameplay task to the correct code assembly, fork namespace, resource folder, localization file, and validation step before editing.

This map is for the current Orion repository shape. Orion is not a plain upstream SS14 tree: it contains upstream-style `Content.*` assemblies, Goob-specific assemblies, fork-specific `_Orion` overlays, and several inherited prefixed content areas.

## Fast Triage

1. Name the player-facing mechanic, admin behavior, UI, entity family, or resource family.
2. Search for the mechanic in this order:
   - Orion-local code and data: `Content.Shared/_Orion/`, `Content.Server/_Orion/`, `Content.Client/_Orion/`, `Resources/Prototypes/_Orion/`, `Resources/Locale/*/_Orion/`, `Resources/Maps/_Orion/`, `Resources/Textures/_Orion/`, `Resources/Audio/_Orion/`.
   - Goob inherited code and data: `Content.Goobstation.*` assemblies, the existing `Content.Server/_Goobstation/` subtree, `Resources/Prototypes/_Goobstation/`, `Resources/ServerInfo/_Goobstation/`, and matching locale/resource folders.
   - Other inherited fork overlays when the feature clearly belongs there: `_DV`, `_EinsteinEngines`, `_Impstation`, `_Shitcode`, or similar prefixed folders.
   - Unprefixed upstream-style domains under `Content.Shared/`, `Content.Server/`, `Content.Client/`, and `Resources/`.
3. Start from `Content.Shared/<Domain>/` or `Content.Shared/_Orion/<Domain>/` when the feature crosses networking, prediction, BUI state/messages, actions, appearance, or client/server event contracts.
4. Start from `Content.Server/<Domain>/` or `Content.Server/_Orion/<Domain>/` when the feature is authority-only: spawning, objectives, round rules, admin commands, economy transactions, persistence, or server-side damage/effects.
5. Start from `Content.Client/<Domain>/` or `Content.Client/_Orion/<Domain>/` when the task is visual-only: XAML, BUI windows, overlays, sprites, local animation, input presentation, or UI polish.
6. Immediately pair code with data: check `Resources/Prototypes/`, `Resources/Locale/en-US/`, `Resources/Locale/ru-RU/`, `Resources/Textures/`, `Resources/Audio/`, `Resources/Maps/`, and `Resources/ServerInfo/` for the same domain or prefix.
7. If prototypes, maps, or serialized component fields change, search for every prototype/component usage before renaming fields or IDs.
8. Check `Content.Tests/`, `Content.IntegrationTests/`, and any domain-specific test helpers when behavior changes are risky.

## Current Repository Shape

Core gameplay assemblies:

- `Content.Shared/`: upstream-style shared components, events, BUI contracts, predicted logic, shared helper types, and shared gameplay primitives.
- `Content.Server/`: authoritative simulation, spawning, round rules, admin commands, persistence hooks, server-only effects, and game-state mutation.
- `Content.Client/`: visuals, XAML, BUIs, overlays, local presentation, input affordances, and client-only effects.
- `Content.Tests/` and `Content.IntegrationTests/`: content and integration validation.

Goob-specific assemblies:

- `Content.Goobstation.Shared/`: shared contracts and components inherited from Goob-specific systems.
- `Content.Goobstation.Server/`: server-authoritative Goob systems and feature logic.
- `Content.Goobstation.Client/`: Goob-side UI, visuals, overlays, and presentation logic.
- `Content.Goobstation.Common/`, `Content.Goobstation.Maths/`, `Content.Goobstation.UIKit/`: Goob support libraries and shared utility/UI infrastructure.

Database and tooling assemblies:

- `Content.Server.Database/` and `Content.Shared.Database/`: database models, persistence-facing types, and shared DB contracts. Do not hide normal gameplay logic here unless it really belongs to persistence.
- `Content.Tools/`, `Content.YAMLLinter/`, `Content.MapRenderer/`, `Content.ModuleManager/`, `Content.Packaging/`, `Content.Replay/`, and similar top-level projects are tooling/runtime support, not normal gameplay feature homes.

Resource roots:

- `Resources/Prototypes/`: entities, components, actions, roles, rules, datasets, sound collections, guidebook prototypes, and other YAML content.
- `Resources/Locale/en-US/` and `Resources/Locale/ru-RU/`: player-facing and admin-facing FTL text. Orion often needs both English and Russian strings.
- `Resources/Locale/en-US/ss14-ru/`: inherited/compatibility localization area; inspect it before creating duplicate strings for older translated prototype paths.
- `Resources/Textures/`: RSI sprites and texture assets.
- `Resources/Audio/`: sound assets and audio content.
- `Resources/Maps/`: station maps, shuttles, generated maps, and Orion-specific domain maps.
- `Resources/ServerInfo/`: guidebook/server-info XML and content documentation.

## Assembly Ownership Rules

Use shared when the client and server both need the type or event:

- Components serialized in prototypes and read on both sides.
- BUI keys, BUI state, BUI messages, action events, network events, appearance data, predicted events, and shared enum/state definitions.
- Cross-assembly helpers that must stay deterministic or be used by both client and server.

Use server when the server owns the truth:

- Spawning, deletion, entity transformation, objective completion, round-end checks, admin commands, economy transfers, persistence, antagonists, station events, damage application, and access/security mutation.
- Server-only popups/audio triggers may still need shared events or shared component data when client presentation depends on them.

Use client when it only changes presentation:

- XAML, BUI windows, overlays, sprite visualizers, local animation, tooltips, local sounds, local input affordances, UI formatting, and purely visual effects.

Use Goob assemblies when the existing implementation lives there:

- If the feature already has `Content.Goobstation.*` code, extend it there unless the change is Orion-specific.
- If the change is Orion-only behavior layered over a Goob system, prefer `_Orion` code that composes with the Goob system rather than editing Goob code unnecessarily.

## Namespace and Prefix Priority

Prefer the smallest correct layer:

1. `_Orion` for Orion-only gameplay, local balance, custom UI, local admin commands, local research/bitrunning/economy additions, and Russian fork behavior.
2. `Content.Goobstation.*` or `_Goobstation` for inherited Goob features that Orion is carrying forward.
3. Other inherited prefixed areas such as `_DV`, `_EinsteinEngines`, `_Impstation`, or `_Shitcode` when the existing feature is already there.
4. Unprefixed `Content.*` and `Resources/*` for upstream-style systems that are not fork-local.

Do not move an existing feature into `_Orion` just because a task is requested for Orion. Follow the existing file family unless the task is explicitly a fork-local extension.

## Common Cross-Assembly Shapes

- Shared component plus server system plus client visualizer.
- Shared action event plus server validation plus client popup/audio/visual feedback.
- Shared BUI key/state/messages plus server BUI handler plus client BUI/window XAML.
- Server-only rule/objective system backed by prototypes and localized text.
- Shared serialized component plus prototype data plus client sprite/appearance handling.
- Server economy/persistence logic plus shared UI state plus client PDA/console UI.

## Common Domain Clusters

### Character, body, damage, and status

Use these for body parts, species, mobs, health, equipment, restraints, movement modifiers, status effects, metabolism, surgery-like interactions, and medical state:

- `Body`, `Mobs`, `Humanoid`, `Hands`, `Inventory`, `Clothing`, `Damage`, `Medical`, `Species`, `Metabolism`, `Cuffs`, `Stunnable`, `Movement`, `StatusEffect`, `Drowsiness`, `Drunk`, `Drugs`, `Crawling`, `Standing`, `Surgery`, `Augments`.

### Item and interaction flow

Use these for verbs, held items, interaction gating, tool use, throwing, storage, equipment actions, construction interactions, and do-afters:

- `Actions`, `Interaction`, `Item`, `Storage`, `Tools`, `Throwing`, `Prying`, `Resist`, `Wieldable`, `Placeable`, `DoAfter`, `DragDrop`, `Construction`, `Hands`, `Inventory`, `Charges`, `Containers`.

### Station infrastructure

Use these for machines, wiring, atmospherics, power distribution, construction graphs, telecom/device links, shuttles, and station-level services:

- `Atmos`, `Power`, `SMES`, `APC`, `Machines`, `DeviceNetwork`, `DeviceLinking`, `NodeContainer`, `Construction`, `Wires`, `Shuttles`, `Station`, `Gravity`, `Holopad`, `Communications`, `Disposal`, `Doors`, `Cargo`.

### Roundflow, roles, objectives, and administration

Use these for role assignment, antagonist logic, objectives, round lifecycle, player sessions, ghost roles, admin-facing behavior, and station events:

- `Objectives`, `Roles`, `Antag`, `GameTicking`, `NukeOps`, `Revolutionary`, `Thief`, `Traitor`, `Administration`, `Preferences`, `Players`, `Respawn`, `Ghost`, `StationEvents`, `Mind`, `Jobs`, `LateJoin`.

### Presentation and feedback

Use these for visible feedback, overlays, alert state, guidebook content, UI state, sprite appearance, audio, and local-only polish:

- `Audio`, `Effects`, `Popups`, `Sprite`, `Alert`, `Alerts`, `StatusEffect`, `Guidebook`, `Overlays`, `UserInterface`, `Appearance`, `Animations`, `ContextMenu`, `Cooldown`, `Fullscreen`, `Outline`, `Viewport`, `HealthAnalyzer`.

### Economy, market, banking, and finance

Orion has local economy work under `_Orion`. Check these before adding generic cargo or vending logic:

- `Content.Server/_Orion/Economy/`: station accounts, salary/payday rules, transfers, CRAB-17/protocol logic, and other authoritative finance systems.
- `Content.Shared/_Orion/Economy/` if present for shared contracts, BUI messages, actions, and UI state.
- `Content.Client/_Orion/Economy/` if present for console/PDA/market UI.
- `Resources/Prototypes/_Orion/` for economy-related machines, items, actions, rules, and vending data.
- `Resources/Locale/en-US/_Orion/` and `Resources/Locale/ru-RU/_Orion/` for UI/admin/player-facing strings.

### Bitrunning

Bitrunning is Orion-local and crosses all layers. For any bitrunning task, inspect the whole family, not just one system:

- `Content.Shared/_Orion/Bitrunning/`: shared components, BUI keys, state/messages, visuals, points/vendor data, disk/netpod/console contracts.
- `Content.Server/_Orion/Bitrunning/`: domain execution, objectives, avatar control, byteforge, quantum server, server-side validation, rewards, spawning, and disconnect logic.
- `Content.Client/_Orion/Bitrunning/`: quantum console UI, netpod UI, bitrunning windows, visual presentation.
- `Resources/Prototypes/_Orion/Bitrunning/`: domain definitions and domain data.
- `Resources/Prototypes/_Orion/Entities/Structures/Machines/bitrunning.yml`: bitrunning machines such as quantum console/server/netpod/byteforge.
- `Resources/Prototypes/_Orion/DeviceLinking/bitrunning_ports.yml`: device-linking ports for bitrunning machines.
- `Resources/Prototypes/_Orion/Entities/Objects/Specific/Cargo/bitrunning_disks.yml`: disks and cargo-side bitrunning objects.
- `Resources/Maps/_Orion/Bitrun/`: bitrunning domain maps.
- `Resources/Locale/en-US/_Orion/bitrunning/` and `Resources/Locale/ru-RU/_Orion/bitrunning/`: names, descriptions, UI messages, domain text, and player-facing feedback.

### Research and R&D

Orion has local research work under `_Orion`. Research tasks usually touch prototypes, client UI, and shared discovery/experiment state:

- `Content.Shared/_Orion/Research/`: shared research discovery/experiment contracts and data types.
- `Content.Client/_Orion/Research/`: destructive analyzer/research UI helpers and windows.
- `Content.Server/_Orion/Research/` if present: server-side experiment, research, or analysis authority.
- `Resources/Prototypes/_Orion/Research/`: nodes, technologies, experiments, prices, and unlock data.
- `Resources/Prototypes/_Orion/Entities/Objects/` and `Resources/Prototypes/_Orion/Entities/Structures/` for research disks, machines, and analyzer-related entities.
- Matching locale under `Resources/Locale/en-US/_Orion/` and `Resources/Locale/ru-RU/_Orion/`.

### Mood

Mood is Orion-local. Do not assume upstream mood behavior exists:

- `Content.Shared/_Orion/Mood/`: shared mood component, effect prototypes, shared state, and contracts.
- `Content.Server/_Orion/Mood/`: authoritative mood application, commands, and game-state effects.
- `Content.Client/_Orion/Mood/` if present: UI, alerts, and client presentation.
- `Resources/Prototypes/_Orion/` and locale folders for mood effects and strings.

### Morph, cortical borer, recruitment, posing, language, and other Orion-local systems

Inspect `_Orion` first for these systems:

- `Content.Shared/_Orion/Morph/`, `Content.Server/_Orion/Morph/`, `Content.Client/_Orion/Morph/` and `Resources/Prototypes/_Orion/Actions/morph.yml` for morph/antag action logic.
- `Content.Server/_Orion/CorticalBorer/`, `Resources/Prototypes/_Orion/Alerts/cortical_borer.yml`, and related entity/locale data for cortical borer work.
- `Content.Shared/_Orion/Recruitment/`, `Content.Server/_Orion/Recruitment/`, and `Content.Client/_Orion/Recruitment/` for recruitment/member-list UI and state.
- `Content.Shared/_Orion/Posing/` and `Content.Server/_Orion/Posing/` for posing mechanics.
- `Content.Shared/_Orion/Language/` for language learning/use events and contracts.
- `Content.Shared/_Orion/PowerCell/`, `Content.Shared/_Orion/Lighting/`, `Content.Shared/_Orion/ChameleonStamp/`, and neighboring `_Orion` folders for small local feature families.

## Server-Heavy Hotspots

These areas are usually server-authoritative. Verify peers before assuming they are server-only:

- `Acz`, `Afk`, `Announcements`, `Chunking`, `Codewords`, `Connection`, `CPUJob`, `Database`, `Discord`, `ExCable`.
- `GameTicking`, `StationEvents`, `Objectives`, `Roles`, `Antag`, `Jobs`, `Ghost`, `Respawn`, `Station`, `Shuttles`.
- `PowerSink`, `RandomAppearance`, `RandomMetadata`, `RequiresGrid`, `Screens`, `ServerInfo`, `ServerUpdates`.
- `Spawners`, `Tesla`, `VentHorde`, `Vocalization`, `VoiceTrigger`, `KillTracking`, `GuideGenerator`.
- Orion-local server systems such as `_Orion/Bitrunning`, `_Orion/Economy`, `_Orion/Mood`, `_Orion/StationGoal`, `_Orion/DocumentPrinter`, `_Orion/Recruitment`, `_Orion/CorticalBorer`, `_Orion/Morph`, `_Orion/Objectives`.

If a task lands here, expect server validation and state authority. Add shared/client code only for contracts or presentation.

## Client-Heavy Hotspots

These areas are usually client presentation. Verify peers before assuming they are client-only:

- `Alerts`, `Animations`, `Changelog`, `Clickable`, `CloningConsole`, `ContextMenu`, `Cooldown`, `Credits`.
- `DamageState`, `DebugMon`, `FeedbackPopup`, `FlavorText`, `Fullscreen`, `Gameplay`, `Graphics`, `HealthAnalyzer`.
- `Interactable`, `Items`, `Kudzu`, `LateJoin`, `Launcher`, `Lobby`, `MainMenu`, `Markers`, `Message`.
- `NetworkConfigurator`, `Options`, `Orbit`, `Outline`, `Playtime`, `Replay`, `Resources`, `RichText`, `Screenshot`, `Stylesheets`, `Viewport`.
- Orion-local UI folders such as `_Orion/Bitrunning/UI`, `_Orion/Research/UI`, `_Orion/Recruitment`, and any XAML-backed console/PDA UI.

If a task only touches these areas, avoid introducing new server/shared dependencies unless a real state contract is missing.

## Shared Utility Buckets

Some buckets are shared-first even when they do not map cleanly to a server/client folder pair:

- `ActionBlocker`, `APC`, `Blocking`, `Climbing`, `ComponentTable`, `DetailExaminable`, `Execution`, `Friction`.
- `Glue`, `HealthExaminable`, `Internals`, `Metabolism`, `Prototypes`, `Repairable`, `Rotatable`, `Spawning`.
- `StatusEffect`, `Timing`, `Warps`, `Whistle`, `Appearance`, `DoAfter`, `EntityTable`, `EntityList`, `BUI` state/message definitions.

Treat these as shared primitives that other domains compose. Keep them generic and avoid hiding feature-specific logic inside them.

## Code-To-Data Pairing Rules

- If you add or rename a serialized component field, search all YAML prototypes and maps using that component.
- If you add a new popup, action, UI label, admin command string, examine text, objective text, or machine message, add FTL for both `en-US` and `ru-RU` when the feature is Orion-facing.
- If you touch inherited localization under `Resources/Locale/en-US/ss14-ru/`, verify whether the matching `_Orion` or upstream locale already exists before duplicating keys.
- If you add reusable audio, prefer a sound collection prototype or existing resource pattern instead of hardcoding file paths.
- If you add a reusable visual state, decide whether it belongs in a shared appearance component, a client visualizer, a sprite RSI, or all three.
- If you add a new prototype family, keep parents/base prototypes in `base.yml` when that pattern already exists and put variants in neighboring files.
- If the mechanic has maps, check `Resources/Maps/` as well as entity prototypes. Bitrunning domains, shuttles, station events, and ruins often depend on map files.
- If the mechanic has guidebook/player documentation, check `Resources/ServerInfo/` and guidebook prototypes.

## Validation Checklist

Before handing off a patch, choose the relevant subset:

- Build: `dotnet build --configuration DebugOpt --no-restore /m` after `dotnet restore`, or a narrower project build.
- Code tests: `dotnet test --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0` and/or `dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed` when behavior changed.
- Prototype/YAML changes: run the repo's YAML/content validation path if available, and at minimum check IDs, parents, components, enum values, resource paths, and localization keys.
- UI changes: open the BUI/window in-game or through the nearest existing debug/admin path; check scaling, missing loc strings, disabled buttons, and stale state updates.
- Resource changes: verify RSI state names, sprite paths, audio paths, licenses/meta files, and casing.
- Networking/prediction changes: verify shared events/components are in shared code, server authority remains server-side, and client code does not mutate authoritative state.

## Useful Next References

- `../feature-checklist.md`
- `../../ss14-client-server-shared/references/client-server-primer.md`
- `../../ss14-client-server-shared/references/shared-and-prediction.md`
- `../../ss14-prototype-basics/references/first-prototype-workflow.md`
