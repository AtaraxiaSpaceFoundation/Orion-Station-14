<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Common Prototype Components

## `Sprite`

- points to the RSI and default state used for rendering
- common keys: `sprite`, `state`

## `Item`

- makes an entity behave like a carried item
- size matters for storage and pockets

## `Tag`

- lightweight labels used by systems, whitelists, and other content

## `Storage`

- controls storage grids and capacity
- can override inherited storage behavior

## Useful Reminder

Many prototype behaviors are inherited. When you add `Storage` or another component to a child, you may be replacing or extending something that already exists on the parent.
