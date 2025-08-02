using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Goliath;

public sealed partial class GoliathSummonBabyAction : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId EntityId = "MobGoliathBaby";

    [DataField]
    public int SummonCount = 2;
}
