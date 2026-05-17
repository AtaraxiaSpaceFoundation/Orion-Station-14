<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Selectors And Entity Args

## Use FTL For Grammar

- plural forms
- selectors
- entity-aware helpers such as article and capitalization functions

## Anchor

- `Resources/Locale/en-US/wieldable/wieldable-component.ftl`

## Pairing Anchor

- `Content.Shared/Wieldable/SharedWieldableSystem.cs`

## Reminder

If the text depends on count, actor, or item entity grammar, solve it in FTL rather than branching into many hardcoded strings.
