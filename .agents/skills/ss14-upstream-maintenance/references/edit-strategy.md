<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Edit Strategy

## Purpose

Use this file when deciding where and how to implement a change in a fork.

## Rules

- Do not edit `RobustToolbox/` for normal gameplay, UI, prototype, or localization work.
- Touch `RobustToolbox/` only when the task explicitly calls for engine work and there is no content-side solution.
- Prefer extending content code before changing engine code.
- Keep diffs narrow inside upstream files.
- Preserve existing ordering, spacing, and local style in touched files.
- When creating Orion-only behavior, prefer `_Orion` paths that mirror the upstream feature layout. When the feature already belongs to another inherited tree, extend that owner instead.
- When adding or changing Orion-specific code in an inherited file outside the owning `_Orion` path, mark it when the surrounding style uses edit markers or the change would otherwise be hard to distinguish:
  - Single added or changed line: append `// Orion` as an inline comment.
  - Multiple lines: wrap with block markers:

```csharp
// Orion-Edit-Start
...code here...
// Orion-Edit-End
```

- Keep edit-marker ranges as narrow as practical. Do not wrap unrelated upstream code or whole files when only a small block belongs to Orion.
- Multiple marked blocks in one file are allowed when the Orion changes are separated by upstream code.
- For files that do not use `//` comments, use the native comment syntax while preserving `Orion-Edit-Start`, `Orion-Edit-End`, and `Orion-Edit`.
- Prefer reusable hooks, helpers, and data-driven content over one-off special cases or hardcoded values.

## Good Patterns

- Add fork-specific systems or resources beside the upstream feature in `_Orion`.
- Extend an existing public system API instead of rewriting the whole feature.
- Mark narrow Orion-specific edits in inherited files with paired `Orion-Edit` comments.
- Add only the fields, prototypes, and locale entries required for the task.
- Move repeated or feature-configurable behavior into reusable APIs, component data, or prototypes.

## Bad Patterns

- Moving a large amount of upstream code into fork-only files without need.
- Refactoring unrelated upstream code while fixing a small gameplay issue.
- Leaving hard-to-identify Orion-specific code in inherited files when nearby style expects edit markers.
- Editing engine code because it feels cleaner than respecting content boundaries.
- Solving one call site with hardcoded IDs, strings, paths, or special-case branches when the code should stay reusable.
