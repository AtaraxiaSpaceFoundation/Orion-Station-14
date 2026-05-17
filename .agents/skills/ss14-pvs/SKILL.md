<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-pvs
description: Work with SS14 PVS, visibility, network interest, PVS filters, PVS override behavior, visibility-dependent audio or popups, zoom PVS scale, or systems that behave differently when entities leave PVS.
---

# SS14 PVS

Use this skill when a change depends on which clients can see, hear, or receive an entity.

PVS is a networking and relevance boundary, not just a visual effect. Keep filters narrow, avoid leaking hidden information, and remember that clients may not know about entities outside their PVS.

## Workflow

1. Decide what must be relevant.
- World feedback for nearby players should usually use PVS filters or PVS-scoped helpers.
- Admin, ghost, camera, or debug visibility may require explicit override behavior.
- UI or client code must tolerate entities leaving PVS or arriving later.

2. Keep network interest narrow.
- Prefer `Filter.Pvs(...)`, `Filter.PvsExcept(...)`, and existing audio/popup helpers over broadcasting to all players.
- Do not use global filters for local world feedback unless the feature truly is global.
- Be cautious with PVS overrides; remove or scope them when the special view ends.

3. Do not assume the client has every entity.
- Client code should handle missing entities, nullspace, and stale entity references.
- Avoid client decisions that require hidden server-only or out-of-PVS entities unless a deliberate message/state path provides the data.

4. Coordinate with prediction and feedback.
- Predicted local feedback may happen before server PVS feedback.
- Avoid duplicate popups or sounds when both local prediction and PVS broadcasts exist.

5. Validate visibility behavior.
- Test with another client or call out when multi-client verification was not possible.
- For admin/debug visibility, check cleanup paths as carefully as setup paths.

## Reference Map

- `../ss14-netcode/SKILL.md`
- `../ss14-prediction/SKILL.md`
- `../ss14-audio/SKILL.md`
- `../ss14-debugging-workflow/SKILL.md`

## Good File Anchors

- `Content.Server/**/Pvs*.cs`
- `Content.Server/**/PvsOverride*.cs`
- `Content.Client/**/ContentEye*.cs`
- `Content.Server/**/Filter*.cs`
- `Content.Client/**/Overlay*.cs`

## Common Pitfalls

- Broadcasting feedback globally when PVS or station/PVS filters are enough.
- Assuming a client can resolve an entity that may have left PVS.
- Leaving PVS overrides active after an admin camera, ghost, or debug view closes.
- Treating PVS as security for data that should never be sent.
