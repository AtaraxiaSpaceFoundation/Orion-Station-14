<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Localization Required

Treat localization as mandatory work for gameplay, UI, admin-facing, and prototype-backed player text.

## Required

- Localize every player-facing string.
- Add or update FTL in `Resources/Locale/en-US/` first.
- Use specific feature-scoped `kebab-case` keys.
- Store reusable localization keys in data as `LocId` when the surrounding pattern already does that.

## Applies To

- Popups, chat text, examine text, UI labels, button text, tooltips, and alerts.
- Prototype names, descriptions, dataset entries, marking names, reagent names, and similar content data.
- Admin or debug text when it is exposed to players or operators in the game client.

## Do Not

- Hardcode user-facing English in C#.
- Compare localized strings in logic.
- Show raw enum `ToString()` or raw prototype IDs to players.

## Pairing Reminder

- If you add a popup or action result in code, update FTL in the same pass.
- If you add a player-visible prototype or marking, update its locale in the same pass.
