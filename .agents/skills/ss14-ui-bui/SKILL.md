<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ui-bui
description: Work with SS14 Bound User Interfaces, BUI state and messages, UI keys, `BoundUserInterface` client classes, server-side UI handlers, predicted BUI actions, and shared BUI contracts.
---

# SS14 BUI

Use this skill for entity-bound UI flows. Load `ss14-ui-xaml` as well when editing the paired window layout.

BUIs sit on top of entity state and network messages. Keep the server authoritative, use predicted messages for immediate local actions, and avoid duplicating state that the client already receives through networked components.

## Workflow

1. Trace the whole BUI chain before editing.
- Shared: UI key, state, messages, component state, and predicted contracts.
- Server: authority, validation, state construction, and message handling.
- Client: `BoundUserInterface`, window creation, state application, and outgoing messages.

2. Keep state ownership clear.
- Prefer reading existing networked component state when the client already has it.
- Add BUI state only for data that is actually UI-specific or not otherwise replicated.
- Dirty authoritative component state when server actions change replicated data.

3. Use predicted BUI paths when appropriate.
- Use predicted messages for local player actions that should feel immediate.
- Keep predicted shared logic deterministic and safe to replay.
- Avoid duplicate popups, audio, or visual feedback between prediction and authoritative updates.

4. Validate all incoming requests.
- Treat client BUI messages as requests.
- Re-check entity validity, access, distance, permissions, and component state on the server.
- Handle closed UI, deleted owner, and stale target cases.

5. Localize and verify.
- Localize all window text, buttons, labels, and feedback.
- Build affected shared/server/client projects.
- Call out when the UI still needs an in-game pass.

## Reference Map

- `references/ui-flow-map.md`
- `references/predicted-bui-patterns.md`
- `../ss14-ui-xaml/SKILL.md`
- `../ss14-prediction/references/bui-prediction.md`
- `../ss14-netcode/SKILL.md`
- `../ss14-localization-strings/SKILL.md`

## Useful References

- `references/ui-flow-map.md`
- `references/predicted-bui-patterns.md`
- `references/xaml-window-patterns.md`

## Good File Anchors

- `Content.Client/**/*BoundUserInterface*.cs`
- `Content.Shared/**/*Bui*.cs`
- `Content.Shared/**/*Ui*.cs`
- `Content.Server/**/*System*.cs`
- `Content.Client/**/*.xaml`

## Common Pitfalls

- Trusting client BUI messages without server-side validation.
- Duplicating networked component state in a separate BUI state without a reason.
- Sending non-predicted messages for immediate local actions.
- Forgetting localization for labels, buttons, and feedback.
