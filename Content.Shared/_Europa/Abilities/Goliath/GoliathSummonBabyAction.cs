using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Goliath;

public sealed partial class GoliathSummonBabyAction : EntityWorldTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "MobGoliathBaby";

    [DataField]
    public int SummonCount = 2;

    [DataField]
    public List<Direction> OffsetDirections = new();

    [DataField]
    public float Offset = 4f;
}
