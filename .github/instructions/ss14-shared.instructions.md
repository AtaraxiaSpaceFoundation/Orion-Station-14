<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
applyTo: "Content.Shared/**/*.cs,Content.Goobstation.Shared/**/*.cs,Content.Goobstation.Common/**/*.cs,Content.Goobstation.Maths/**/*.cs"
---

For shared content code (`Content.Shared` and existing shared Goobstation modules):

- Load `ss14-ecs-components`, `ss14-ecs-entities`, `ss14-ecs-systems`, `ss14-events`, `ss14-prediction`, and `ss14-netcode`.
- Also load `ss14-localization-code` when the shared code stores `LocId` or emits player-facing text.
- Keep shared code limited to data, events, prediction, and replicated state both sides must understand.
- Do not add direct server-only or client-only dependencies.
