<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prototype And Locale Checklist

## Before Editing

1. Find the narrowest existing prototype folder for the feature.
2. Find the matching locale area instead of guessing from the prototype folder name.
3. Check for existing parent prototypes or base trees before adding a new parent.
4. Check whether code should use `ProtoId<T>`, a sound collection, or an RSI sprite specifier instead of a raw string.

## Prototype Rules

- Keep entity prototype header order as `type`, `abstract`, `parent`, `id`, `categories`, `name`, `suffix`, `description`, `components`.
- Keep one blank line between prototype blocks.
- Do not insert blank lines between component `- type:` entries.
- Put new parent trees in `base.yml` and keep variants in sibling files.
- Prefer `suffix` for spawn-menu distinctions.
- Prefer existing field naming and component order from nearby prototypes.

## Localization Rules

- Localize every player-facing string.
- Use specific `kebab-case` FTL keys.
- Start with `Resources/Locale/en-US/`.
- Do not compare localized strings in code.
- Do not expose raw enum `ToString()` output to players.

## Asset Rules

- Keep RSI `meta.json` ordered as `version`, `license`, `copyright`, `size`, `states`.
- Use 4-space indentation in RSI metadata.
- Prefer reusable sound collections over ad-hoc hardcoded audio paths.
- Keep repo assets self-hosted; do not point content or docs at random external hosts.

## Frequent Pairings

- New prototype plus UI text: prototype file plus matching `.ftl`.
- New reusable sound: audio asset plus `SoundCollections/` prototype plus any code or entity references.
- New marking or sprite-backed entity: prototype plus RSI plus localized display name.
- New map content: map file plus any entity prototypes, decals, textures, or localized labels it introduces.

## Validation

- `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- `py -3 Schemas/validate_rsis.py Resources` after RSI edits
- Run targeted gameplay or UI verification when a prototype changes behavior rather than only data values

## Common Mistakes

- Editing prototypes without checking if locale already exists nearby.
- Creating a new top-level prototype folder when a narrower existing subtree already fits.
- Copying an existing prototype block but forgetting to rename IDs, parents, or localization keys consistently.
- Leaving stale asset references after renaming an RSI or moving textures.
