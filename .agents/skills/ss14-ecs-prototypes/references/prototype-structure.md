<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prototype Structure

## Purpose

Use this file to place and structure new or edited prototypes correctly.

## Placement Rules

- Use the most specific existing subtree in `Resources/Prototypes/`.
- Put parent trees in `base.yml` when creating a new parent family.
- Keep fork-specific content under `_Orion/` when it is genuinely fork-only.

## Entity Prototype Order

Keep the header order:

- `type`
- `abstract`
- `parent`
- `id`
- `categories`
- `name`
- `suffix`
- `description`
- `components`

## Content Coupling

- New player-facing names or descriptions need FTL.
- New visuals often need sprites or `GenericVisualizer` setup.
- New sounds should prefer sound collections when reused.

## Validation

- `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- `py -3 Schemas/validate_rsis.py Resources` after RSI edits
