namespace Content.Server._Orion.Morph.Objectives;

[RegisterComponent, Access(typeof(MorphDevourLivingConditionSystem))]
public sealed partial class MorphDevourLivingConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
