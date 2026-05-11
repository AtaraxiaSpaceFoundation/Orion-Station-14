namespace Content.Server._Orion.Economy.Rules.Components;

[RegisterComponent]
public sealed partial class PaydayRuleComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan NextPayday;
}
