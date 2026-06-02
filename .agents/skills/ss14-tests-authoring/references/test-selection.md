<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Test Selection

## Use `Content.Tests` When

- The change is mostly pure shared/content logic.
- Prototype loading, serialization, parsing, or utility behavior is the risk.
- You can verify behavior without a full server-client runtime.

## Use `Content.IntegrationTests` When

- The feature crosses prediction, networking, entity lifecycle, or server-client interaction.
- The risk depends on runtime behavior, spawning, systems, or round setup.

## Use Validation-Only When

- The change is limited to YAML, FTL, or RSI metadata and existing validators cover the risk.

## Commands

- `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0`
- `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`
- `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt`
- `python3 RobustToolbox/Schemas/validate_rsis.py Resources/`
