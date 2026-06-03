<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
applyTo: "Content.Server/**/*.cs,Content.Goobstation.Server/**/*.cs,Content.Server.Database/**/*.cs"
---

For server content code (`Content.Server`, `Content.Goobstation.Server`, and server database code):

- Keep authority, round rules, persistence, and server-only side effects here.
- If the local player should feel the action immediately, verify the shared prediction path instead of leaving the behavior server-only by accident.
- Load `ss14-tests-authoring` when the behavior change is risky enough to warrant coverage.
- Pair player-facing server behavior with prototype and locale updates in the same change.
