<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# ECS Primer

## Entity

- an `EntityUid`, which is the runtime identifier for one entity instance
- just an identifier and container for components

## Component

- data only
- marks the entity as having some property or state
- should not own gameplay logic

## System

- owns behavior
- subscribes to events
- reads or mutates components

## Events

- signals that something happened
- systems subscribe and react
- often the cleanest way to connect behavior to gameplay flow

## Rule

If you are wondering where a piece of gameplay logic should live, the answer is almost always “in a system, not in the component”.
