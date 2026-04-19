using Content.Shared.Implants;
using Content.Shared.Roles;
using Robust.Shared.Audio;
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
    public ProtoId<StartingGearPrototype>? PreferredLoadout = "ShaftMinerGear";

    [DataField]
    public List<ProtoId<StartingGearPrototype>> AllowedLoadout = new();

    [DataField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Machines/airlock_open.ogg");

    [DataField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/Machines/airlock_close.ogg");
}
