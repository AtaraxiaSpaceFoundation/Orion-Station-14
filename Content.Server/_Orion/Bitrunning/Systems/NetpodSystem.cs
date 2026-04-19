using Content.Server.Popups;
using Content.Server._Orion.Bitrunning.Components;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Implants;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Robust.Server.GameObjects;
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

    private static readonly TimeSpan PodAnimationDuration = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<NetpodComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NetpodComponent, EntityTerminatingEvent>(OnDestroyed);
        SubscribeLocalEvent<NetpodComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<NetpodComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<NetpodComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<NetpodComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<NetpodComponent, NetpodSelectOutfitMessage>(OnSelectOutfit);
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

        ent.Comp.Occupant = args.Entity;
        Dirty(ent);

        if (TryComp<MobStateComponent>(args.Entity, out var mobState) && mobState.CurrentState == MobState.Dead)
        {
            EjectOccupant(ent.Owner);
            _popup.PopupEntity(Loc.GetString("bitrunning-netpod-enter-failed"), ent, args.Entity);
            return;
        }

        if (ent.Comp.Avatar != null)
        {
            UpdateVisuals(ent);
            TryAutoConnect(ent, args.Entity);
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

    private void OnUiOpened(Entity<NetpodComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnSelectOutfit(Entity<NetpodComponent> ent, ref NetpodSelectOutfitMessage args)
    {
        if (!_prototype.HasIndex<ChameleonOutfitPrototype>(args.OutfitId))
            return;

        ent.Comp.PreferredOutfit = args.OutfitId;
        Dirty(ent);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<NetpodComponent> ent)
    {
        var outfits = new List<NetpodOutfitEntry>();
        foreach (var outfit in _prototype.EnumeratePrototypes<ChameleonOutfitPrototype>())
        {
            if (!outfit.ID.EndsWith("ChameleonOutfit", StringComparison.Ordinal))
                continue;

            var displayName = outfit.LoadoutName ?? outfit.Name ?? outfit.ID;
            outfits.Add(new NetpodOutfitEntry(outfit.ID, Loc.GetString(displayName)));
        }

        outfits.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        _ui.SetUiState(ent.Owner, NetpodUiKey.Key, new NetpodBoundUiState(ent.Comp.PreferredOutfit, outfits));
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
