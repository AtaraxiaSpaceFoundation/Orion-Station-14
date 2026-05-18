<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Network Event Patterns

## Use These For

- `SubscribeNetworkEvent<T>(...)`
- `[Serializable, NetSerializable]` payloads
- client-to-server requests and server-to-client notifications

## Example Anchors

- `Content.Shared/Administration/SharedBwoinkSystem.cs`
- `Content.Shared/Actions/ActionEvents.cs`

## Rules

- Network events inherit `EntityEventArgs`.
- Mark them `[Serializable, NetSerializable]`.
- Keep payloads small and explicit.
- Prefer clear request/response naming over vague transport names.

## Common Mistakes

- Putting client-only or server-only types into shared network payloads.
- Sending full entities instead of `NetEntity` or replicated state references.
- Hiding authoritative checks in a client-only path.
