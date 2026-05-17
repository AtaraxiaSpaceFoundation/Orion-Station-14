<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
applyTo: "Content.Tests/**/*.cs,Content.IntegrationTests/**/*.cs"
---

For tests:

- Load `ss14-tests-authoring`.
- Prefer the smallest test layer that meaningfully covers the behavior.
- Use `Content.Tests` for parsing, shared logic, and content-loading checks.
- Use `Content.IntegrationTests` only when the behavior truly depends on runtime orchestration or multi-side interaction.
