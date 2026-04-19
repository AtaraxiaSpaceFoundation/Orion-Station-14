using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QuantumServerComponent : Component
{
    [DataField, AutoNetworkedField]
    public BitrunningServerState State = BitrunningServerState.Ready;

    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public int ScannerTier = 1;

    [DataField, AutoNetworkedField]
    public float CooldownEfficiency = 1f;

    [DataField, AutoNetworkedField]
    public float QualityBonus;

    [DataField, AutoNetworkedField]
    public EntProtoId AvatarPrototype = "MobHuman";

    [DataField, AutoNetworkedField]
    public EntProtoId RewardCachePrototype = "CrateBitrunSecure";

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEndTime;

    [DataField, AutoNetworkedField]
    public bool BroadcastEnabled;

    [DataField]
    public float BroadcastWirelessRange = 6767f;

    [DataField]
    public SoundSpecifier DomainStartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public TimeSpan ExitParalyzeTime = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan ExitBlindnessTime = TimeSpan.FromSeconds(2.5);

    [DataField]
    public List<Vector2> CacheSpawnOffsets = new()
    {
        Vector2.Zero,
        new Vector2(1f, 0f),
        new Vector2(0f, 1f),
    };

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<BitrunningVirtualDomainPrototype>)), AutoNetworkedField]
    public string? CurrentDomain;

    [ViewVariables]
    public EntityUid? DomainMapUid;

    [ViewVariables]
    public EntityUid? DomainGridUid;

    [ViewVariables]
    public readonly HashSet<EntityUid> ActiveConnections = new();

    [ViewVariables]
    public readonly HashSet<EntityUid> Occupants = new();

    [ViewVariables]
    public EntityCoordinates? ExitCoordinates;

    [ViewVariables]
    public EntityCoordinates? CacheCoordinates;

    [ViewVariables]
    public EntityCoordinates? GoalCoordinates;

    [ViewVariables]
    public TimeSpan DomainStartTime;

    [ViewVariables]
    public int ObjectivePoints;

    [ViewVariables]
    public int ObjectiveGoal = 10;

    [ViewVariables]
    public int ThreatsSpawned;
}
