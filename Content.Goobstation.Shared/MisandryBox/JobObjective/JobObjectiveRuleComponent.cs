using Content.Shared.Mind;

namespace Content.Goobstation.Shared.MisandryBox.JobObjective;

[RegisterComponent]
public sealed partial class JobObjectiveRuleComponent : Component
{
    [DataField]
    public List<(EntityUid Mind, MindComponent MindComp)> TrackedMinds = [];
}
