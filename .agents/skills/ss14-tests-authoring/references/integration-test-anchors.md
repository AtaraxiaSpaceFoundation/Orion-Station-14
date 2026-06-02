<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Integration Test Anchors

## Good Starting Points

- `Content.IntegrationTests/PoolManager.cs`
- `Content.IntegrationTests/Pair/TestPair.cs`
- `Content.IntegrationTests/Tests/ResearchTest.cs`
- `Content.IntegrationTests/Tests/Station/StationJobsTest.cs`
- `Content.IntegrationTests/Tests/Goobstation/UplinkPreferenceTests.cs`
- `Content.IntegrationTests/Tests/_Orion/CustomGhostDefaultTest.cs`

## Use These For

- entity spawning and lifecycle
- server-client or roundflow behavior
- system interactions that need a real runtime harness

## Pattern Reminder

Prefer an integration test only when the bug or feature truly depends on runtime orchestration. If a smaller shared/content test can cover the behavior, use that instead.
