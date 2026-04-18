using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class NetpodSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private static readonly TimeSpan PodAnimationDuration = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<NetpodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NetpodComponent, EntityTerminatingEvent>(OnDestroyed);
        SubscribeLocalEvent<NetpodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<NetpodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
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
        if (TryComp<NetpodContainerComponent>(ent, out var containerComp) && containerComp.BodyContainer.ContainedEntity is { } contained)
            _container.Remove(contained, containerComp.BodyContainer);

        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);
    }

    private void OnEntInserted(Entity<NetpodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != "netpod-body")
            return;

        ent.Comp.Occupant = args.Entity;
        Dirty(ent);
        if (ent.Comp.Avatar != null)
        {
            UpdateVisuals(ent);
            return;
        }

        SetVisualState(ent, NetpodVisualState.Opening);
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
        Timer.Spawn(PodAnimationDuration,
            () =>
        {
            if (!Exists(ent.Owner))
                return;

            UpdateVisuals(ent);
        });
    }

    private EntityUid? ResolveServer(Entity<NetpodComponent> ent)
    {
        if (ent.Comp.LinkedServer is { } linked && Exists(linked) && HasComp<QuantumServerComponent>(linked))
            return linked;

        var nearby = _lookup.GetEntitiesInRange(ent.Owner, 2f);
        foreach (var uid in nearby)
        {
            if (!HasComp<QuantumServerComponent>(uid))
                continue;

            ent.Comp.LinkedServer = uid;
            Dirty(ent);
            return uid;
        }

        return null;
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
            ? NetpodVisualState.Active
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
