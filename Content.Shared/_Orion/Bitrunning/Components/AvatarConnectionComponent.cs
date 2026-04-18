using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AvatarConnectionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalBody;

    [DataField, AutoNetworkedField]
    public EntityUid? Server;

    [DataField, AutoNetworkedField]
    public EntityUid? Netpod;

    [DataField, AutoNetworkedField]
    public bool NoHit = true;

    [DataField, AutoNetworkedField]
    public bool DeleteOnDisconnect;

    [DataField]
    public EntProtoId DisconnectActionPrototype = "ActionBitrunningDisconnectAvatar";

    [DataField]
    public EntityUid? DisconnectActionEntity;
}
