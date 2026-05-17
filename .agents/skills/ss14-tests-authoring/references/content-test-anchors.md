<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Content Test Anchors

## Good Starting Points

- `Content.Tests/Shared/Chemistry/ReagentPrototype_Tests.cs`
- `Content.Tests/Shared/LocalizedDatasetPrototypeTest.cs`
- `Content.Tests/Shared/Gamestates/ComponentStateNullTest.cs`

## Use These For

- prototype parsing
- localized data loading
- shared serialization and replicated-state guards

## Pattern Reminder

Keep content tests narrow. Test the parsing or logic edge the change actually introduces instead of rebuilding a full runtime in a unit-style test.
