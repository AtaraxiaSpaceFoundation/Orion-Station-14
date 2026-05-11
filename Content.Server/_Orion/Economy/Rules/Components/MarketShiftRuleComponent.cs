using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Orion.Economy.Rules.Components;

[RegisterComponent]
public sealed partial class MarketShiftRuleComponent : Component
{
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromMinutes(6);

    [DataField]
    public int MinIncreased = 2;

    [DataField]
    public int MaxIncreased = 4;

    [DataField]
    public int MinDecreased = 1;

    [DataField]
    public int MaxDecreased = 3;

    [DataField]
    public float IncreasedMultiplierMin = 1.1f;

    [DataField]
    public float IncreasedMultiplierMax = 1.8f;

    [DataField]
    public float DecreasedMultiplierMin = 0.8f;

    [DataField]
    public float DecreasedMultiplierMax = 2f;

    [DataField]
    public bool AnnouncementsEnabled = true;

    [DataField]
    public List<string>? AllowedMaterials;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextShiftTime;
}
