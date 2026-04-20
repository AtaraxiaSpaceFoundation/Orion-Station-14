using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent]
public sealed partial class BitrunningDomainRuntimeComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningExitMarkerComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningGoalMarkerComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningCacheMarkerComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningCacheCrateMarkerComponent : Component
{
    [DataField]
    public EntProtoId CratePrototype = "CrateBitrunSecure";
}

[RegisterComponent]
public sealed partial class BitrunningSpawnMarkerComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningObjectivePointComponent : Component
{
    [DataField]
    public int Points = 1;

    [DataField]
    public bool ConsumeOnUse = true;

    [DataField]
    public SoundSpecifier PickupSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");
}

[RegisterComponent]
public sealed partial class BitrunningObjectiveDeliveryPointComponent : Component
{
    [DataField]
    public int Points = 1;
}

[RegisterComponent]
public sealed partial class BitrunningObjectiveCargoComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningDeliveredObjectiveCargoComponent : Component;

[RegisterComponent]
public sealed partial class BitrunningDomainEnemyObjectiveComponent : Component
{
    [DataField]
    public int Points = 1;
}
