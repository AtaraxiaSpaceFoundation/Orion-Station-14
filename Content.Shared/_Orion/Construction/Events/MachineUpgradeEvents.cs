using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Orion.Construction.Components;
using Content.Shared.Stacks;
using Robust.Shared.Utility;

namespace Content.Shared._Orion.Construction.Events;

public struct MachinePartState
{
    public MachinePartComponent Part;
    public StackComponent? Stack;

    public readonly int Quantity()
    {
        return Stack?.Count ?? 1;
    }
}

public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartState> Parts = new List<MachinePartState>();
    public Dictionary<string, float> PartRatings = new();
}

public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private readonly FormattedMessage _message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        _message = message;
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent)) + '\n',
            out _);
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier, float timeModifier)
    {
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage-extra",
            1 or float.NaN => "machine-upgrade-not-upgraded-extra",
            > 1 => "machine-upgrade-increased-by-percentage-extra",
        };

        FixedPoint2 percent = multiplier switch
        {
            < 1 => 100 * timeModifier * MathF.Abs(multiplier - 1),
            1 or float.NaN => 100 / timeModifier,
            > 1 => 100 / timeModifier * MathF.Abs(multiplier - 1),
        };

        var color = timeModifier switch
        {
            < 1 => "#6DFFA5",
            1 or float.NaN => "#FFFFFF",
            > 1 => "#FF7A7A",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent),
            ("color", color)) + '\n',
            out _);
    }
}
