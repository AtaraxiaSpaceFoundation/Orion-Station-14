using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Orion.Economy.Rules.Components;

[RegisterComponent]
public sealed partial class PaydayRuleComponent : Component
{
    [DataField]
    public TimeSpan Interval = TimeSpan.FromMinutes(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPayday;
}
