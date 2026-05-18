<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-tests-authoring
description: Add or choose SS14 test coverage for gameplay, content, and integration changes. Use when deciding whether a change needs `Content.Tests`, `Content.IntegrationTests`, YAML validation, or only a targeted build/runtime pass, and when looking for in-repo test anchors to copy.
---

# SS14 Tests Authoring

Use this skill when a gameplay or content change needs meaningful verification.

## Workflow

1. Open `references/test-selection.md`.
2. Open `references/content-test-anchors.md` for lightweight content and shared tests.
3. Open `references/integration-test-anchors.md` for multi-side or end-to-end coverage.
4. Pick the smallest test layer that actually exercises the risk.

## Reference Map

- `references/test-selection.md`
- `references/content-test-anchors.md`
- `references/integration-test-anchors.md`
- `../ss14-gameplay-feature/references/prediction-and-cross-assembly.md`
