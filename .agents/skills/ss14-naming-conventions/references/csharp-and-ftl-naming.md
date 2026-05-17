<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# C# And FTL Naming

## C# Methods

- Event handlers: `OnSomething`
- Public action API: `TrySomething`
- Checks: `CanSomething`
- Refresh/aggregation hooks: `Refresh...`, `Get...`, `Update...`

## Fields And Dependencies

- Dependency fields use underscore-prefixed names such as `_popup`, `_audio`, `_hands`.
- New component fields should use clear domain names instead of abbreviations.

## Types

- Components should end with `Component`.
- Systems should end with `System`.
- Events should end with `Event`.
- Prototype types should end with `Prototype` where that is the project norm.

## Localization

- Use specific `kebab-case` IDs.
- Keep IDs feature-scoped, for example `wieldable-component-not-in-hands`.
- Prefer stable semantic IDs instead of sentence fragments.

## Prototype IDs

- Match the local domain naming style.
- Prefer descriptive IDs over short or joke abbreviations unless the feature already uses them.
