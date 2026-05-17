<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prompting Patterns

## Better Prompts

- ask for changes relative to an existing nearby system
- mention prediction, localization, reuse, and engine-boundary constraints explicitly
- ask for a reusable system API or data-driven solution, not a one-off branch for one call site
- say explicitly when hardcoded IDs, strings, paths, magic numbers, or special-case behavior are not acceptable
- give the actual file context or surrounding code when the bug is subtle

## Example Shapes

- explain this code and point out likely risks
- write tests for this system, including misuse cases and limits
- refactor this code without changing behavior
- use system X as the pattern and keep the result predicted and reusable
- extend system X with a reusable helper; do not touch `RobustToolbox/` and do not hardcode the new case

## Rule

“Write the whole feature” is usually worse than “follow this nearby pattern and satisfy these specific SS14 constraints”.
