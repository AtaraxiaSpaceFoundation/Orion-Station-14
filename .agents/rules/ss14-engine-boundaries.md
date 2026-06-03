<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Engine Boundaries

Use this rule whenever a task smells like it might require engine work.

## Default Position

- Do not edit `RobustToolbox/` unless the task explicitly requires engine behavior changes.
- Assume gameplay, prediction, UI, prototype, and localization issues belong in content code first.
- Treat engine edits as escalation, not cleanup.

## Before Touching Engine Code

1. Confirm the issue cannot be solved in `Content.Shared/`, `Content.Server/`, `Content.Client/`, or `Resources/`.
2. Check whether Orion already has a matching `_Orion` extension point, or whether the feature belongs to an existing inherited subtree such as `_Goobstation`, `_EinsteinEngines`, `_Shitmed`, `_DV`, `_NF`, or another present owner.
3. Prefer extending an existing public content API over patching engine internals.

## When Upstream Content Must Change

- Keep the diff narrow.
- Preserve ordering, spacing, and nearby style.
- Avoid opportunistic refactors in unrelated upstream files.

## Good Escalations

- Missing engine hook with no content-side workaround.
- Serialization or prediction primitive genuinely absent from the engine.
- UI or rendering capability unavailable from content code.

## Bad Escalations

- Editing engine code because it feels cleaner than using the existing content architecture.
- Moving a fork-only gameplay rule into the engine.
- Refactoring engine internals while fixing a small gameplay bug.
