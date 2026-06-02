<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# UI Flow Map

## Purpose

Use this file to trace an SS14 UI feature from shared state to server handling, client rendering, localization, and validation before editing.

## Standard UI Chain

Most SS14 UI work touches some combination of:

1. Shared component, shared event, or shared BUI state in `Content.Shared/`
2. Server-side system or bound UI handling messages in `Content.Server/`
3. Client controller, bound UI, or window class in `Content.Client/`
4. Paired `.xaml` and `.xaml.cs` files for the actual layout
5. FTL entries under `Resources/Locale/en-US/`

Start from the feature domain, then walk outward through this chain instead of editing the window in isolation.

## Client-Only UI Hotspots

Several top-level `Content.Client/` folders are presentation-only and usually do not need new shared types unless the data model changes:

- `ContextMenu`, `Cooldown`, `Fullscreen`, `Gameplay`, `Lobby`, `MainMenu`, `Options`, `Orbit`
- `Outline`, `Replay`, `RichText`, `Screenshot`, `Stylesheets`, `Viewport`
- `Alerts`, `DamageState`, `FeedbackPopup`, `HealthAnalyzer`, `Message`, `Markers`

If your change is purely visual, stay in the client layer and avoid inventing new networking.

## Feature-Local UI

A lot of UI lives inside the same domain folder as the gameplay system:

- `Cargo`, `PDA`, `Medical`, `Research`, `Lathe`, `Storage`, `Power`, `Shuttles`, `Objectives`, `NukeOps`
- Expect windows and controllers under a nearby `UI/` folder, with shared state or messages under the same feature in `Content.Shared/`

When searching, check both the domain folder and the generic `UserInterface` helpers.

## Predicted UI Rules

- Prefer reading already-networked component state when possible.
- Use `SendPredictedMessage` for predicted interactions.
- Update predicted client UI from shared state and `AfterAutoHandleStateEvent` where that pattern already exists.
- Do not put server-only effects directly in predicted shared code paths.

## Orion Caveat

The fork has broad `_Orion` client UI areas such as administration, bitrunning, economy, lobby, morph, recruitment, research, custom controls, and windows. If a new Orion-specific UI is required, prefer either:

- extending the existing domain folder when the feature belongs to a normal SS14 system, or
- adding a clearly named `_Orion` domain only when the behavior is genuinely fork-specific

## Documentation Caveat

This repo no longer relies on a separate local docs site for UI reference coverage.

- Trust nearby in-repo UI implementations first.
- Use the local skill references in this skill for the stable guidance.
- If a pattern conflicts with an old tutorial you remember from elsewhere, prefer the live repo implementation.

## Validation

- Build the affected projects.
- Run targeted tests if they exist.
- Perform an in-game pass for layout, state sync, button behavior, and prediction if possible.
- Say explicitly when in-game verification was not possible.

## Common Mistakes

- Editing only the `.xaml` while forgetting the shared or server-side state shape.
- Duplicating networked data in extra BUI state without a reason.
- Hardcoding UI strings instead of adding FTL.
- Adding new global stylesheet rules when a nearby local pattern already exists.
