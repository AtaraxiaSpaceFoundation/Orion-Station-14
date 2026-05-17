<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-standard-optimizations
description: Optimize SS14 gameplay code in common hot paths. Use when working on Update loops, frequently raised events, high-volume entity iteration, prediction-heavy client code, or other gameplay code where standard SS14 performance patterns matter.
---

# SS14 Standard Optimizations

Use this skill when performance matters, especially in frequent event handlers or update paths.

## Workflow

1. Open `references/optimization-checklist.md`.
2. Open `references/event-hotpath-patterns.md` for frequent handlers.
3. Open `references/entity-query-patterns.md` for high-volume iteration.
2. Keep the optimization local to the actual hot path.
3. Do not trade away correctness or prediction safety for micro-optimizations without evidence.

## Reference Map

- `references/optimization-checklist.md`
- `references/event-hotpath-patterns.md`
- `references/entity-query-patterns.md`
- `../ss14-debugging-workflow/references/debugging-mindset.md`
