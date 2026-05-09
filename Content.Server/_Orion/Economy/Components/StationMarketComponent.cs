using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Orion.Economy.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StationMarketComponent : Component
{
    [DataField]
    public Dictionary<string, float> MaterialMultipliers = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextMarketUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextReportUpdate;

    [DataField]
    public TimeSpan MarketUpdateDelay = TimeSpan.FromMinutes(6);

    [DataField]
    public TimeSpan ReportDelay = TimeSpan.FromMinutes(5);
}
