using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Orion.ResponseForce;

[Prototype("responseForceTeam")]
public sealed class ResponseForceTeamPrototype : IPrototype, IInheritingPrototype
{
    /// <summary>
    /// Name of the Response ForceTeam that will be shown at the round end manifest.
    /// </summary>
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ResponseForceTeamPrototype>))]
    public string[]? Parents { get; }

    /// <summary>
    /// Is that Response Force Team is abstract.
    /// </summary>
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Name of the Response Force Team that will be shown at the round end manifest.
    /// </summary>
    [DataField(required: true)]
    public LocId ResponseForceName;

    /// <summary>
    /// Shuttle path for the Response Force.
    /// </summary>
    [DataField(required: true)]
    public string ShuttlePath = default!;

    [DataField(required: true)]
    public EntProtoId SpawnMarker;

    /// <summary>
    /// Announcement text for the Response Force.
    /// </summary>
    [DataField]
    public LocId? AnnouncementText;

    /// <summary>
    /// Announcement title for the Response Force.
    /// </summary>
    [DataField]
    public LocId? AnnouncementTitle;

    /// <summary>
    /// Announcement sound for the Response Force.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Orion/Adminbuse/yesert.ogg");

    /// <summary>
    /// На какое количество игроков будет приходиться спавн ещё одной гост роли.
    /// По умолчанию: за каждого 10-го игрока прибавляется 1 гост роль
    /// </summary>
    [DataField]
    public int SpawnPerPlayers = 10;

    /// <summary>
    /// Max amount of ghost roles that can be spawned.
    /// </summary>
    [DataField]
    public int MaxRolesAmount = 8;

    [DataField]
    public string RaffleSettings = "default";

    /// <summary>
    /// Response Force that will be spawned no matter what.
    /// Uses EntitySpawnEntry and therefore has ability to change spawn prob.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> GuaranteedSpawn = new();

    /// <summary>
    /// Response Force that will be spawned using the spawnPerPlayers variable.
    /// Ghost roles will spawn by the order they arranged in list.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> ResponseForceSpawn= new();
}
