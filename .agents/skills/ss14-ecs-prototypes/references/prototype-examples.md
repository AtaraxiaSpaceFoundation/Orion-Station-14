<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prototype Examples

Use these in-repo anchors when you need a concrete pattern.

## Simple Item Base

- `Resources/Prototypes/Entities/Objects/base_item.yml`
- Use this when you need the baseline item stack of `Item`, physics, sprite, pullable, and common sounds.

## Simple Structure / Foldable Visual State

- `Resources/Prototypes/Entities/foldable.yml`
- Shows a small entity tree plus `GenericVisualizer` state switching.

## Reagent YAML

- `Resources/Prototypes/Reagents/biological.yml`
- Shows a reagent prototype with `name`, `desc`, metabolisms, plant reactions, and sound hooks.

## GenericVisualizer Pattern

- `Resources/Prototypes/Entities/Objects/base_item.yml`
- `Resources/Prototypes/Entities/foldable.yml`
- Use these as anchors for appearance-driven layer changes.

## Localization Pairing

- `Resources/Locale/en-US/wieldable/wieldable-component.ftl`
- Use it as a straightforward example of gameplay strings consumed from C#.

## Reminders

- Do not copy-paste blindly. Match the nearest feature pattern instead.
- If the example is from a parent prototype, keep only the parts your child actually needs.
