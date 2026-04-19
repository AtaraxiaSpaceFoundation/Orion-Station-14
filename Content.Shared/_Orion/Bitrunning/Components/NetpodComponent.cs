using Content.Shared.Implants;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    [DataField, AutoNetworkedField]
    public ProtoId<ChameleonOutfitPrototype>? PreferredOutfit = "ShaftMinerChameleonOutfit";
}
