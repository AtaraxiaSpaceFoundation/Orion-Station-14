<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Testing And Validation

Choose the smallest meaningful verification for the files you touched.

## Required Validation By Change Type

- C# gameplay code: build the affected project or the full `DebugOpt` solution slice.
- Prototypes, maps, or FTL: run `Content.YAMLLinter` when practical.
- RSI metadata or sprite state changes: run `RobustToolbox/Schemas/validate_rsis.py`.
- Client code or UI: do an in-game or runtime client pass when possible.

## Standard Commands

- SDK: `global.json` pins .NET SDK `9.0.100` with `latestFeature` roll-forward.
- Submodules: run `git submodule update --init --recursive` before restore/build/test when `RobustToolbox/` is not initialized; CI also pulls engine updates before building.
- Restore: `dotnet restore`.
- Baseline build: `dotnet build --configuration DebugOpt --no-restore /m`.
- Content tests after a successful build: `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0`.
- Integration tests after a successful build: `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`.
- Standalone content test run: `dotnet test --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0`.
- Standalone integration test run: `dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`.
- YAML/resource edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt`.
- CI YAML linter path: build Release, then `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build`.
- RSI edits: `python3 RobustToolbox/Schemas/validate_rsis.py Resources/` after initializing the `RobustToolbox` submodule and installing `pillow` and `jsonschema`.
- Helper scripts exist under `Scripts/sh/`, but some are interactive and write logs; prefer the direct commands above for non-interactive agents.

## Reporting

- State exactly what you ran.
- For multiline checks or regex-heavy helper commands, avoid inline Markdown that can be parsed as math; use a fenced block or report a short wrapper command plus the purpose.
- State what you could not run.
- If runtime verification was not possible, call that out explicitly instead of implying full coverage.
