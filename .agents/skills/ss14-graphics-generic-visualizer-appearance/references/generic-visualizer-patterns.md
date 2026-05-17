<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# GenericVisualizer Patterns

## Use This For

- layer visibility toggles
- enum or boolean driven sprite state changes
- simple appearance-to-layer mapping fully expressed in prototypes

## Example Anchors

- `Resources/Prototypes/Entities/foldable.yml`
- `Resources/Prototypes/Entities/Objects/base_item.yml`

## Rules

- Keep the gameplay state source separate from the visual layer mapping.
- Use `Appearance` plus `GenericVisualizer` when the existing project pattern can express the change declaratively.
- Avoid a custom client visualizer when a simple layer toggle is enough.
