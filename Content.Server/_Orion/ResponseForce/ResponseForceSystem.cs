using System.Linq;
using System.Numerics;
using System.Threading;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.RandomMetadata;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared._Orion.ResponseForce;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

using static Content.Server._Orion.ResponseForce.ResponseForceState;

namespace Content.Server._Orion.ResponseForce;

public sealed class ResponseForceSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private ISawmill _log = default!;
    private readonly ReaderWriterLockSlim _callLock = new();
    private TimeSpan CallDelay => TimeSpan.FromMinutes(_configurationManager.GetCVar(CCVars.ResponseForceDelay));

    public override void Initialize()
    {
        base.Initialize();

        _log = Logger.GetSawmill("responseforce.call");

        SubscribeLocalEvent<ResponseForceComponent, MapInitEvent>(OnMapInit, after: [typeof(RandomMetadataSystem)]);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<ResponseForceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResponseForceComponent, ComponentShutdown>(OnShutdown);
//        SubscribeLocalEvent<BlobCriticalStageEvent>(OnBlobCritical);
    }

    private void SetupShipyard()
    {
        if (ShipyardMap != null && _mapSystem.MapExists(ShipyardMap))
            return;

        var mapEntity = _mapSystem.CreateMap(out var mapId);
        ShipyardMap = mapId;

        _mapSystem.SetPaused(mapEntity, false);
        ShuttleIndex = 0f;
    }

    private void CleanupShipyard()
    {
        if (ShipyardMap == null || !_mapSystem.MapExists(ShipyardMap.Value))
        {
            ShipyardMap = null;
            ShuttleIndex = 0f;
            return;
        }

        _mapSystem.DeleteMap(ShipyardMap.Value);
        ShipyardMap = null;
        ShuttleIndex = 0f;
    }

