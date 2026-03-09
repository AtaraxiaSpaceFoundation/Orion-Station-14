using Content.Shared._Orion.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentatorComponent : Component
{
    [DataField]
    public List<ProtoId<ResearchExperimentatorOperationPrototype>> Operations = new();

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
