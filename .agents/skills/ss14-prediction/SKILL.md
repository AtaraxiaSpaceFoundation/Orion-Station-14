<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-prediction
description: Implement or review predicted gameplay and networked state in SS14. Use when a player action should feel immediate, when touching networked components, predicted popups or audio, predicted BUI flows, or shared client/server system splits.
---

# SS14 Prediction

Use this skill when prediction, network sync, or shared/client/server split decisions matter.

## Workflow

1. Open `references/prediction-checklist.md`.
2. Open `references/predicted-feedback.md` for popups, audio, and immediate player feedback.
3. Open `references/networked-component-state.md` for replicated fields.
4. Open `references/bui-prediction.md` for predicted UI paths.
2. Keep predicted logic in `Content.Shared/`.
3. Dirty networked state whenever authoritative values change.
4. Use predicted APIs instead of server-only equivalents where player feedback should be immediate.

## Reference Map

- `references/prediction-checklist.md`
- `references/predicted-feedback.md`
- `references/networked-component-state.md`
- `references/bui-prediction.md`
- `../ss14-gameplay-feature/references/prediction-and-cross-assembly.md`
- `../ss14-client-server-shared/references/shared-and-prediction.md`
