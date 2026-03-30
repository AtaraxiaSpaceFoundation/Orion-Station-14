using System.Globalization;
using System.Linq;
using Content.Shared._Orion.Research.Components;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Research.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Research.Systems;

public static class ResearchExperimentUiData
{
    public static ResearchMachineExperimentUiData Create(ResearchExperimentPrototype prototype, ResearchExperimentProgress progress, IPrototypeManager prototypeManager)
    {
        var target = progress.Target > 0 ? progress.Target : Math.Max(1, prototype.Objective.Target);
        var objective = Loc.GetString($"research-experiment-objective-{prototype.Objective.Kind.ToString().ToLowerInvariant()}");
        var goal = BuildGoalText(prototype.Objective, prototypeManager);

        return new ResearchMachineExperimentUiData(
            prototype.ID,
            Loc.GetString(prototype.Name),
            Loc.GetString(prototype.Description),
            progress.Progress,
            target,
            objective,
            goal);
    }

    private static string BuildGoalText(ExperimentObjective objective, IPrototypeManager prototypeManager)
    {
        var action = Loc.GetString($"research-experiment-goal-action-{objective.Kind.ToString().ToLowerInvariant()}");
        var details = BuildGoalDetails(objective, prototypeManager);

        return string.IsNullOrWhiteSpace(details)
            ? action
            : Loc.GetString("research-experiment-goal-with-details", ("action", action), ("details", details));
    }

    private static string BuildGoalDetails(ExperimentObjective objective, IPrototypeManager prototypeManager)
    {
        if (objective is not ScanEntityExperimentObjective scan)
            return string.Empty;

        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(scan.RequiredReagent))
        {
            var reagentName = scan.RequiredReagent;
            if (prototypeManager.TryIndex<ReagentPrototype>(scan.RequiredReagent, out var reagentPrototype))
                reagentName = reagentPrototype.LocalizedName;

            details.Add(Loc.GetString("research-experiment-goal-detail-reagent", ("name", reagentName)));

            if (scan.MinReagentPurity is { } minReagentPurity)
            {
                details.Add(Loc.GetString("research-experiment-goal-detail-purity",
                    ("name", Loc.GetString("research-experiment-goal-purity-reagent")),
                    ("value", FormatPercent(minReagentPurity))));
            }
        }

        if (!string.IsNullOrWhiteSpace(scan.RequiredGas))
        {
            details.Add(Loc.GetString("research-experiment-goal-detail-gas", ("name", GetGasName(scan.RequiredGas, prototypeManager))));

            if (scan.MinGasPurity is { } minGasPurity)
            {
                details.Add(Loc.GetString("research-experiment-goal-detail-purity",
                    ("name", Loc.GetString("research-experiment-goal-purity-gas")),
                    ("value", FormatPercent(minGasPurity))));
            }
        }

        if (scan.RequiredEntityPrototypes.Count > 0)
        {
            var names = scan.RequiredEntityPrototypes
                .Select(id => GetEntityName(id, prototypeManager));
            details.Add(Loc.GetString("research-experiment-goal-detail-entities", ("names", string.Join(", ", names))));
        }

        if (scan.RequiredConditions.Count > 0)
        {
            var conditionNames = scan.RequiredConditions
                .Select(GetConditionName);
            details.Add(Loc.GetString("research-experiment-goal-detail-conditions", ("names", string.Join(", ", conditionNames))));
        }

        if (scan.MinExplosiveIntensity is { } minIntensity)
            details.Add(Loc.GetString("research-experiment-goal-detail-intensity", ("value", minIntensity.ToString("0.##", CultureInfo.InvariantCulture))));

        return string.Join(", ", details);
    }

    private static string FormatPercent(float value)
    {
        return (value * 100f).ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static string GetConditionName(ExperimentEntityCondition condition)
    {
        return Loc.GetString($"research-experiment-goal-condition-{condition.ToString().ToLowerInvariant()}");
    }

    private static string GetEntityName(string prototypeId, IPrototypeManager prototypeManager)
    {
        return !prototypeManager.TryIndex<EntityPrototype>(prototypeId, out var prototype)
            ? prototypeId
            : prototype.Name;
    }

    private static string GetGasName(string gasId, IPrototypeManager prototypeManager)
    {
        if (!Enum.TryParse<Gas>(gasId, true, out var gas) || !prototypeManager.TryIndex<GasPrototype>(((int) gas).ToString(), out var gasPrototype))
            return gasId;

        return Loc.GetString(gasPrototype.Name);
    }
}
