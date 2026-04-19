using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class NetpodSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly TimeSpan PodAnimationDuration = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<NetpodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NetpodComponent, EntityTerminatingEvent>(OnDestroyed);
        SubscribeLocalEvent<NetpodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<NetpodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<NetpodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<NetpodComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NetpodComponent, NetpodSelectLoadoutMessage>(OnSelectLoadout);
    }

    private void OnInit(Entity<NetpodComponent> ent, ref ComponentInit args)
    {
        var containerComp = EnsureComp<NetpodContainerComponent>(ent);
        containerComp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent, "netpod-body");
        ent.Comp.Occupant = containerComp.BodyContainer.ContainedEntity;
        UpdateVisuals(ent);
    }

    private void OnDestroyed(Entity<NetpodComponent> ent, ref EntityTerminatingEvent args)
    {
        EjectOccupant(ent.Owner);

        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);
    }

    private void OnPowerChanged(Entity<NetpodComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);

        EjectOccupant(ent.Owner);
        UpdateVisuals(ent);
    }

    private void OnEntInserted(Entity<NetpodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        if (args.Entity == EntityUid.Invalid || !Exists(args.Entity))
            return;

        if (TryComp<MobStateComponent>(args.Entity, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            EjectOccupant(ent.Owner);
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-enter-failed"), ent, args.Entity);
            return;
        }

        ent.Comp.Occupant = args.Entity;
        Dirty(ent);

        if (ent.Comp.Avatar != null)
        {
            UpdateVisuals(ent);
            TryAutoConnect(ent, args.Entity);
            return;
        }

        SetVisualState(ent, NetpodVisualState.Opening);
        _audio.PlayPvs(ent.Comp.CloseSound, ent);
        TryAutoConnect(ent, args.Entity);
        Timer.Spawn(PodAnimationDuration,
            () =>
        {
            if (!Exists(ent.Owner))
                return;

            UpdateVisuals(ent);
        });
    }

    private void OnEntRemoved(Entity<NetpodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        ent.Comp.Occupant = null;
        Dirty(ent);
        if (ent.Comp.Avatar != null)
        {
            UpdateVisuals(ent);
            return;
        }

        SetVisualState(ent, NetpodVisualState.Closing);
        _audio.PlayPvs(ent.Comp.OpenSound, ent);
        Timer.Spawn(PodAnimationDuration,
            () =>
        {
            if (!Exists(ent.Owner))
                return;

            UpdateVisuals(ent);
        });
    }

    private void OnUiOpened(Entity<NetpodComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSelectLoadout(Entity<NetpodComponent> ent, ref NetpodSelectLoadoutMessage args)
    {
        if (!_prototype.HasIndex<StartingGearPrototype>(args.LoadoutId))
            return;

        if (!ent.Comp.AllowedLoadout.Contains(args.LoadoutId))
            return;

        ent.Comp.PreferredLoadout = args.LoadoutId;
        Dirty(ent);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<NetpodComponent> ent)
    {
        var loadouts = new List<NetpodLoadoutEntry>();
        foreach (var loadoutId in ent.Comp.AllowedLoadout)
        {
            if (!_prototype.TryIndex(loadoutId, out _))
                continue;

            loadouts.Add(new NetpodLoadoutEntry(loadoutId, loadoutId));
        }

        loadouts.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        _ui.SetUiState(ent.Owner, NetpodUiKey.Key, new NetpodBoundUiState(ent.Comp.PreferredLoadout, loadouts));
    }

    public bool EjectOccupant(EntityUid podUid)
    {
        if (!TryComp<NetpodContainerComponent>(podUid, out var containerComp))
            return false;

        if (containerComp.BodyContainer.ContainedEntity is not { } contained)
            return false;

        return _container.Remove(contained, containerComp.BodyContainer);
    }

    private EntityUid? ResolveServer(Entity<NetpodComponent> ent)
    {
        if (ent.Comp.LinkedServer is { } linked && Exists(linked) && HasComp<QuantumServerComponent>(linked))
            return linked;

        var podCoords = Transform(ent.Owner).Coordinates;
        EntityUid? nearestServer = null;
        var nearestDistance = float.MaxValue;

        foreach (var uid in _lookup.GetEntitiesInRange(ent.Owner, 6f))
        {
            if (!HasComp<QuantumServerComponent>(uid))
                continue;

            if (!Transform(uid).Coordinates.TryDistance(EntityManager, podCoords, out var distance))
                continue;

            distance *= distance;
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestServer = uid;
        }

        if (nearestServer == null)
            return null;

        if (ent.Comp.LinkedServer == nearestServer)
            return nearestServer;

        ent.Comp.LinkedServer = nearestServer;
        Dirty(ent);

        return nearestServer;
    }

    private void TryAutoConnect(Entity<NetpodComponent> ent, EntityUid user)
    {
        var serverUid = ResolveServer(ent);
        if (serverUid == null)
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-no-server"), ent, user);
            return;
        }

        if (_server.TryConnectRunner(serverUid.Value, ent.Owner, user))
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connected"), ent, user);
            return;
        }

        _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connect-failed"), ent, user);
    }

    public void UpdateVisuals(Entity<NetpodComponent> ent)
    {
        var state = ent.Comp.Avatar != null
            ? ent.Comp.Occupant != null
                ? NetpodVisualState.Active
                : NetpodVisualState.OpenActive
            : ent.Comp.Occupant != null
                ? NetpodVisualState.Closed
                : NetpodVisualState.Open;

        SetVisualState(ent, state);
    }

    private void SetVisualState(Entity<NetpodComponent> ent, NetpodVisualState state)
    {
        _appearance.SetData(ent, NetpodVisuals.State, state);
    }
}
