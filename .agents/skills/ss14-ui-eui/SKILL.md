<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ui-eui
description: Work with SS14 EUI flows, `BaseEui`, `EuiStateBase`, `EuiMessageBase`, admin/debug windows, EUI state serialization, and client/server EUI message handling.
---

# SS14 EUI

Use this skill for engine-style EUI windows, especially admin and debug tools.

EUI is for explicit client/server UI sessions. Keep messages typed, state minimal, permission-sensitive behavior server-authoritative, and windows tolerant of close/reopen cycles.

## Workflow

1. Map the EUI chain.
- Client: `BaseEui`, window/control, `HandleState`, and `SendMessage`.
- Shared: state and message types when both sides need the contract.
- Server: session setup, permission checks, state construction, and message handling.

2. Keep authority server-side.
- Validate permissions and target entities on the server for admin/debug actions.
- Treat client messages as requests, not proof that an action is allowed.
- Avoid sending sensitive data unless the viewer is authorized.

3. Keep state and messages small.
- Send IDs, summaries, and requested page data instead of huge object graphs.
- Use `NetEntity` or stable identifiers for entities crossing the wire.
- Handle stale targets, disconnects, and windows closing.

4. Pair EUI with XAML carefully.
- Use `ss14-ui-xaml` for window layout and localized text.
- Keep EUI message/state logic out of code-behind when a system can own it.

5. Validate with the real role.
- Admin/debug EUI needs permission-aware runtime testing when possible.
- Call out when the UI was built but not exercised in-game.

## Reference Map

- `../ss14-ui-xaml/SKILL.md`
- `../ss14-netcode/SKILL.md`
- `../ss14-localization-strings/SKILL.md`
- `../ss14-debugging-workflow/SKILL.md`

## Good File Anchors

- `Content.Client/**/*Eui.cs`
- `Content.Server/**/*Eui.cs`
- `Content.Shared/**/*Eui*.cs`
- `Content.Client/Administration/UI/**`

## Common Pitfalls

- Trusting client EUI messages for admin actions.
- Sending too much state every update.
- Forgetting close and stale-entity paths.
- Mixing BUI and EUI patterns without a reason.
