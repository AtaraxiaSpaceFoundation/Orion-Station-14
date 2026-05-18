<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Appearance Data Flow

## Typical Shape

1. Gameplay state changes in a shared or authoritative system.
2. The state is exposed through an appearance component or networked visual field.
3. A prototype `GenericVisualizer` maps the appearance value to sprite-layer changes.
4. The client renders the updated layers.

## Practical Rules

- Put gameplay truth in components and systems, not in the visualizer definition.
- Keep visual enums or keys stable and feature-scoped.
- If the client only needs a visible state toggle, avoid adding a separate BUI or unrelated visual channel.

## Good Anchors

- `Resources/Prototypes/Entities/foldable.yml`
- `Resources/Prototypes/Entities/Objects/base_item.yml`
