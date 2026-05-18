<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Simple System Walkthrough

## Reading Order

1. Find the component class.
2. Find the system class.
3. Open `Initialize()` to see dependency wiring and subscriptions first.
4. Inspect each `SubscribeLocalEvent(...)`.
5. Follow the handlers and public methods.
6. Check what component state or side effects change afterward.

## Example Anchor

- `Content.Shared/Wieldable/Components/WieldableComponent.cs`
- `Content.Shared/Wieldable/SharedWieldableSystem.cs`

## What To Notice

- dependencies: which other systems or services this system relies on
- subscriptions: which events enter the system and where the flow starts
- guard clauses: which early returns define the allowed conditions
- public action methods: the reusable APIs other systems, verbs, or handlers may call
- component mutation and synchronization: what state actually changes and whether it is dirtied or replicated
