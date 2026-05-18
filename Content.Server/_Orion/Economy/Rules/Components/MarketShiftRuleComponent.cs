using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Orion.Economy.Rules.Components;

[RegisterComponent]
public sealed partial class MarketShiftRuleComponent : Component
{
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromMinutes(60);

    [DataField]
    public int MinIncreased = 1;

    [DataField]
    public int MaxIncreased = 4;

    [DataField]
    public int MinDecreased = 1;

    [DataField]
    public int MaxDecreased = 5;

    [DataField]
    public float IncreasedMultiplierMin = 1.1f;

    [DataField]
    public float IncreasedMultiplierMax = 1.8f;

    [DataField]
    public float DecreasedMultiplierMin = 0.2f;

    [DataField]
    public float DecreasedMultiplierMax = 0.9f;

    [DataField]
    public bool AnnouncementsEnabled = true;

    [DataField]
    public List<string>? AllowedMaterials;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextShiftTime;
}
