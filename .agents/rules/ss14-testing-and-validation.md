<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Testing And Validation

Choose the smallest meaningful verification for the files you touched.

## Required Validation By Change Type

- C# gameplay code: build the affected project or repo slice.
- Prototypes or FTL: run `Content.YAMLLinter`.
- RSI metadata or sprite state changes: run `Schemas/validate_rsis.py`.
- Client code or UI: do an in-game or runtime client pass when possible.

## Standard Commands

- Baseline build: `dotnet restore` then `dotnet build --configuration DebugOpt --no-restore /m`
- Unit/content tests: `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj`
- Integration tests: `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj`
- YAML/resource edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- RSI edits: `py -3 Schemas/validate_rsis.py Resources`

## Reporting

- State exactly what you ran.
- State what you could not run.
- If runtime verification was not possible, call that out explicitly instead of implying full coverage.
