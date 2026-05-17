<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Entity API Patterns

## Purpose

Use this file to choose the right entity and component access pattern before writing gameplay code.

## Preferred Patterns

- Public system APIs that act on an entity should usually take `EntityUid` or `Entity<T?>` first.
- If the caller already has both the uid and component, prefer `Entity<T?>`.
- Call `Resolve(...)` early when a method accepts optional component parameters.
- Use `TryComp(...)` for conditional behavior and guard clauses.

## Naming

- `uid` or `ent` for the acted-on entity
- `user` for the actor
- `used` for the item being used
- `target` for the thing being acted on

## Example Anchors

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `Content.Shared/Wieldable/Components/WieldableComponent.cs`

## Common Mistakes

- Carrying separate uid/component parameters long after a pair already exists.
- Delaying `Resolve(...)` until the middle of the method.
- Using `EntityUid.Invalid` instead of nullable entity references.
