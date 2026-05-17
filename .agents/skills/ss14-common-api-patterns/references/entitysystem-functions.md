<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# EntitySystem Functions

## Most Common Helpers

- `HasComp<T>(uid)`
- `Comp<T>(uid)`
- `CompOrNull<T>(uid)`
- `TryComp<T>(uid, out comp)`
- `Resolve(ent, ref ent.Comp)`
- `AddComp<T>(uid)`
- `EnsureComp<T>(uid)`
- `RemComp<T>(uid)`
- `RemCompDeferred<T>(uid)`
- `Del(uid)`: delete immediately when the current context is safe for it
- `QueueDel(uid)`: queue deletion for later; prefer inside handlers, during iteration, or when in doubt
- `Transform(uid)`
- `MetaData(uid)`
- `Name(uid)`
- `Prototype(uid)`
- `EntityQueryEnumerator<...>()`

## Rules

- prefer `TryComp` for guard clauses
- prefer `Resolve` when your method already takes `Entity<T?>`
- prefer `EnsureComp` over manual existence-then-add patterns
- prefer `QueueDel` inside event handlers
