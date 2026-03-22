using Content.Server._Orion.Objectives.Systems;

namespace Content.Server._Orion.Objectives.Components;

[RegisterComponent, Access(typeof(ResearchDiskConditionSystem))]
public sealed partial class ResearchDiskConditionComponent : Component
{
    [DataField]
    public int MinTechnologyCount = 18;

    [DataField]
    public int MaxTechnologyCount = 30;

    [DataField]
    public int RequiredTechnologyCount;

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;
}
