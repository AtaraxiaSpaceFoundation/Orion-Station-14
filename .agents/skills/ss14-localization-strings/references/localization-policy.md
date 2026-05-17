<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Localization Policy

## Rules

- Every player-facing string must be localized.
- Add new text to `Resources/Locale/en-US/` first.
- Use specific `kebab-case` IDs.
- Do not compare localized strings in code.
- Do not expose raw enum `ToString()` output to players.

## C# Usage

- Prefer `Loc.GetString("feature-key", ("arg", value))`.
- Keep arguments named clearly and consistently with the FTL entry.

## Prototype Usage

- Pair new prototype-facing text with matching FTL when the prototype or its UI is visible to players.
