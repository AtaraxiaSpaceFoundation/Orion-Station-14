using System.Linq;
using System.Numerics;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Server.Actions;
using Content.Server.Clothing.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Stunnable;
using Content.Server.SurveillanceCamera;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Prototypes;
using Content.Shared._Orion.Bitrunning.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Parallax;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class QuantumServerSystem : EntitySystem
{
    [Dependency] private readonly BitrunningDomainSystem _domains = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly NetpodSystem _netpod = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly OutfitSystem _outfit = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly BitrunningPointsSystem _bitrunningPoints = default!;
    [Dependency] private readonly ByteforgeSystem _byteforge = default!;
    [Dependency] private readonly BitrunningDiskSystem _bitrunningDisk = default!;

    private static readonly EntProtoId ExitBlindnessStatusEffect = "StatusEffectBitrunningExitBlindness";
    private const string ServerSourcePort = "BitrunningServerSource";

    public override void Initialize()
    {
        SubscribeLocalEvent<QuantumServerComponent, ComponentInit>(OnServerInit);
        SubscribeLocalEvent<QuantumServerComponent, MapInitEvent>(OnServerMapInit);
        SubscribeLocalEvent<QuantumServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<QuantumServerComponent, InteractUsingEvent>(OnServerInteractUsing);
        SubscribeLocalEvent<QuantumServerComponent, EntityTerminatingEvent>(OnServerTerminating);
        SubscribeLocalEvent<QuantumServerComponent, PowerChangedEvent>(OnServerPowerChanged);
        SubscribeLocalEvent<AvatarConnectionComponent, DamageChangedEvent>(OnAvatarDamaged);
        SubscribeLocalEvent<AvatarConnectionComponent, MobStateChangedEvent>(OnAvatarStateChanged);
        SubscribeLocalEvent<AvatarConnectionComponent, BitrunningDisconnectAvatarActionEvent>(OnAvatarDisconnectAction);
        SubscribeLocalEvent<AvatarConnectionComponent, SuicideGhostEvent>(OnAvatarSuicideGhost);
        SubscribeLocalEvent<AvatarConnectionComponent, SuicideEvent>(OnAvatarSuicide);
    }

    private void OnServerInit(Entity<QuantumServerComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ServerSourcePort);
        UpdateServerVisuals(ent);
    }

    private void OnServerMapInit(Entity<QuantumServerComponent> ent, ref MapInitEvent args)
    {
        _byteforge.RefreshLinkedByteforge(ent);
        UpdateServerVisuals(ent);
    }

    private void OnServerShutdown(Entity<QuantumServerComponent> ent, ref ComponentShutdown args)
    {
        StopDomain(ent, true);
    }

    private void OnServerTerminating(Entity<QuantumServerComponent> ent, ref EntityTerminatingEvent args)
    {
        StopDomain(ent, true);
    }

    private void OnServerInteractUsing(Entity<QuantumServerComponent> ent, ref InteractUsingEvent args)
    {
        if (ent.Comp.State != BitrunningServerState.Running)
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("bitrunning-server-active"), ent, args.User);
    }

    private void OnServerPowerChanged(Entity<QuantumServerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateServerVisuals(ent);

        if (args.Powered || ent.Comp.State != BitrunningServerState.Running)
            return;

        foreach (var connection in ent.Comp.ActiveConnections.ToArray())
        {
            DisconnectAvatar(connection, true);
        }
    }

    public bool TryColdBoot(EntityUid serverUid, string domainId, bool randomized = false)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return false;

        if (server.State != BitrunningServerState.Ready)
            return false;

        if (server.CurrentDomain != null || server.DomainMapUid != null)
            return false;

        if (!_domains.TryGetDomain(domainId, out var domain) || domain == null)
            return false;

        if (domain.Difficulty == BitrunningDifficulty.Extreme && !HasComp<EmaggedComponent>(serverUid))
            return false;

        if (server.Points < domain.Cost)
            return false;

        var mapEntity = _map.CreateMap(out var mapId, runMapInit: true);
        EnsureComp<BitrunningDomainRuntimeComponent>(mapEntity);
        _metaData.SetEntityName(mapEntity, Loc.GetString(domain.Name));

        var parallax = EnsureComp<ParallaxComponent>(mapEntity);
        parallax.Parallax = HasComp<EmaggedComponent>(serverUid)
            ? "CyberRed"
            : "Cyber";
        Dirty(mapEntity, parallax);

        if (!_mapLoader.TryLoadGrid(mapId, domain.MapPath, out var grid, offset: Vector2.Zero))
        {
            QueueDel(mapEntity);
            return false;
        }

        server.DomainMapUid = mapEntity;
        server.DomainGridUid = grid.Value;
        server.CurrentDomain = domainId;
        server.State = BitrunningServerState.Running;
        server.DomainStartTime = _timing.CurTime;
        server.ObjectivePoints = 0;
        server.ObjectiveGoal = Math.Max(domain.ObjectiveTarget, 0);
        server.ObjectiveType = domain.ObjectiveType;
        server.ObjectiveCompleted = false;
        server.Points -= domain.Cost;
        server.ThreatsSpawned = 0;
        server.CooldownEndTime = TimeSpan.Zero;
        server.AllowDiskModifications = domain.AllowDiskModifications;
        server.WasRandomizedRun = randomized;
        server.GrantedItemDisks.Clear();

        ResolveDomainMarkers((serverUid, server));
        _audio.PlayPvs(server.DomainStartSound, serverUid);
        UpdateServerVisuals((serverUid, server));
        Dirty(serverUid, server);
        return true;
    }

    private void UpdateServerVisuals(Entity<QuantumServerComponent> serverEnt)
    {
        var visualState = !_power.IsPowered(serverEnt.Owner)
            ? QuantumServerVisualState.Unpowered
            : serverEnt.Comp.State == BitrunningServerState.Running
                ? QuantumServerVisualState.Running
                : QuantumServerVisualState.Cooling;

        _appearance.SetData(serverEnt, BitrunningVisuals.QuantumServerState, visualState);
    }

    private void ResolveDomainMarkers(Entity<QuantumServerComponent> serverEnt)
    {
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;
        serverEnt.Comp.SpawnCoordinates = null;

        if (serverEnt.Comp.DomainMapUid is not { } mapUid)
            return;

        var exits = EntityQueryEnumerator<BitrunningExitMarkerComponent, TransformComponent>();
        while (exits.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.ExitCoordinates ??= Transform(uid).Coordinates;
        }

        var goals = EntityQueryEnumerator<BitrunningGoalMarkerComponent, TransformComponent>();
        while (goals.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.GoalCoordinates ??= Transform(uid).Coordinates;
        }

        var hasObjective = HasActiveObjective(serverEnt.Comp);
        var cacheCoordinates = new List<EntityCoordinates>();
        if (hasObjective && serverEnt.Comp.ObjectiveType == BitrunningObjectiveType.CollectEncryptedCaches)
        {
            var caches = EntityQueryEnumerator<BitrunningObjectiveEncryptedCacheSpawnMarkerComponent, TransformComponent>();
            while (caches.MoveNext(out var uid, out _, out var xform))
            {
                if (xform.MapUid != mapUid)
                    continue;

                var coordinates = Transform(uid).Coordinates;
                serverEnt.Comp.CacheCoordinates ??= coordinates;
                cacheCoordinates.Add(coordinates);
            }
        }

        var spawnMarkers = EntityQueryEnumerator<BitrunningAvatarSpawnMarkerComponent, TransformComponent>();
        while (spawnMarkers.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            serverEnt.Comp.SpawnCoordinates ??= Transform(uid).Coordinates;
        }

        if (serverEnt.Comp.ExitCoordinates == null && serverEnt.Comp.DomainGridUid is { } gridUid)
        {
            serverEnt.Comp.ExitCoordinates = TryComp<MapGridComponent>(gridUid, out var gridComp)
                ? new EntityCoordinates(gridUid, gridComp.LocalAABB.Center)
                : new EntityCoordinates(gridUid, Vector2.Zero);
        }

        serverEnt.Comp.GoalCoordinates ??= serverEnt.Comp.ExitCoordinates;
        serverEnt.Comp.CacheCoordinates ??= serverEnt.Comp.GoalCoordinates;
        serverEnt.Comp.SpawnCoordinates ??= serverEnt.Comp.ExitCoordinates;

        if (serverEnt.Comp.GoalCoordinates is not { } goal)
            return;

        if (!hasObjective)
            return;

        if (cacheCoordinates.Count > 0)
        {
            foreach (var cacheCoordinatesValue in cacheCoordinates)
            {
                if (cacheCoordinatesValue.IsValid(EntityManager))
                    Spawn("BitrunningEncryptedCacheObjectiveSpawner", cacheCoordinatesValue);
            }
            return;
        }

        if (serverEnt.Comp.ObjectiveType == BitrunningObjectiveType.DeliveryCacheCrate)
        {
            SpawnDeliveryCacheCrates(serverEnt.Comp, goal);
            return;
        }

        if (serverEnt.Comp.ObjectiveType != BitrunningObjectiveType.CollectEncryptedCaches)
            return;

        foreach (var offset in serverEnt.Comp.CacheSpawnOffsets)
        {
            var spawnCoordinates = new EntityCoordinates(goal.EntityId, goal.Position + offset);
            if (!spawnCoordinates.IsValid(EntityManager))
                continue;

            Spawn("BitrunningEncryptedCacheObjectiveSpawner", spawnCoordinates);
        }
    }

    private void SpawnDeliveryCacheCrates(QuantumServerComponent server, EntityCoordinates fallbackCoordinates)
    {
        var hasMarker = false;
        var markers = EntityQueryEnumerator<BitrunningObjectiveCacheCrateSpawnMarkerComponent, TransformComponent>();
        while (markers.MoveNext(out var uid, out var marker, out var xform))
        {
            if (xform.MapUid != server.DomainMapUid)
                continue;

            Spawn(marker.CratePrototype, Transform(uid).Coordinates);
            hasMarker = true;
        }

        if (hasMarker)
            return;

        for (var i = 0; i < Math.Max(server.ObjectiveGoal, 1); i++)
        {
            Spawn("CrateBitrunSecure", fallbackCoordinates);
        }
    }

    public bool StopDomain(Entity<QuantumServerComponent> serverEnt, bool immediate = false)
    {
        foreach (var connection in serverEnt.Comp.ActiveConnections.ToArray())
        {
            DisconnectAvatar(connection, false);
        }

        if (serverEnt.Comp.DomainMapUid is { } mapUid)
            _map.DeleteMap(Comp<MapComponent>(mapUid).MapId);

        serverEnt.Comp.DomainMapUid = null;
        serverEnt.Comp.DomainGridUid = null;
        serverEnt.Comp.CurrentDomain = null;
        serverEnt.Comp.Occupants.Clear();
        serverEnt.Comp.ActiveConnections.Clear();
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;
        serverEnt.Comp.SpawnCoordinates = null;
        serverEnt.Comp.ObjectivePoints = 0;
        serverEnt.Comp.ObjectiveCompleted = false;
        serverEnt.Comp.GrantedItemDisks.Clear();
        serverEnt.Comp.WasRandomizedRun = false;

        if (immediate)
        {
            serverEnt.Comp.State = BitrunningServerState.Ready;
            serverEnt.Comp.CooldownEndTime = TimeSpan.Zero;
        }
        else
        {
            serverEnt.Comp.State = BitrunningServerState.CoolingDown;
            var effectiveEfficiency = Math.Max(serverEnt.Comp.CooldownEfficiency, 0.001f);
            var delay = TimeSpan.FromSeconds(serverEnt.Comp.Cooldown.TotalSeconds / effectiveEfficiency);
            serverEnt.Comp.CooldownEndTime = _timing.CurTime + delay;
            Timer.Spawn(delay,
                () =>
                {
                    if (!TryComp(serverEnt.Owner, out QuantumServerComponent? server))
                        return;

                    server.State = BitrunningServerState.Ready;
                    server.CooldownEndTime = TimeSpan.Zero;
                    UpdateServerVisuals((serverEnt.Owner, server));
                    Dirty(serverEnt.Owner, server);
                });
        }

        UpdateServerVisuals(serverEnt);
        Dirty(serverEnt);
        return true;
    }

    public bool TryConnectRunner(EntityUid serverUid, EntityUid podUid, EntityUid user)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server) || !TryComp<NetpodComponent>(podUid, out var pod))
            return false;

        if (server.State != BitrunningServerState.Running)
            return false;

        if (pod.Occupant != null && pod.Occupant != user)
            return false;

        if (pod.Avatar != null)
        {
            if (TryReconnectRunner((podUid, pod), user))
                return true;

            if (pod.Avatar is { } oldAvatar && Exists(oldAvatar))
                DisconnectAvatar(oldAvatar, true);

            pod.Avatar = null;
            Dirty(podUid, pod);
        }

        if (server.ExitCoordinates == null)
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return false;

        var avatar = Spawn(server.AvatarPrototype, server.SpawnCoordinates ?? server.ExitCoordinates.Value);
        EnsureComp<BitrunningDomainRuntimeComponent>(avatar);
        _metaData.SetEntityName(avatar, Name(user));

        var connection = EnsureComp<AvatarConnectionComponent>(avatar);
        connection.OriginalBody = user;
        connection.Server = serverUid;
        connection.Netpod = podUid;
        connection.NoHit = true;
        connection.DeleteOnDisconnect = GetDeleteOnDisconnect(server);
        EnsureComp<AvatarNavRelayComponent>(avatar).RelayEntity = podUid;

        _mind.TransferTo(mindId, avatar, mind: mind);
        TryApplyAvatarOutfit(avatar, server, pod);
        SetAvatarBroadcastEnabled(avatar, server, server.BroadcastEnabled);
        _actions.AddAction(avatar, ref connection.DisconnectActionEntity, connection.DisconnectActionPrototype, avatar);
        var objectivePopupText = server.ObjectiveCompleted
            ? Loc.GetString("bitrunning-objective-completed")
            : GetObjectiveInstructions(server);
        _popup.PopupEntity(objectivePopupText, avatar, avatar, PopupType.Large);

        pod.Occupant = user;
        pod.Avatar = avatar;
        pod.LinkedServer = serverUid;

        server.ActiveConnections.Add(avatar);
        server.Occupants.Add(avatar);

        Dirty(podUid, pod);
        _netpod.UpdateVisuals((podUid, pod));
        Dirty(serverUid, server);
        Dirty(avatar, connection);
        _bitrunningDisk.RefreshAvatarEffects(avatar);
        return true;
    }

    private bool TryReconnectRunner(Entity<NetpodComponent> pod, EntityUid user)
    {
        if (pod.Comp.Avatar is not { } avatarUid || !TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
            return false;

        if (HasComp<ActorComponent>(avatarUid))
            return false;

        if (connection.OriginalBody == null || connection.OriginalBody != user)
            return false;

        if (TryComp<MobStateComponent>(avatarUid, out var state) && state.CurrentState == MobState.Dead)
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return false;

        _mind.TransferTo(mindId, avatarUid, mind: mind);
        _actions.AddAction(avatarUid, ref connection.DisconnectActionEntity, connection.DisconnectActionPrototype, avatarUid);
        EnsureComp<AvatarNavRelayComponent>(avatarUid).RelayEntity = pod.Owner;

        if (connection.Server != null && TryComp<QuantumServerComponent>(connection.Server.Value, out var server))
        {
            server.ActiveConnections.Add(avatarUid);
            server.Occupants.Add(avatarUid);
            var objectivePopupText = server.ObjectiveCompleted
                ? Loc.GetString("bitrunning-objective-completed")
                : GetObjectiveInstructions(server);
            _popup.PopupEntity(objectivePopupText, avatarUid, avatarUid, PopupType.Large);
            Dirty(connection.Server.Value, server);
        }

        Dirty(avatarUid, connection);
        _bitrunningDisk.RefreshAvatarEffects(avatarUid);
        return true;
    }

    private bool GetDeleteOnDisconnect(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain) || domain == null)
            return false;

        return domain.DeleteAvatarOnDisconnect;
    }

    private void TryApplyAvatarOutfit(EntityUid avatar, QuantumServerComponent server, NetpodComponent pod)
    {
        if (!TryResolveLoadout(server, pod, out var loadoutId))
            return;

        _outfit.SetOutfit(avatar, loadoutId);
    }

    private bool TryResolveLoadout(QuantumServerComponent server, NetpodComponent pod, out string loadout)
    {
        loadout = string.Empty;

        if (server.CurrentDomain != null &&
            _domains.TryGetDomain(server.CurrentDomain, out var domain) &&
            domain is { ForcedLoadout: not null })
        {
            loadout = domain.ForcedLoadout.Value;
            return true;
        }

        if (pod.PreferredLoadout == null)
            return false;

        loadout = pod.PreferredLoadout.Value;
        return true;

    }

    private void SetAvatarBroadcastEnabled(EntityUid avatar, QuantumServerComponent server, bool enabled)
    {
        if (!enabled)
        {
            RemCompDeferred<SurveillanceCameraComponent>(avatar);
            RemCompDeferred<DeviceNetworkComponent>(avatar);
            RemCompDeferred<WirelessNetworkComponent>(avatar);
            return;
        }

        EnsureComp<WirelessNetworkComponent>(avatar).Range = server.BroadcastWirelessRange;

        var device = EnsureComp<DeviceNetworkComponent>(avatar);
        device.NetIdEnum = DeviceNetworkComponent.DeviceNetIdDefaults.Wireless;
        device.ReceiveFrequencyId = "SurveillanceCameraEntertainment";
        device.TransmitFrequencyId = "SurveillanceCamera";

        EnsureComp<SurveillanceCameraComponent>(avatar);
    }

    public void DisconnectAvatar(EntityUid avatarUid, bool harmful)
    {
        if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
            return;

        _actions.RemoveAction(connection.DisconnectActionEntity);

        var originalBody = connection.OriginalBody;
        var serverUid = connection.Server;
        var podUid = connection.Netpod;
        var canRedirectToBitrunner = CanRedirectToBitrunnerBody(connection, originalBody);

        if (canRedirectToBitrunner && TryComp<MindContainerComponent>(avatarUid, out var container) && container.Mind is { } mindId && originalBody is { } bodyToTransfer)
            _mind.TransferTo(mindId, bodyToTransfer);

        if (podUid != null && TryComp<NetpodComponent>(podUid.Value, out var pod))
        {
            pod.Occupant = TryComp<NetpodContainerComponent>(podUid.Value, out var containerComp)
                ? containerComp.BodyContainer.ContainedEntity
                : null;

            if (harmful || connection.DeleteOnDisconnect)
                pod.Avatar = null;

            Dirty(podUid.Value, pod);
            _netpod.UpdateVisuals((podUid.Value, pod));
            _netpod.EjectOccupant(podUid.Value);
        }

        if (!harmful && canRedirectToBitrunner && originalBody is { } body && serverUid is { } currentServerUid && TryComp<QuantumServerComponent>(currentServerUid, out var currentServer))
        {
            _stun.TryAddParalyzeDuration(body, currentServer.ExitParalyzeTime);

            _statusEffects.TryUpdateStatusEffectDuration(
                body,
                ExitBlindnessStatusEffect,
                currentServer.ExitBlindnessTime);
        }

        if (serverUid != null && TryComp<QuantumServerComponent>(serverUid.Value, out var server))
        {
            server.ActiveConnections.Remove(avatarUid);
            server.Occupants.Remove(avatarUid);
            Dirty(serverUid.Value, server);
        }

        if (harmful || connection.DeleteOnDisconnect)
            QueueDel(avatarUid);
    }

    public void AddObjectiveProgress(EntityUid serverUid, int points)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        if (server.State != BitrunningServerState.Running)
            return;

        if (server.ObjectiveCompleted)
            return;

        if (!HasActiveObjective(server))
            return;

        server.ObjectivePoints += points;
        if (server.ObjectivePoints >= server.ObjectiveGoal)
            CompleteObjective((serverUid, server));

        Dirty(serverUid, server);
    }

    public void AddObjectivePoint(EntityUid serverUid, int points)
    {
        AddObjectiveProgress(serverUid, points);
    }

    public bool TryGetServerByDomainMap(EntityUid mapUid, out EntityUid serverUid, out QuantumServerComponent server)
    {
        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var foundUid, out var foundServer))
        {
            if (foundServer.DomainMapUid != mapUid)
                continue;

            serverUid = foundUid;
            server = foundServer;
            return true;
        }

        serverUid = default;
        server = default!;
        return false;
    }

    private string GetObjectiveInstructions(QuantumServerComponent server)
    {
        if (!HasActiveObjective(server))
            return Loc.GetString("bitrunning-training-instructions-none");

        var target = server.ObjectiveGoal.ToString();
        return server.ObjectiveType switch
        {
            BitrunningObjectiveType.CollectEncryptedCaches => Loc.GetString("bitrunning-training-instructions-collect", ("target", target)),
            BitrunningObjectiveType.DeliveryCacheCrate => Loc.GetString("bitrunning-training-instructions-delivery", ("target", target)),
            BitrunningObjectiveType.EliminateEnemies => Loc.GetString("bitrunning-training-instructions-eliminate", ("target", target)),
            _ => Loc.GetString("bitrunning-training-instructions-collect", ("target", target)),
        };
    }

    private void CompleteObjective(Entity<QuantumServerComponent> serverEnt)
    {
        if (serverEnt.Comp.ObjectiveCompleted)
            return;

        serverEnt.Comp.ObjectiveCompleted = true;
        var serverRewardMultiplier = CalculateServerRewardMultiplier(serverEnt.Comp);
        var bitrunningRewardMultiplier = CalculateBitrunningRewardMultiplier(serverEnt.Comp);

        if (ShouldSpawnCompletionRewardCache(serverEnt.Comp) &&
            serverEnt.Comp.ObjectiveType != BitrunningObjectiveType.DeliveryCacheCrate &&
            serverEnt.Comp.CacheCoordinates is { } cacheCoordinates)
            Spawn(serverEnt.Comp.RewardCachePrototype, cacheCoordinates);

        var baseServerReward = GetDomainServerReward(serverEnt.Comp);
        var baseBitrunningReward = GetDomainBitrunningReward(serverEnt.Comp);
        var randomServerBonus = GetRandomServerBonusReward(serverEnt.Comp);
        var randomBitrunningBonus = GetRandomBitrunningBonusReward(serverEnt.Comp);

        var serverReward = Math.Max(0, (int) MathF.Round(baseServerReward * serverRewardMultiplier));
        var bitrunningReward = Math.Max(0, (int) MathF.Round(baseBitrunningReward * bitrunningRewardMultiplier));

        if (serverEnt.Comp.WasRandomizedRun)
        {
            serverReward += randomServerBonus;
            bitrunningReward += randomBitrunningBonus;
        }

        if (serverReward > 0)
            serverEnt.Comp.Points += serverReward;

        AwardParticipants(serverEnt.Comp, (uint) bitrunningReward);

        var objectiveCompletedText = Loc.GetString("bitrunning-objective-completed-rewards",
            ("server", serverReward),
            ("np", bitrunningReward));

        if (serverEnt.Comp.BroadcastEnabled)
        {
            foreach (var avatar in serverEnt.Comp.Occupants)
            {
                if (Exists(avatar))
                    _popup.PopupEntity(objectiveCompletedText, serverEnt.Owner, avatar, PopupType.LargeCaution);
            }
        }
        else if (serverEnt.Comp.Occupants.FirstOrDefault() is { } avatar)
        {
            _popup.PopupEntity(objectiveCompletedText, serverEnt.Owner, avatar, PopupType.LargeCaution);
        }

        if (ShouldAutoStopOnObjectiveComplete(serverEnt.Comp))
            StopDomain(serverEnt);

        Dirty(serverEnt);
    }

    private void AwardParticipants(QuantumServerComponent server, uint reward)
    {
        if (reward == 0)
            return;

        var rewarded = new HashSet<EntityUid>();

        foreach (var avatarUid in server.ActiveConnections)
        {
            if (!TryComp<AvatarConnectionComponent>(avatarUid, out var connection))
                continue;

            if (connection.OriginalBody is not { } bodyUid)
                continue;

            if (!rewarded.Add(bodyUid))
                continue;

            if (_bitrunningPoints.GetPointComp(bodyUid) is not { } account)
                continue;

            _bitrunningPoints.AddPoints(account, reward);
        }
    }

    private bool TryGetCurrentDomain(QuantumServerComponent server, out BitrunningVirtualDomainPrototype? domain)
    {
        domain = null;
        return server.CurrentDomain != null &&
               _domains.TryGetDomain(server.CurrentDomain, out domain) &&
               domain != null;
    }

    private int GetDomainServerReward(QuantumServerComponent server)
    {
        if (!TryGetCurrentDomain(server, out var domain) || domain == null)
            return 0;

        return domain.ServerRewardPoints;
    }

    private int GetDomainBitrunningReward(QuantumServerComponent server)
    {
        if (!TryGetCurrentDomain(server, out var domain) || domain == null)
            return 0;

        return domain.BitrunningRewardPoints;
    }

    private int GetRandomServerBonusReward(QuantumServerComponent server)
    {
        if (!TryGetCurrentDomain(server, out var domain) || domain == null)
            return 0;

        return domain.RandomServerBonusPoints;
    }

    private int GetRandomBitrunningBonusReward(QuantumServerComponent server)
    {
        if (!TryGetCurrentDomain(server, out var domain) || domain == null)
            return 0;

        return domain.RandomBitrunningBonusPoints;
    }

    private bool ShouldAutoStopOnObjectiveComplete(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain) || domain == null)
            return false;

        return domain.AutoStopOnObjectiveComplete;
    }

    private bool ShouldSpawnCompletionRewardCache(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain) || domain == null)
            return true;

        return domain.SpawnRewardCacheOnObjectiveComplete;
    }

    private static bool HasActiveObjective(QuantumServerComponent server)
    {
        return server.ObjectiveGoal > 0;
    }

    private float CalculateServerRewardMultiplier(QuantumServerComponent server)
    {
        return CalculateBaseRewardMultiplier(server);
    }

    private float CalculateBitrunningRewardMultiplier(QuantumServerComponent server)
    {
        return CalculateBaseRewardMultiplier(server);
    }

    private float CalculateBaseRewardMultiplier(QuantumServerComponent server)
    {
        var noHitCount = 0;
        foreach (var uid in server.ActiveConnections)
        {
            if (CompOrNull<AvatarConnectionComponent>(uid)?.NoHit == true)
                noHitCount++;
        }

        var total = 0.8f;
        total += server.QualityBonus;
        total += Math.Max(0, server.ActiveConnections.Count - 1) * 0.5f;
        total += noHitCount * 0.4f;
        total += server.ThreatsSpawned * 0.5f;
        return Math.Max(0.5f, total);
    }

    private void OnAvatarDamaged(Entity<AvatarConnectionComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageIncreased)
            return;

        ent.Comp.NoHit = false;
        Dirty(ent);
    }

    private void OnAvatarStateChanged(Entity<AvatarConnectionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (CanRedirectToBitrunnerBody(ent.Comp, ent.Comp.OriginalBody) && ent.Comp.OriginalBody is { } body && TryComp<DamageableComponent>(ent, out var avatarDamage))
        {
            var scaledDamage = avatarDamage.TotalDamage > 0
                ? avatarDamage.Damage * 0.40f
                : new DamageSpecifier
                {
                    DamageDict =
                    {
                        ["Blunt"] = 20f,
                        ["Cellular"] = 2f, // No brain damage, lol 🥹
                    },
                };

            _damageable.TryChangeDamage(body, scaledDamage, ignoreResistances: true, targetPart: TargetBodyPart.Head);
        }

        DisconnectAvatar(ent, true);
    }

    private void OnAvatarDisconnectAction(Entity<AvatarConnectionComponent> ent, ref BitrunningDisconnectAvatarActionEvent args)
    {
        if (args.Handled)
            return;

        DisconnectAvatar(ent, false);
        args.Handled = true;
    }

    private static void OnAvatarSuicideGhost(Entity<AvatarConnectionComponent> ent, ref SuicideGhostEvent args)
    {
        args.Handled = true;
        args.CanReturnToBody = true;
    }

    private void OnAvatarSuicide(Entity<AvatarConnectionComponent> ent, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        DisconnectAvatar(ent, true);
        args.Handled = true;
    }

    private bool CanRedirectToBitrunnerBody(AvatarConnectionComponent connection, EntityUid? originalBody)
    {
        if (originalBody is not { } bodyUid || connection.Netpod is not { } podUid)
            return false;

        if (!TryComp<NetpodContainerComponent>(podUid, out var containerComp))
            return false;

        return containerComp.BodyContainer.ContainedEntity == bodyUid;
    }

    public void SetBroadcastState(EntityUid serverUid, bool enabled)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        server.BroadcastEnabled = enabled;
        Dirty(serverUid, server);

        foreach (var avatar in server.ActiveConnections)
        {
            if (Exists(avatar))
                SetAvatarBroadcastEnabled(avatar, server, enabled);
        }
    }

    public string? GetRandomDomainId(EntityUid serverUid)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return null;

        var emagged = HasComp<EmaggedComponent>(serverUid);
        var allowed = _domains.GetAllDomains()
            .Where(d => d.Cost <= server.Points)
            .Where(d => emagged || d.Difficulty != BitrunningDifficulty.Extreme)
            .Select(d => d.ID)
            .ToList();

        return allowed.Count == 0
            ? null
            : _random.Pick(allowed);
    }
}
