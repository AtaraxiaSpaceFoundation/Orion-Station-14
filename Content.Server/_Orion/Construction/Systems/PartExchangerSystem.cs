using System.Linq;
using Content.Server._Orion.Construction.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Orion.Construction.Events;
using Content.Shared._Orion.Construction.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Construction.Systems;

public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (component.DoDistanceCheck && !args.CanReach)
            return;

        if (args.Target is not { } target)
            return;

        if (!HasComp<MachineComponent>(target))
            return;

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"), target, args.User);
            args.Handled = true;
            return;
        }

        var stream = _audio.PlayPvs(component.ExchangeSound, uid);
        if (stream != null)
            component.AudioStream = stream.Value.Entity;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ExchangeDuration, new ExchangerDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, PartExchangerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
        {
            component.AudioStream = _audio.Stop(component.AudioStream);
            return;
        }

        if (args.Handled || args.Args.Target is not { } target)
            return;

        if (!TryComp<MachineComponent>(target, out var machine) || !TryComp<StorageComponent>(uid, out var storage))
            return;

        var machineParts = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid Uid, MachinePartState State)>>();
        var storageParts = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid Uid, MachinePartState State)>>();

        foreach (var partUid in machine.PartContainer.ContainedEntities)
        {
            if (!_construction.GetMachinePartState(partUid, out var partState))
                continue;

            if (!machineParts.TryGetValue(partState.Part.Part, out var bucket))
            {
                bucket = new();
                machineParts[partState.Part.Part] = bucket;
            }
            bucket.Add((partUid, partState));
        }

        foreach (var partUid in storage.Container.ContainedEntities)
        {
            if (!_construction.GetMachinePartState(partUid, out var partState))
                continue;

            if (!storageParts.TryGetValue(partState.Part.Part, out var bucket))
            {
                bucket = new();
                storageParts[partState.Part.Part] = bucket;
            }
            bucket.Add((partUid, partState));
        }

        var changed = false;

        foreach (var (partType, current) in machineParts)
        {
            if (!storageParts.TryGetValue(partType, out var available))
                continue;

            available.Sort((a, b) => b.State.Part.Tier.CompareTo(a.State.Part.Tier));

            var requiredCount = current.Sum(x => x.State.Quantity());
            var selected = new List<EntityUid>();

            foreach (var candidate in available)
            {
                if (requiredCount <= 0)
                    break;

                if (candidate.State.Stack is { } stack && stack.Count > requiredCount)
                {
                    var split = _stack.Split(candidate.Uid, requiredCount, Transform(uid).Coordinates, stack);
                    if (split != null)
                        selected.Add(split.Value);

                    requiredCount = 0;
                    break;
                }

                selected.Add(candidate.Uid);
                requiredCount -= candidate.State.Quantity();
            }

            if (requiredCount > 0)
                continue;

            foreach (var (oldUid, _) in current)
            {
                _container.RemoveEntity(target, oldUid);
                _storage.Insert(uid, oldUid, out _, playSound: false);
            }

            foreach (var newUid in selected)
            {
                _container.TryRemoveFromContainer(newUid, force: true);
                _container.Insert(newUid, machine.PartContainer, force: true);
            }

            changed = true;
        }

        if (changed)
            _construction.RefreshParts(target, machine);

        args.Handled = true;
    }
}
