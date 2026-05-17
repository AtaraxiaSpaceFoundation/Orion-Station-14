<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Try Can Do Pattern

## Required Flow

- `On...` handler routes
- `Try...` is the reusable public API
- `Can...` checks without state mutation
- execution mutates state

## Example Anchor

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`
- `.agents/rules/ss14-interaction-flow.md`

## Reminder

Use this even for small features so verbs, UI, and other systems can reuse the same public action path.
