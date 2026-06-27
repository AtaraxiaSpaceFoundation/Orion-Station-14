using Content.Server._Orion.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server._Orion.Chemistry.EntitySystems;

/// <summary>
/// Manages buffer capacity of ChemMaster based on two internal capacity beakers.
/// On MapInit: transfers beaker contents to buffer and sets capacity.
/// On ComponentShutdown: distributes buffer back to beakers and ejects them.
/// Remainder is spilled on the floor.
/// </summary>
public sealed class ChemMasterBeakerCapacitySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<ChemMasterBeakerCapacityComponent> ent, ref MapInitEvent args)
    {
        TransferBeakersToBuffer(ent);
        RecalculateCapacity(ent);
    }

    private void OnInserted(Entity<ChemMasterBeakerCapacityComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapacitySlot1 && args.Container.ID != ent.Comp.CapacitySlot2)
            return;
        RecalculateCapacity(ent);
    }

    private void OnRemoved(Entity<ChemMasterBeakerCapacityComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.CapacitySlot1 && args.Container.ID != ent.Comp.CapacitySlot2)
            return;
        RecalculateCapacity(ent);
    }

    private void OnShutdown(Entity<ChemMasterBeakerCapacityComponent> ent, ref ComponentShutdown args)
    {
        ReturnBufferToBeakers(ent);

        _slots.TryEject(ent.Owner, ent.Comp.CapacitySlot1, null, out _);
        _slots.TryEject(ent.Owner, ent.Comp.CapacitySlot2, null, out _);
    }

    private void RecalculateCapacity(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var buffer))
            return;

        var total = FixedPoint2.Zero;

        foreach (var slotId in new[] { ent.Comp.CapacitySlot1, ent.Comp.CapacitySlot2 })
        {
            var beaker = _slots.GetItemOrNull(ent.Owner, slotId);
            if (beaker == null)
                continue;

            if (_solutions.TryGetFitsInDispenser(beaker.Value, out _, out var sol))
                total += sol.MaxVolume;
        }

        buffer.MaxVolume = total == FixedPoint2.Zero
            ? ent.Comp.FallbackCapacity
            : total * ent.Comp.Multiplier;
    }

    // On MapInit: transfers any pre-existing reagents in the capacity beakers into the buffer.
    private void TransferBeakersToBuffer(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out var bufSoln, out var buffer))
            return;

        foreach (var slotId in new[] { ent.Comp.CapacitySlot1, ent.Comp.CapacitySlot2 })
        {
            var beaker = _slots.GetItemOrNull(ent.Owner, slotId);
            if (beaker == null)
                continue;

            if (!_solutions.TryGetFitsInDispenser(beaker.Value, out _, out var beakerSol)
                || beakerSol.Volume == FixedPoint2.Zero)
                continue;

            // Temporarily expand buffer to accept all beaker contents before capping
            buffer.MaxVolume += beakerSol.Volume;
            var split = beakerSol.SplitSolution(beakerSol.Volume);
            _solutions.TryAddSolution(bufSoln.Value, split);
        }
    }

    // On shutdown: distributes buffer contents back into the two capacity beakers. Any remainder that doesn't fit is spilled on the floor.
    private void ReturnBufferToBeakers(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var buffer)
            || buffer.Volume == FixedPoint2.Zero)
            return;

        foreach (var slotId in new[] { ent.Comp.CapacitySlot1, ent.Comp.CapacitySlot2 })
        {
            if (buffer.Volume == FixedPoint2.Zero)
                return;

            var beaker = _slots.GetItemOrNull(ent.Owner, slotId);
            if (beaker == null)
                continue;

            if (!_solutions.TryGetFitsInDispenser(beaker.Value, out var beakerSoln, out var beakerSol))
                continue;

            var canFit = beakerSol.AvailableVolume;
            if (canFit <= FixedPoint2.Zero)
                continue;

            var toTransfer = FixedPoint2.Min(canFit, buffer.Volume);
            _solutions.TryAddSolution(beakerSoln.Value, buffer.SplitSolution(toTransfer));
        }

        // Spill leftover on the floor
        if (buffer.Volume > FixedPoint2.Zero)
            _puddle.TrySpillAt(ent.Owner, buffer.SplitSolution(buffer.Volume), out _);
    }
}
