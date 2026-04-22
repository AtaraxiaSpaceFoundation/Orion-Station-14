using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Content.Shared.EntityTable;
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
    public int BroadcastWirelessRange = 6767;

    [DataField]
    public SoundSpecifier DomainStartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryEasyLootTable = "BitrunningDeliveryEasyLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryMediumLootTable = "BitrunningDeliveryMediumLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryHardLootTable = "BitrunningDeliveryHardLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> DeliveryExtremeLootTable = "BitrunningDeliveryExtremeLoot";

    [DataField]
    public TimeSpan ExitParalyzeTime = TimeSpan.FromSeconds(3.5);

    [DataField]
    public TimeSpan ExitBlindnessTime = TimeSpan.FromSeconds(3.5);

    [DataField]
    public List<Vector2> CacheSpawnOffsets = new()
    {
        Vector2.Zero,
        new Vector2(1f, 0f),
        new Vector2(0f, 1f),
    };

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<BitrunningVirtualDomainPrototype>)), AutoNetworkedField]
    public string? CurrentDomain;

    public EntityUid? DomainMapUid;

    public EntityUid? DomainGridUid;

    public readonly HashSet<EntityUid> ActiveConnections = new();

    public readonly HashSet<EntityUid> Occupants = new();

    public EntityCoordinates? ExitCoordinates;

    public EntityCoordinates? CacheCoordinates;

    public EntityCoordinates? GoalCoordinates;

    public EntityCoordinates? SpawnCoordinates;

    public EntityUid? LinkedByteforge;

    public TimeSpan DomainStartTime;

    public int ObjectivePoints;

    public int ObjectiveGoal = 10;

    public bool ObjectiveCompleted;

    public BitrunningObjectiveType ObjectiveType = BitrunningObjectiveType.CollectEncryptedCaches;

    public int ThreatsSpawned;

    public bool AllowDiskModifications = true;

    public bool WasRandomizedRun;

    public readonly HashSet<EntityUid> GrantedItemDisks = new();
}
