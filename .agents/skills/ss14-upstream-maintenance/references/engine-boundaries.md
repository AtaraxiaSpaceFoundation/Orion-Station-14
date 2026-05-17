<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Engine Boundaries

## Default

- do not touch `RobustToolbox/` for routine content work
- prefer content-side solutions first
- treat engine edits as a last-resort escalation, not a convenience cleanup
- escalate to engine edits only when the required hook or primitive is genuinely missing

## Good Reasons To Escalate

- missing engine capability with no content-side workaround
- missing serialization, prediction, or rendering primitive

## Bad Reasons To Escalate

- the engine edit looks cleaner than respecting content boundaries
- the task is fork-only gameplay behavior
- the agent wants a fast one-off fix instead of designing a reusable content-side solution
