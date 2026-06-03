<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-localization-strings
description: Add or review SS14 localization strings and localized C# usage. Use when changing player-facing text, FTL keys, Loc.GetString calls, localized prototype names or descriptions, or ensuring gameplay and UI text is properly localized.
---

# SS14 Localization Strings

Use this skill for FTL changes and localized string usage from C# or prototypes.

## Workflow

1. Open `references/localization-policy.md`.
2. Open `references/localization-examples.md` when you need a concrete pattern.
3. Open `references/ftl-naming-and-layout.md` for file and key layout.
4. Open `references/prototype-and-marking-examples.md` for localized content data.
5. Open `references/selectors-and-entity-args.md` for entity-aware FTL patterns.
3. Add FTL whenever a player-facing string changes.
4. Keep localization IDs specific and feature-scoped.

## FTL Formatting Notes

- Keep Fluent selectors, variables, functions, and SS14 grammar helpers such as `THE(...)` as syntax/helpers; do not translate or copy them as visible text.
- When a feature already maintains `ru-RU` locale, update matching `ru-RU` entries in the same change instead of leaving raw English player-facing text nearby.
- Preserve FTL structure while translating text: keep key names, variable names, select cases, and helper calls stable unless the code/prototype contract also changes.

## Reference Map

- `references/localization-policy.md`
- `references/localization-examples.md`
- `references/ftl-naming-and-layout.md`
- `references/prototype-and-marking-examples.md`
- `references/selectors-and-entity-args.md`
- `../ss14-localization-code/references/entity-name-and-popup-patterns.md`
