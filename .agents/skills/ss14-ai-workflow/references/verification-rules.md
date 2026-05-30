<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Verification Rules

## Always

- read the generated code
- compare it to nearby repo patterns
- run the smallest meaningful validation
- report the checks you ran in the PR or final handoff
- call out intentional deviations from the nearest repo pattern
- verify the change is reusable from multiple call sites instead of solving only the immediate one
- verify the code did not slip in hardcoded IDs, strings, paths, or magic values without a repo-pattern reason
- distrust unexplained architectural changes

## For SS14 Specifically

- verify prediction-sensitive behavior
- verify localization was not skipped
- verify new serialized fields are paired with prototypes
- verify server/client/shared ownership still makes sense
- verify the solution stayed in content code and did not drift into `RobustToolbox/` without an explicit engine requirement

## Rule

Responsibility does not transfer to the model. The merged code is still your code.