/* TODO: Do this.
    private void OnBlobCritical(BlobCriticalStageEvent ev)
    {
        var responseEvent = new BlobCriticalStageEvent
        {
            Station = ev.Station,
        };

        RaiseLocalEvent(ev.Station, responseEvent);
    }
*/

    private void OnShutdown(EntityUid uid, ResponseForceComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.FTLKey);
    }

    private void OnStartup(EntityUid uid, ResponseForceComponent component, ComponentStartup args)
    {
        if (component.ActionFTLName != null)
            _actions.AddAction(uid, ref component.FTLKey, component.ActionFTLName);
    }

    private void OnMapInit(EntityUid uid, ResponseForceComponent component, MapInitEvent args)
    {
        foreach (var entry in component.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            EntityManager.AddComponent(uid, comp);
        }
    }

    public TimeSpan DelayTime
    {
        get
        {
            var ct = _gameTicker.RoundDuration();
            var lastUsedTime = LastCallTime + CallDelay;
            return ct > lastUsedTime ? TimeSpan.Zero : lastUsedTime - ct;
        }
    }

    /// <summary>
    /// Calls ResponseForce, creating new map with a shuttle, and spawning on it ResponseForces.
    /// </summary>
    /// <param name="protoId"> ResponseForcePrototype ID.</param>
    /// <param name="source"> Source of the call.</param>
    /// <param name="forceCountExtra"> How many extra ResponseForce will be forced to spawn.</param>
    /// <param name="forceCall"> If true, cooldown will be ignored.</param>
    /// <returns>Returns true if call was successful.</returns>
    public bool CallResponseForce(ProtoId<ResponseForceTeamPrototype> protoId, string source = "Unknown", int? forceCountExtra = null, bool forceCall = false)
    {
        if (!_callLock.TryEnterWriteLock(-1))
        {
            _log.Warning("ResponseForce is busy!");
            return false;
        }
        try
        {
            if (!_prototypes.TryIndex(protoId, out var prototype))
            {
                _log.Error("Wrong ResponseForceTeamPrototype ID!");
                return false;
            }

            if (_gameTicker.RunLevel != GameRunLevel.InRound)
            {
                _log.Warning("Can't call ResponseForce while not in the round.");
                return false;
            }

            var currentTime = _gameTicker.RoundDuration();

#if !DEBUG
            if (LastCallTime + CallDelay > currentTime && !forceCall)
            {
                _log.Info("Tried to call ResponseForce when it's on cooldown.");
                return false;
            }
#endif

            LastCallTime = currentTime;

            var shuttle = SpawnShuttle(prototype.ShuttlePath);
            if (shuttle == null)
            {
                _log.Error("Failed to load ResponseForce shuttle!");
                return false;
            }

            SpawnGhostRole(prototype, shuttle.Value, forceCountExtra);
            DispatchAnnouncement(prototype);

            _log.Info($"Successfully called {prototype.ID} ResponseForceTeam. Source: {source}");

            CalledEvents.Add(new ResponseForceHistory { Event = prototype.ResponseForceName, RoundTime = currentTime, WhoCalled = source });

            return true;
        }
        finally
        {
            _callLock.ExitWriteLock();
        }
    }

    private EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates, ResponseForceTeamPrototype responseForce)
    {
        if (protoName == null)
            return EntityUid.Invalid;

        var uid = EntityManager.SpawnEntity(protoName, coordinates);

        EnsureComp<ResponseForceComponent>(uid);

        // If entity is a GhostRoleMobSpawner, it's child prototype is valid AND
        // has GhostRoleComponent, clone this component and add it to the parent.
        // This is necessary for SpawnMarkers that don't have GhostRoleComp in prototype.
        if (TryComp<GhostRoleMobSpawnerComponent>(uid, out var mobSpawnerComponent) &&
            mobSpawnerComponent.Prototype != null &&
            _prototypes.TryIndex<EntityPrototype>(mobSpawnerComponent.Prototype, out var spawnObj) &&
            spawnObj.TryGetComponent<GhostRoleComponent>(out var tplGhostRoleComponent, _componentFactory))
        {
            var comp = _serialization.CreateCopy(tplGhostRoleComponent, notNullableOverride: true);

            comp.RaffleConfig ??= new();
            comp.RaffleConfig.Settings = new ProtoId<GhostRoleRaffleSettingsPrototype>(responseForce.RaffleSettings);

            EntityManager.AddComponent(uid, comp);
        }

        if (TryComp<GhostRoleComponent>(uid, out var ghostRole) && ghostRole.RaffleConfig == null)
        {
            ghostRole.RaffleConfig = new()
            {
                Settings = new ProtoId<GhostRoleRaffleSettingsPrototype>(responseForce.RaffleSettings),
            };
        }

        return uid;
    }

    public int GetCount(ResponseForceTeamPrototype proto, int? plrCount = null)
    {
        return ((plrCount ?? _playerManager.PlayerCount) + proto.SpawnPerPlayers) / proto.SpawnPerPlayers;
    }

    private void SpawnGhostRole(ResponseForceTeamPrototype proto, EntityUid shuttle, int? forceCountExtra = null)
    {
        // Find all spawn points on the shuttle, add them in list
        var spawns = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out _, out var meta, out var xform))
        {
            if (meta.EntityPrototype!.ID != proto.SpawnMarker)
                continue;

            if (xform.GridUid != shuttle)
                continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            _log.Warning("Shuttle has no valid spawns for ResponseForce! Making something up...");
            spawns.Add(Transform(shuttle).Coordinates);
        }

        SpawnGuaranteed(proto, spawns);
        SpawnResponseForce(proto, spawns, forceCountExtra);
    }

    private void SpawnGuaranteed(ResponseForceTeamPrototype proto, List<EntityCoordinates> spawns)
    {
        // If responseForceSpawn is empty, we can't continue
        if (proto.GuaranteedSpawn.Count == 0)
            return;

        // Spawn Guaranteed ResponseForce from the prototype.
        var toSpawnGuaranteed = EntitySpawnCollection.GetSpawns(proto.GuaranteedSpawn, _random);

        foreach (var mob in toSpawnGuaranteed)
        {
            var spawned = SpawnEntity(mob, _random.Pick(spawns), proto);
            _log.Info($"Successfully spawned {ToPrettyString(spawned)} Static ResponseForce.");
        }
    }

    private void SpawnResponseForce(ResponseForceTeamPrototype proto, List<EntityCoordinates> spawns, int? forceCountExtra)
    {
        // If responseForceSpawn is empty, we can't continue
        if (proto.ResponseForceSpawn.Count == 0)
            return;

        // Count how many other forces there should be.
        var countExtra = GetCount(proto);
        // If bigger than MaxAmount, set to MaxAmount and extract already spawned roles
        countExtra = Math.Min(countExtra, proto.MaxRolesAmount);

        // If CountExtra was forced to some number, check if this number is in range and extract already spawned roles.
        if (forceCountExtra is >= 0)
            countExtra = Math.Min(forceCountExtra.Value, proto.MaxRolesAmount);

        // Either zero or bigger than zero, no negatives
        countExtra = Math.Max(0, countExtra);

        // Spawn Guaranteed Response Force from the prototype.
        // If all mobs from the list are spawned and we still have free slots, restart the cycle again.
        while (countExtra > 0)
        {
            var toSpawnForces = EntitySpawnCollection.GetSpawns(proto.ResponseForceSpawn, _random);
            if (toSpawnForces.Count == 0)
                break;

            foreach (var mob in toSpawnForces.Where(_ => countExtra > 0))
            {
                countExtra--;
                var spawned = SpawnEntity(mob, _random.Pick(spawns), proto);
                _log.Info($"Successfully spawned {ToPrettyString(spawned)} Opt-in ResponseForce.");
            }
        }
    }

    /// <summary>
    /// Spawns shuttle for Response Force on a new map.
    /// </summary>
    /// <param name="shuttlePath"></param>
    /// <returns>Grid's entity of the shuttle.</returns>
    private EntityUid? SpawnShuttle(string shuttlePath)
    {
        SetupShipyard();

        if (!_map.TryLoadGrid(ShipyardMap!.Value, new ResPath(shuttlePath), out var grid, offset: new Vector2(500f + ShuttleIndex, 1f)))
            return null;

        ShuttleIndex += grid.Value.Comp.LocalAABB.Width + 1;

        return grid;
    }

    private void DispatchAnnouncement(ResponseForceTeamPrototype proto)
    {
        var stations = _stationSystem.GetStations();

        if (stations.Count == 0)
            return;

        // If we don't have title or text for the announcement, we can't make the announcement.
        if (proto.AnnouncementText == null || proto.AnnouncementTitle == null)
            return;

        foreach (var station in stations)
        {
            _chatSystem.DispatchStationAnnouncement(station,
                Loc.GetString(proto.AnnouncementText),
                Loc.GetString(proto.AnnouncementTitle),
                playDefaultSound: true,
                announcementSound: proto.Sound);
        }
    }

    private void OnRoundEnd(RoundEndTextAppendEvent ev)
    {
        foreach (var calledEvent in CalledEvents)
        {
            ev.AddLine(Loc.GetString("spec-forces-system-round-end",
                ("responseforce", Loc.GetString(calledEvent.Event)),
                ("time", calledEvent.RoundTime.ToString(@"hh\:mm\:ss")),
                ("who", calledEvent.WhoCalled)));
        }
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        CalledEvents.Clear();
        LastCallTime = TimeSpan.Zero;

        if (_callLock.IsWriteLockHeld)
            _callLock.ExitWriteLock();

        CleanupShipyard();
    }
}
