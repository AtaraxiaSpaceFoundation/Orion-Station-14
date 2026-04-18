using Robust.Shared.GameStates;

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
}
