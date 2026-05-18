<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-netcode
description: Work with SS14 network events, `NetEntity`, replicated state, and shared/server/client routing. Use when adding or reviewing `[NetSerializable]` events, `SubscribeNetworkEvent`, networked component state, or deciding which assembly owns a networked message or replicated field.
---

# SS14 Netcode

Use this skill when a change crosses the wire or changes the shape of replicated state.

## Workflow

1. Open `references/network-event-patterns.md`.
2. Open `references/netentity-and-state.md` for `NetEntity` and replicated component data.
3. Open `references/shared-server-client-routing.md` before deciding where the event or state belongs.
4. Keep prediction and netcode decisions aligned.

## Reference Map

- `references/network-event-patterns.md`
- `references/netentity-and-state.md`
- `references/shared-server-client-routing.md`
- `../ss14-prediction/references/networked-component-state.md`
- `../ss14-client-server-shared/references/networking-and-dirty.md`
