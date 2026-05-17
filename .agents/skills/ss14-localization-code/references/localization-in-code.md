<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Localization In Code

## Rules

- Use `Loc.GetString(...)` for player-facing text emitted from systems, UI, verbs, or events.
- Keep arguments explicit, for example `("item", uid)` or `("user", user)`.
- Let FTL handle grammar, selectors, and entity formatting instead of concatenating strings in C#.

## Example Anchors

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `Resources/Locale/en-US/wieldable/wieldable-component.ftl`

## Common Mistakes

- Building messages with `+` or string interpolation when the final result should be localizable.
- Hardcoding temporary English and forgetting to come back later.
- Passing a plain name string when the FTL entry expects an entity for grammar helpers.
