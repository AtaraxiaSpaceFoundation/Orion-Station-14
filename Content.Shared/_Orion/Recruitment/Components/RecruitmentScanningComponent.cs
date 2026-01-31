using Content.Shared.Whitelist;
using Robust.Shared.Audio;
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

    [DataField("organization")]
    public LocId OrganizationName = "organization-unknown";

    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> ScannedEntities = [];

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntityWhitelist? Whitelist;
}
