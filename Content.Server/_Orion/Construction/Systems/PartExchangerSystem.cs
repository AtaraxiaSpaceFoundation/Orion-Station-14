using System.Linq;
using Content.Server._Orion.Construction.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Orion.Construction.Components;
using Content.Shared._Orion.Construction.Events;
using Content.Shared._Orion.Construction.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
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
    [Dependency] private readonly MachineFrameSystem _machineFrame = default!;

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

        if (!HasComp<MachineComponent>(target) && !HasComp<MachineFrameComponent>(target))
            return;

        if (component.RequireOpenPanel && TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"), target, args.User);
            args.Handled = true;
            return;
        }

        var stream = _audio.PlayPvs(component.ExchangeSound, uid);

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ExchangeDuration, new ExchangerDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        if (started)
        {
            if (stream != null)
                component.AudioStream = stream.Value.Entity;

            args.Handled = true;
        }
        else if (stream != null)
        {
            _audio.Stop(stream.Value.Entity);
        }
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

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        if (TryComp<MachineFrameComponent>(target, out var machineFrame))
        {
            args.Handled = TryInsertIntoMachineFrame(uid, target, storage, machineFrame);
            return;
        }

        if (!TryComp<MachineComponent>(target, out var machine))
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

            foreach (var currentPart in current.OrderBy(part => part.State.Part.Tier))
            {
                var replacementIndex = available.FindIndex(part => part.State.Part.Tier > currentPart.State.Part.Tier);
                if (replacementIndex < 0)
                    continue;

                var replacement = available[replacementIndex];
                var replacementUid = replacement.Uid;
                available.RemoveAt(replacementIndex);

                if (!_container.TryRemoveFromContainer(replacementUid, force: true))
                    continue;

                _container.RemoveEntity(target, currentPart.Uid);

                if (!_storage.Insert(uid, currentPart.Uid, out _, playSound: false))
                {
                    _container.Insert(currentPart.Uid, machine.PartContainer, force: true);
                    _container.Insert(replacementUid, storage.Container, force: true);
                    continue;
                }

                _container.Insert(replacementUid, machine.PartContainer, force: true);
                changed = true;
            }
        }

        if (changed)
            _construction.RefreshParts(target, machine);

        args.Handled = true;
    }

    private bool TryInsertIntoMachineFrame(EntityUid user, EntityUid frameUid, StorageComponent storage, MachineFrameComponent machineFrame)
    {
        var changed = false;

        foreach (var partUid in storage.Container.ContainedEntities.ToArray())
        {
            if (TryComp<MachinePartComponent>(partUid, out var machinePart) && machineFrame.PartRequirements.TryGetValue(machinePart.Part, out var partRequirement) && machineFrame.PartProgress.TryGetValue(machinePart.Part, out var partProgress) && partProgress < partRequirement)
            {
                var remaining = partRequirement - partProgress;
                var count = TryComp<StackComponent>(partUid, out var partStack) ? partStack.Count : 1;
                var amount = Math.Min(remaining, count);
                var partToInsert = partUid;

                if (amount <= 0)
                    continue;

                if (partStack != null && partStack.Count > amount)
                {
                    var split = _stack.Split(partUid, amount, Transform(frameUid).Coordinates, partStack);
                    if (split == null)
                        continue;

                    partToInsert = split.Value;
                }
                else if (!_container.TryRemoveFromContainer(partUid, force: true))
                {
                    continue;
                }

                if (!_container.Insert(partToInsert, machineFrame.PartContainer))
                {
                    _container.Insert(partToInsert, storage.Container, force: true);
                    continue;
                }

                machineFrame.PartProgress[machinePart.Part] += amount;
                changed = true;
                continue;
            }

            if (!TryComp<StackComponent>(partUid, out var stack) || !machineFrame.MaterialRequirements.TryGetValue(stack.StackTypeId, out var materialRequirement) || !machineFrame.MaterialProgress.TryGetValue(stack.StackTypeId, out var materialProgress) || materialProgress >= materialRequirement)
                continue;

            var materialRemaining = materialRequirement - materialProgress;
            var materialAmount = Math.Min(materialRemaining, stack.Count);
            var stackToInsert = partUid;

            if (materialAmount <= 0)
                continue;

            if (stack.Count > materialAmount)
            {
                var split = _stack.Split(partUid, materialAmount, Transform(frameUid).Coordinates, stack);
                if (split == null)
                    continue;

                stackToInsert = split.Value;
            }
            else if (!_container.TryRemoveFromContainer(partUid, force: true))
            {
                continue;
            }

            if (!_container.Insert(stackToInsert, machineFrame.PartContainer))
            {
                _container.Insert(stackToInsert, storage.Container, force: true);
                continue;
            }

            machineFrame.MaterialProgress[stack.StackTypeId] += materialAmount;
            changed = true;
        }

        if (!changed)
            return false;

        if (_machineFrame.IsComplete(machineFrame))
            _popup.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), frameUid, user);

        _machineFrame.RegenerateProgress(machineFrame);
        return true;
    }
}
