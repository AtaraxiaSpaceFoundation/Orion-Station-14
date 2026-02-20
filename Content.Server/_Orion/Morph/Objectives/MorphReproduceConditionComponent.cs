namespace Content.Server._Orion.Morph.Objectives;

[RegisterComponent, Access(typeof(MorphReproduceConditionSystem))]
public sealed partial class MorphReproduceConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
