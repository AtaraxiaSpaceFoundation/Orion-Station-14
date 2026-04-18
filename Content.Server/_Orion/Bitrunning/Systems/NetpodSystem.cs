using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class NetpodSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NetpodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NetpodComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<NetpodComponent, EntityTerminatingEvent>(OnDestroyed);
    }

    private void OnInit(Entity<NetpodComponent> ent, ref ComponentInit args)
    {
        var containerComp = EnsureComp<NetpodContainerComponent>(ent);
        containerComp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent, "netpod-body");
    }

    private void OnInteractHand(Entity<NetpodComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<NetpodContainerComponent>(ent, out var containerComp))
            return;

        if (containerComp.BodyContainer.ContainedEntity == null)
        {
            if (!_container.Insert(args.User, containerComp.BodyContainer))
            {
                _popup.PopupEntity(Loc.GetString("bitrunning-netpod-enter-failed"), ent, args.User);
                return;
            }

            ent.Comp.Occupant = args.User;
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-entered"), ent, args.User);
        }

        var serverUid = ResolveServer(ent);
        if (serverUid == null)
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-no-server"), ent, args.User);
            return;
        }

        if (ent.Comp.Avatar != null)
        {
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, false);
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-disconnected"), ent, args.User);
            args.Handled = true;
            return;
        }

        var occupant = containerComp.BodyContainer.ContainedEntity ?? args.User;
        if (_server.TryConnectRunner(serverUid.Value, ent.Owner, occupant))
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connected"), ent, args.User);
            args.Handled = true;
            return;
        }

        _popup.PopupEntity(Loc.GetString("bitrunning-netpod-connect-failed"), ent, args.User);
    }

    private void OnDestroyed(Entity<NetpodComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp<NetpodContainerComponent>(ent, out var containerComp) &&
            containerComp.BodyContainer.ContainedEntity is { } contained)
        {
            _container.Remove(contained, containerComp.BodyContainer);
        }

        if (ent.Comp.Avatar != null)
            _server.DisconnectAvatar(ent.Comp.Avatar.Value, true);
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
}
