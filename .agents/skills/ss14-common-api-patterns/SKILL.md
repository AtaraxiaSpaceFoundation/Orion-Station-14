<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-common-api-patterns
description: Use common SS14 gameplay helper APIs correctly. Use when choosing between `TryComp`, `Resolve`, `EnsureComp`, `Spawn`, `EntityQueryEnumerator`, prototype lookups, audio methods, popup methods, or safe randomness patterns in gameplay code.
---

# SS14 Common API Patterns

Use this skill for the “which helper do I call here?” layer of SS14 coding.

## Workflow

1. Open `references/entitysystem-functions.md`.
2. Open `references/prototype-manager-and-spawn.md` for prototype and spawn helpers.
3. Open `references/audio-popup-random.md` for common gameplay side-effect APIs.
4. Prefer the existing `EntitySystem` helper over manually resolving lower-level managers when possible.

## Reference Map

- `references/entitysystem-functions.md`
- `references/prototype-manager-and-spawn.md`
- `references/audio-popup-random.md`
- `../ss14-prediction/references/predicted-feedback.md`
