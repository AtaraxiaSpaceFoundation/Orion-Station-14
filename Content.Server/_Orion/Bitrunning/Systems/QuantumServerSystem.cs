using System.Linq;
using System.Numerics;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Server.Actions;
using Content.Server.Clothing.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.SurveillanceCamera;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Damage;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
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

        if (server.Points < domain.Cost)
            return false;

        var mapEntity = _map.CreateMap(out var mapId, runMapInit: true);
        EnsureComp<BitrunningDomainRuntimeComponent>(mapEntity);

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
        server.ObjectiveGoal = domain.ObjectiveGoalPoints;
        server.Points -= domain.Cost;
        server.ThreatsSpawned = 0;
        server.CooldownEndTime = TimeSpan.Zero;

        ResolveDomainMarkers((serverUid, server));
        Dirty(serverUid, server);
        return true;
    }

    private void ResolveDomainMarkers(Entity<QuantumServerComponent> serverEnt)
    {
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;

        if (serverEnt.Comp.DomainMapUid is not { } mapUid)
            return;

        var query = EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out var uid, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            if (HasComp<BitrunningExitMarkerComponent>(uid) && serverEnt.Comp.ExitCoordinates == null)
                serverEnt.Comp.ExitCoordinates = Transform(uid).Coordinates;
            if (HasComp<BitrunningGoalMarkerComponent>(uid) && serverEnt.Comp.GoalCoordinates == null)
                serverEnt.Comp.GoalCoordinates = Transform(uid).Coordinates;
            if (HasComp<BitrunningCacheMarkerComponent>(uid) && serverEnt.Comp.CacheCoordinates == null)
                serverEnt.Comp.CacheCoordinates = Transform(uid).Coordinates;
        }

        if (serverEnt.Comp.ExitCoordinates == null && serverEnt.Comp.DomainGridUid is { } gridUid)
        {
            serverEnt.Comp.ExitCoordinates = TryComp<MapGridComponent>(gridUid, out var gridComp)
                ? new EntityCoordinates(gridUid, gridComp.LocalAABB.Center)
                : new EntityCoordinates(gridUid, Vector2.Zero);
        }

        serverEnt.Comp.GoalCoordinates ??= serverEnt.Comp.ExitCoordinates;
        serverEnt.Comp.CacheCoordinates ??= serverEnt.Comp.GoalCoordinates;

        if (serverEnt.Comp.GoalCoordinates is not { } goal)
            return;

        Spawn("BitrunningEncryptedCacheObjectiveSpawner", goal);
        Spawn("BitrunningEncryptedCacheObjectiveSpawner", new EntityCoordinates(goal.EntityId, goal.Position + new Vector2(1f, 0f)));
        Spawn("BitrunningEncryptedCacheObjectiveSpawner", new EntityCoordinates(goal.EntityId, goal.Position + new Vector2(0f, 1f)));
    }

    public bool StopDomain(Entity<QuantumServerComponent> serverEnt, bool immediate = false)
    {
        foreach (var connection in serverEnt.Comp.ActiveConnections.ToArray())
        {
            DisconnectAvatar(connection, true);
        }

        if (serverEnt.Comp.DomainMapUid is { } mapUid)
        {
            _map.DeleteMap(Comp<MapComponent>(mapUid).MapId);
        }

        serverEnt.Comp.DomainMapUid = null;
        serverEnt.Comp.DomainGridUid = null;
        serverEnt.Comp.CurrentDomain = null;
        serverEnt.Comp.Occupants.Clear();
        serverEnt.Comp.ActiveConnections.Clear();
        serverEnt.Comp.ExitCoordinates = null;
        serverEnt.Comp.GoalCoordinates = null;
        serverEnt.Comp.CacheCoordinates = null;
        serverEnt.Comp.ObjectivePoints = 0;

        if (immediate)
        {
            serverEnt.Comp.State = BitrunningServerState.Ready;
            serverEnt.Comp.CooldownEndTime = TimeSpan.Zero;
        }
        else
        {
            serverEnt.Comp.State = BitrunningServerState.CoolingDown;
            var delay = TimeSpan.FromSeconds(serverEnt.Comp.Cooldown.TotalSeconds * serverEnt.Comp.CooldownEfficiency);
            serverEnt.Comp.CooldownEndTime = _timing.CurTime + delay;
            Timer.Spawn(delay,
                () =>
                {
                    if (!TryComp(serverEnt.Owner, out QuantumServerComponent? server))
                        return;

                    server.State = BitrunningServerState.Ready;
                    server.CooldownEndTime = TimeSpan.Zero;
                    Dirty(serverEnt.Owner, server);
                });
        }

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

            pod.Avatar = null;
            Dirty(podUid, pod);
        }

        if (server.ExitCoordinates == null)
            return false;

        if (!_mind.TryGetMind(user, out var mindId, out var mind))
            return false;

        var avatar = Spawn(server.AvatarPrototype, server.ExitCoordinates.Value);
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
        SetAvatarBroadcastEnabled(avatar, server.BroadcastEnabled);
        _actions.AddAction(avatar, ref connection.DisconnectActionEntity, connection.DisconnectActionPrototype, avatar);
        _popup.PopupEntity(Loc.GetString("bitrunning-training-instructions"), avatar, avatar);

        pod.Occupant = user;
        pod.Avatar = avatar;
        pod.LinkedServer = serverUid;

        server.ActiveConnections.Add(avatar);
        server.Occupants.Add(avatar);

        Dirty(podUid, pod);
        _netpod.UpdateVisuals((podUid, pod));
        Dirty(serverUid, server);
        Dirty(avatar, connection);
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
            Dirty(connection.Server.Value, server);
        }

        Dirty(avatarUid, connection);
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
        if (!TryResolveOutfit(server, pod, out var outfitId))
            return;

        _outfit.SetOutfit(avatar, outfitId);
    }

    private bool TryResolveOutfit(QuantumServerComponent server, NetpodComponent pod, out string outfit)
    {
        outfit = string.Empty;

        if (server.CurrentDomain != null &&
            _domains.TryGetDomain(server.CurrentDomain, out var domain) &&
            domain != null &&
            domain.ForcedOutfit != null &&
            TryGetStartingGear(domain.ForcedOutfit.Value, out outfit))
        {
            return true;
        }

        if (pod.PreferredOutfit != null && TryGetStartingGear(pod.PreferredOutfit.Value, out outfit))
            return true;

        return false;
    }

    private bool TryGetStartingGear(ProtoId<ChameleonOutfitPrototype> outfitId, out string gear)
    {
        gear = string.Empty;
        if (!_prototype.TryIndex(outfitId, out var outfit))
            return false;

        if (outfit.StartingGear != null)
        {
            gear = outfit.StartingGear.Value;
            return true;
        }

        if (outfit.Job != null &&
            _prototype.TryIndex(outfit.Job.Value, out var job) &&
            job.StartingGear != null)
        {
            gear = job.StartingGear;
            return true;
        }

        return false;
    }

    private void SetAvatarBroadcastEnabled(EntityUid avatar, bool enabled)
    {
        if (!enabled)
        {
            RemCompDeferred<SurveillanceCameraComponent>(avatar);
            RemCompDeferred<DeviceNetworkComponent>(avatar);
            RemCompDeferred<WirelessNetworkComponent>(avatar);
            RemCompDeferred<EyeComponent>(avatar);
            return;
        }

        EnsureComp<EyeComponent>(avatar);
        EnsureComp<WirelessNetworkComponent>(avatar).Range = 6767;

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

        _actions.RemoveAction(avatarUid, connection.DisconnectActionEntity);

        var originalBody = connection.OriginalBody;
        var serverUid = connection.Server;
        var podUid = connection.Netpod;

        if (TryComp<MindContainerComponent>(avatarUid, out var container) && container.Mind is { } mindId && originalBody != null)
        {
            _mind.TransferTo(mindId, originalBody);
        }

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

        if (serverUid != null && TryComp<QuantumServerComponent>(serverUid.Value, out var server))
        {
            server.ActiveConnections.Remove(avatarUid);
            server.Occupants.Remove(avatarUid);
            Dirty(serverUid.Value, server);
        }

        if (harmful || connection.DeleteOnDisconnect)
            QueueDel(avatarUid);
    }

    public void AddObjectivePoint(EntityUid serverUid, int points)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        if (server.State != BitrunningServerState.Running)
            return;

        server.ObjectivePoints += points;
        if (server.ObjectivePoints >= server.ObjectiveGoal)
            CompleteObjective((serverUid, server));

        Dirty(serverUid, server);
    }

    private void CompleteObjective(Entity<QuantumServerComponent> serverEnt)
    {
        if (serverEnt.Comp.CacheCoordinates == null)
            return;

        var rewardMultiplier = CalculateRewards(serverEnt.Comp);
        var grade = GradeCompletion(serverEnt.Comp);

        Spawn(serverEnt.Comp.RewardCachePrototype, serverEnt.Comp.CacheCoordinates.Value);
        serverEnt.Comp.Points += (int) MathF.Round(GetDomainReward(serverEnt.Comp) * rewardMultiplier);

        var reportText = Loc.GetString("bitrunning-certificate-template",
            ("domain", serverEnt.Comp.CurrentDomain ?? "unknown"),
            ("time", (_timing.CurTime - serverEnt.Comp.DomainStartTime).ToString("g")),
            ("reward", GetDomainReward(serverEnt.Comp)),
            ("bonus", rewardMultiplier.ToString("0.0")),
            ("threats", serverEnt.Comp.ThreatsSpawned),
            ("grade", grade.ToString()));

        if (serverEnt.Comp.BroadcastEnabled)
        {
            foreach (var avatar in serverEnt.Comp.Occupants)
            {
                if (Exists(avatar))
                    _popup.PopupEntity(reportText, serverEnt.Owner, avatar, PopupType.LargeCaution);
            }
        }
        else if (serverEnt.Comp.Occupants.FirstOrDefault() is { } avatar)
        {
            _popup.PopupEntity(reportText, serverEnt.Owner, avatar, PopupType.LargeCaution);
        }

        StopDomain(serverEnt);
    }

    private int GetDomainReward(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain) || domain == null)
            return 1;

        return domain.RewardPoints;
    }

    private float CalculateRewards(QuantumServerComponent server)
    {
        var total = 0.8f;
        total += server.QualityBonus;
        total += Math.Max(0, server.Occupants.Count - 1) * 0.5f;
        total += server.ActiveConnections.Count(uid => CompOrNull<AvatarConnectionComponent>(uid)?.NoHit == true) * 0.4f;
        total += server.ThreatsSpawned * 0.5f;
        return Math.Max(0.5f, total);
    }

    private BitrunningGrade GradeCompletion(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain) || domain == null)
            return BitrunningGrade.D;

        var seconds = (_timing.CurTime - server.DomainStartTime).TotalSeconds;
        var score = (int) domain.Difficulty * 2 + domain.RewardPoints + server.ThreatsSpawned * 3;
        switch (seconds)
        {
            case < 120:
                score += 4;
                break;
            case < 300:
                score += 2;
                break;
        }

        return score switch
        {
            <= 4 => BitrunningGrade.D,
            <= 7 => BitrunningGrade.C,
            <= 10 => BitrunningGrade.B,
            <= 13 => BitrunningGrade.A,
            _ => BitrunningGrade.S,
        };
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

        if (ent.Comp.OriginalBody is { } body && TryComp<DamageableComponent>(ent, out var avatarDamage))
        {
            var scaledDamage = avatarDamage.TotalDamage > 0
                ? avatarDamage.Damage * 0.5f
                : new DamageSpecifier
                {
                    DamageDict =
                    {
                        ["Blunt"] = 40f,
                    },
                };

            _damageable.TryChangeDamage(body, scaledDamage, ignoreResistances: true);
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

        DisconnectAvatar(ent, false);
        args.Handled = true;
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
                SetAvatarBroadcastEnabled(avatar, enabled);
        }
    }

    public string? GetRandomDomainId(EntityUid serverUid)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return null;

        var allowed = _domains.GetAllDomains().Where(d => d.Cost <= server.Points).Select(d => d.ID).ToList();
        if (allowed.Count == 0)
            return null;

        return _random.Pick(allowed);
    }
}
