<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# GenericVisualizer Examples

## Anchors

- `Resources/Prototypes/Entities/foldable.yml`
- `Resources/Prototypes/Entities/Objects/base_item.yml`

## Use These For

- `Appearance` plus `GenericVisualizer`
- layer visibility changes driven by enum or boolean state
- reusable visualized parent prototypes

## State Choice

- Use a boolean appearance value for simple on/off layer visibility.
- Use an enum appearance value when one key needs to drive multiple visual states.

## Reminder

Prefer the declarative visualizer path when a layer toggle or simple state map is enough.
