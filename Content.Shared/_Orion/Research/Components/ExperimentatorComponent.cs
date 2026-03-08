using Content.Shared._Orion.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentatorComponent : Component
{
    [DataField]
    public List<ProtoId<ResearchExperimentatorOperationPrototype>> Operations = new();
}
