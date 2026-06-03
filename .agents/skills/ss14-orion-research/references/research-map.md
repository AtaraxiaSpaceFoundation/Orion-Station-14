# Orion Research Map

## Inspect These Paths First

- Server code: `Content.Server/_Orion/Research/`.
- Research data: `Resources/Prototypes/_Orion/Research/`, especially `Nodes/`, `Experiments/`, and `disciplines.yml`.
- Related unlock/craft/lathe data: `Resources/Prototypes/_Orion/Recipes/`.
- Locale: `Resources/Locale/en-US/_Orion/research/` and `Resources/Locale/ru-RU/_Orion/research/`.

## Preserve Progression

- Do not add a recipe only to a lathe/prototype file when it should be unlocked by research.
- Do not add a research node without checking prerequisites, cost, discipline/branch placement, and unlocks.
- Do not change costs or availability without checking nearby mining, engineering, science, and related branch progression.
- Do not treat upstream R&D as the source of truth when Orion extensions in `Content.Server/_Orion/Research/` own the behavior.

## High-Risk Areas

- Destructive analyzer and destructive experiment systems.
- Experiment rewards, discounts, research points, and reward import/discovery pipelines.
- Node unlock lists and recipe availability.
- Localized names/descriptions for research UI and prototypes.

## Validate

- Run `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt` after prototype or FTL changes.
- Build after C# changes: `dotnet build --configuration DebugOpt --no-restore /m` after restore when needed.
- Run targeted tests or content tests when unlock/prototype flows are touched.
