using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

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

    [DataField]
    public bool ShutdownHandled;

    [DataField]
    public TimeSpan ProtectionTtl = TimeSpan.FromMinutes(4);

    [DataField]
    public Dictionary<string, TimeSpan> ProtectedUntil = new();

    [DataField]
    public TimeSpan BragInterval = TimeSpan.FromSeconds(11);

    [DataField]
    public TimeSpan NextBragTime = TimeSpan.MaxValue;

    [DataField]
    public int CreditsSinceLastBrag;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> BragPhraseDataset = "Crab17BragPhrases";
}
