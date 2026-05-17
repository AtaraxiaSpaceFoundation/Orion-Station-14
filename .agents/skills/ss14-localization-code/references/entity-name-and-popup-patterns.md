<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Entity Name And Popup Patterns

## Use Entity Arguments When Possible

- Pass entities into `Loc.GetString(...)` when the FTL entry uses helpers like `THE(...)` or capitalization helpers.
- Prefer entity-aware popup strings over manually formatted names.

## Example Anchors

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `Resources/Locale/en-US/wieldable/wieldable-component.ftl`

## Practical Rules

- Popup about an item: use `("item", uid)` instead of `("item", Name(uid))`.
- Popup about the acting player: use `("user", user)` when the FTL entry is written for an entity argument.
- For prototype-backed names or descriptions, prefer the existing localized access path instead of manually looking up a key.
