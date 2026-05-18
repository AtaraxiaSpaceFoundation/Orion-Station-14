using Content.Shared._Orion.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Construction.Components;

[RegisterComponent]
public sealed partial class MachinePartComponent : Component
{
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> Part;

    [DataField]
    public int Tier = 1;

    // TODO: Hook into machine power scaling when machine consumers expose part-based energy coefficients.
    [DataField]
    public int EnergyRating = 1;
}
