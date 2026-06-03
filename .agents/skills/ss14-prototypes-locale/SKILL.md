<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-prototypes-locale
description: Edit SS14 content resources under `Resources/`. Use when touching prototypes, localization, YAML, FTL, sprite metadata, sound collections, maps, or when mapping gameplay and UI code to the correct prototype, locale, texture, audio, or validation files.
---

# SS14 Prototypes And Locale

Use this skill for data-first content changes under `Resources/`.

Keep prototype structure, localization, and asset metadata consistent with SS14 conventions, and use the reference files to place changes in the correct subtree.

## Workflow

1. Open [resources-map.md](references/resources-map.md) first.
- Use it to choose the correct `Resources/` subtree, understand folder naming drift, and find fork-specific `_Orion` content.

2. Open [prototype-locale-checklist.md](references/prototype-locale-checklist.md) before editing.
- Use it for prototype header order, FTL naming, asset metadata, and validation commands.

3. Put data in the right subtree.
- Prototypes live under `Resources/Prototypes/<Area>/`
- Locale lives under `Resources/Locale/<culture>/`
- Sprites and RSIs live under `Resources/Textures/`
- Audio definitions and attributions live under `Resources/Audio/`
- Maps live under `Resources/Maps/`

4. Follow SS14 YAML conventions.
- Keep entity prototype header order as `type`, `abstract`, `parent`, `id`, `categories`, `name`, `suffix`, `description`, `components`.
- Do not insert blank lines between component `- type:` entries inside `components:`.
- Separate prototype blocks with one blank line.
- Prefer no quotes on `name:` and `description:` unless punctuation requires them.
- Use inline lists only where already conventional, such as compact `parent` or `categories` lists.
- Use `suffix` for spawn-menu distinctions instead of changing `name`.
- Put new prototype parents in `base.yml` when you create a new parent tree.

5. Treat localization as required work.
- Every player-facing string needs FTL.
- Use specific `kebab-case` keys.
- Start with `en-US`.
- Update other locales only when the task clearly requires it or you can do so confidently without inventing wording.

6. Handle assets carefully.
- Prefer sound collections and specifiers over hardcoded asset paths in gameplay code when reusable content is intended.
- Keep RSI `meta.json` ordered as `version`, `license`, `copyright`, `size`, `states` with 4-space indentation.
- Keep assets self-hosted in the repo; do not point docs or content at random external hosts.

7. Validate the change.
- `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt`
- `python3 RobustToolbox/Schemas/validate_rsis.py Resources/` after RSI edits
- If map files changed, expect schema validation in CI and keep map-only changes isolated when practical

## Reference Map

- `references/resources-map.md`: subtree map for prototypes, locale, textures, audio, maps, and `_Orion` additions in this fork.
- `references/prototype-locale-checklist.md`: editing checklist, naming rules, validation commands, and code-to-data pairing reminders.
- `../ss14-prototype-basics/references/prototype-primer.md`: first-principles prototype routing and inheritance.
- `../ss14-prototype-basics/references/common-prototype-components.md`: common YAML-side components and what they do.
- `../ss14-graphics-generic-visualizer-appearance/references/visual-prototype-anchors.md`: appearance and visual state anchors.

## Useful References

- `../ss14-ecs-prototypes/references/item-and-structure-examples.md`
- `../ss14-ecs-prototypes/references/reagent-examples.md`
- `../ss14-localization-strings/references/prototype-and-marking-examples.md`

## Common Pitfalls

- Missing localization for a new prototype or UI string.
- YAML indentation drift inside `components:`.
- Putting new parent prototypes into arbitrary leaf files instead of `base.yml`.
- Changing a prototype `name` when a `suffix` would do.
- Hand-editing generated changelog output instead of preparing PR changelog text when needed.
