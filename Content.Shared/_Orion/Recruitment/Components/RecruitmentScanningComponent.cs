using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Recruitment.Components;

[RegisterComponent]
public sealed partial class RecruitmentScanningComponent : Component
{
    /// <summary>
    ///     Implant prototype to inject.
    /// </summary>
    [DataField]
    public EntProtoId? Implant;

    /// <summary>
    ///     Faction to join after successful scanning.
    /// </summary>
    [DataField]
    public string? Faction;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> ScannedEntities = [];

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntityWhitelist? Whitelist;
}
