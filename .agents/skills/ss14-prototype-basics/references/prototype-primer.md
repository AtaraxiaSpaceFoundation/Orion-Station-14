<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prototype Primer

## What A Prototype Is

- In SS14, a prototype is the main YAML-side description of a game object or other content definition.
- For entities, the prototype is the skeleton and the components are the properties attached to that skeleton.
- Code, maps, spawn tools, and other prototypes reference the prototype by `id`.

## Where Prototypes Live

- `Resources/Prototypes/...`
- Common family examples:
  - `Resources/Prototypes/Entities/...`
  - `Resources/Prototypes/Reagents/...`
  - `Resources/Prototypes/SoundCollections/...`
  - `Resources/Prototypes/_Orion/...`

## Core Fields

- `type`: what kind of prototype this is, usually `entity`
- `id`: unique identifier, usually PascalCase
- `parent`: inherit from another prototype to avoid duplication
- `name` and `description`: player-facing text, usually localized or tied to localization
- `components`: the list of behaviors and data attached to the entity

## Rule

Do not think “blank YAML file first”. Think “find the nearest existing parent and extend it”.
