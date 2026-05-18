<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Optimization Checklist

## Use This Skill For

- `Update()` paths
- very frequent `SubscribeLocalEvent` handlers
- large entity iteration
- prediction-heavy client code

## Rules

- Avoid unnecessary allocations in hot paths.
- Reuse established aggregation or refresh events instead of adding new polling loops.
- Keep handlers narrow and return early.
- Optimize the measured or obvious hotspot, not unrelated code nearby.

## Common Mistakes

- Prematurely rewriting architecture for a small micro-optimization.
- Adding hidden complexity to cold paths.
- Breaking prediction or readability for negligible gains.
