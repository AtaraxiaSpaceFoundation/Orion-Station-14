<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prediction Checklist

## Ask These Questions

- Should the local player see the result immediately?
- Does the client already have enough state to predict or render this?
- Does the replicated data belong in a shared networked component?
- Do client and server need specialized concrete systems under a shared base?

## Required Patterns

- Put predicted logic in `Content.Shared/`.
- Use networked shared components for synced predicted state.
- Use `Dirty(...)` or `DirtyField(...)` when authoritative data changes.
- Use `PopupPredicted`, `PlayPredicted`, predicted spawn/delete, and predicted BUI messages where appropriate.

## Common Mistakes

- Leaving a user-facing action server-only.
- Calling server-only effects from predicted shared code.
- Forgetting that predicted client code may run multiple times.
- Syncing reference-heavy state without considering prediction safety.
