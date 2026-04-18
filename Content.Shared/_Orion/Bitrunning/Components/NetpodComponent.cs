using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetpodComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedServer;

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    [DataField, AutoNetworkedField]
    public EntityUid? Avatar;
}
