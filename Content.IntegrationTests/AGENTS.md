<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Content.IntegrationTests Agent Notes

Read [../AGENTS.md](../AGENTS.md) first.

For `Content.IntegrationTests` work always load:

- `ss14-tests-authoring`
- `ss14-naming-conventions`
- `ss14-upstream-maintenance`

Use integration tests when the behavior depends on runtime orchestration, entity lifecycle, prediction, or server-client interaction. Keep them focused and avoid escalating from a simpler content test without need.
