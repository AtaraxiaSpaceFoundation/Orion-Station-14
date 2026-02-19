namespace Content.Server._Orion.CorticalBorer.Objectives;

[RegisterComponent, Access(typeof(CorticalBorerWillingHostsConditionSystem))]
public sealed partial class CorticalBorerWillingHostsConditionComponent : Component
{
    [DataField(required: true)]
    public int Target;
}
