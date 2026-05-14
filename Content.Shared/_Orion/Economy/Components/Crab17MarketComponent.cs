namespace Content.Shared._Orion.Economy.Components;

[RegisterComponent]
public sealed partial class Crab17MarketComponent : Component
{
    [DataField]
    public TimeSpan NextDrainTime = TimeSpan.MaxValue;

    [DataField]
    public TimeSpan DrainInterval = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan DeleteAt = TimeSpan.MaxValue;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromMinutes(8);

    [DataField]
    public int StoredCredits;

    [DataField]
    public EntityUid? ActivatorMind;

    [DataField]
    public string? ActivatorAccountId;

    [DataField]
    public bool IsReady;

    [DataField]
    public TimeSpan StartupNextStageAt;

    [DataField]
    public int StartupStage;
}
