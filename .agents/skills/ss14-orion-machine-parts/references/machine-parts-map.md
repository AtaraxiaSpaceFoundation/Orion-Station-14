# Orion Machine Parts Map

## Inspect These Paths First

- Shared code:
  - `Content.Shared/_Orion/Construction/Components/MachinePartComponent.cs`
  - `Content.Shared/_Orion/Construction/Steps/MachinePartConstructionGraphStep.cs`
  - `Content.Shared/_Orion/Construction/Events/MachineUpgradeEvents.cs`
- Server code:
  - `Content.Server/_Orion/Construction/Systems/ConstructionSystem.Machine.Upgrades.cs`
  - `Content.Server/_Orion/Construction/Systems/PartExchangerSystem.cs`
- Related construction graph, recipe, machine frame, and lathe data: `Resources/Prototypes/_Orion/Recipes/` and `Resources/Prototypes/_Orion/Entities/Structures/Machines/`.
- Targeted integration coverage: `Content.IntegrationTests/Tests/_Orion/Construction/Interaction/MachinePartInteractionTests.cs`.

## Preserve The Existing Upgrade Flow

- Treat machine parts as gameplay components, not raw materials.
- Pass rating/quality through the existing component, construction graph step, upgrade event, or `PartExchangerSystem` path.
- Do not confuse Orion `MachinePartComponent` with upstream `MultipartMachinePartComponent`.
- Do not create a parallel upgrade system while the Orion flow already owns the behavior.

## Validate

- Build after C# changes: `dotnet build --configuration DebugOpt --no-restore /m` after restore when needed.
- Prefer targeted integration tests: `dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj --filter MachinePartInteractionTests -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed`.
- Run `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt` after prototype, graph, or recipe changes.
