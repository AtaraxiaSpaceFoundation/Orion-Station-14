<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Gameplay Feature Checklist

Use this before finishing a gameplay task.

## Code

- component data-only
- system owns behavior
- correct shared/server/client placement
- `On... -> Try... -> Can... -> Do...` flow where applicable

## Data

- prototypes updated if serialized fields or content tuning changed
- visuals or sound collections updated if the feature needs them
- fork-only additions placed under `_Orion` when appropriate

## Text

- every player-facing string localized
- `en-US` updated in the same pass

## Validation

- build or targeted tests run
- YAML linter run for prototype or FTL changes
- runtime or in-game verification called out if not possible
