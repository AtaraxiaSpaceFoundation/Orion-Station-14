<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# First Prototype Workflow

## Practical Loop

1. Find a similar in-game object or prototype.
2. Look up its prototype ID and source file.
3. Parent from the closest existing prototype.
4. Override only the fields or components that actually differ.
5. Spawn and test the new prototype.

## Real Repo Anchors

- `Resources/Prototypes/Entities/Objects/base_item.yml`
- `Resources/Prototypes/Entities/foldable.yml`
- `Resources/Prototypes/_Orion/Entities/Mobs/Customization/Markings/human_tails.yml`

## Good Habits

- Search before creating a new file or folder.
- Keep file names and placement logical.
- If the entity is visible to players, update locale in the same pass.

## Common Failure Modes

- wrong indentation
- duplicated ID
- inventing a new parent tree when one already exists
- copying a full parent instead of inheriting from it
