using Content.Server._Orion.Chemistry.Components;
using Content.Server.Containers;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Containers;

namespace Content.Server._Orion.Chemistry.EntitySystems;

/// <summary>
/// Manages buffer capacity of ChemMaster based on two internal capacity beakers.
/// On MapInit: transfers beaker contents to buffer and sets capacity.
/// </summary>
public sealed class ChemMasterBeakerCapacitySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    private const string MachinePartsContainerName = "machine_parts";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, EntRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, MachineDeconstructedEvent>(
            OnMachineDeconstructed,
            before: [typeof(EmptyOnMachineDeconstructSystem)]);

        SubscribeLocalEvent<ChemMasterBeakerCapacityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ChemMasterBeakerCapacityComponent> ent, ref MapInitEvent args)
    {
        RecalculateCapacity(ent);
        TransferConstructionBeakersToBuffer(ent);
    }

    private void OnInserted(Entity<ChemMasterBeakerCapacityComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != MachinePartsContainerName)
            return;

        if (!HasComp<FitsInDispenserComponent>(args.Entity))
            return;

        RecalculateCapacity(ent);
    }

    private void OnRemoved(Entity<ChemMasterBeakerCapacityComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != MachinePartsContainerName)
            return;

        if (!HasComp<FitsInDispenserComponent>(args.Entity))
            return;

        RecalculateCapacity(ent);
    }

    private void OnMachineDeconstructed(Entity<ChemMasterBeakerCapacityComponent> ent, ref MachineDeconstructedEvent args)
    {
        ReturnBufferToConstructionBeakers(ent);
    }

    private void OnShutdown(Entity<ChemMasterBeakerCapacityComponent> ent, ref ComponentShutdown args)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var buffer)
            || buffer.Volume == FixedPoint2.Zero)
        {
            return;
        }

        var coords = Transform(ent.Owner).Coordinates;
        _puddle.TrySpillAt(coords, buffer.SplitSolution(buffer.Volume), out _);
    }

    public void RefreshFromConstructionBeakers(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        RecalculateCapacity(ent);
    }

    public void EnsureInitialized(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        RecalculateCapacity(ent);
        TransferConstructionBeakersToBuffer(ent);
        RecalculateCapacity(ent);
    }

    private IEnumerable<EntityUid> GetConstructionBeakers(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            yield break;

        if (!_containers.TryGetContainer(uid, MachinePartsContainerName, out var container, manager))
            yield break;

        var found = 0;
        foreach (var entity in container.ContainedEntities)
        {
            if (!HasComp<FitsInDispenserComponent>(entity))
                continue;

            yield return entity;
            found++;

            if (found >= 2)
                yield break;
        }
    }

    private void RecalculateCapacity(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out var bufferSoln, out var buffer))
            return;

        var total = FixedPoint2.Zero;

        foreach (var beaker in GetConstructionBeakers(ent.Owner))
        {
            if (_solutions.TryGetFitsInDispenser(beaker, out _, out var beakerSolution))
                total += beakerSolution.MaxVolume;
        }

        var targetCapacity = total == FixedPoint2.Zero
            ? ent.Comp.FallbackCapacity
            : total * ent.Comp.Multiplier;

        targetCapacity = FixedPoint2.Max(targetCapacity, buffer.Volume);
        _solutions.SetCapacity(bufferSoln.Value, targetCapacity);
    }

    private void TransferConstructionBeakersToBuffer(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        var beakers = new List<EntityUid>(GetConstructionBeakers(ent.Owner));
        TransferConstructionBeakersToBuffer(ent, beakers);
    }

    private void TransferConstructionBeakersToBuffer(Entity<ChemMasterBeakerCapacityComponent> ent, IReadOnlyList<EntityUid> beakers)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out var bufferSoln, out _))
            return;

        foreach (var beaker in beakers)
        {
            if (!_solutions.TryGetFitsInDispenser(beaker, out _, out var beakerSolution)
                || beakerSolution.Volume == FixedPoint2.Zero)
                continue;

            var split = beakerSolution.SplitSolution(beakerSolution.Volume);
            _solutions.TryAddSolution(bufferSoln.Value, split);
        }
    }

    private void ReturnBufferToConstructionBeakers(Entity<ChemMasterBeakerCapacityComponent> ent)
    {
        if (!_solutions.TryGetSolution(ent.Owner, SharedChemMaster.BufferSolutionName, out _, out var buffer)
            || buffer.Volume == FixedPoint2.Zero)
        {
            return;
        }

        foreach (var beaker in GetConstructionBeakers(ent.Owner))
        {
            if (buffer.Volume == FixedPoint2.Zero)
                return;

            if (!_solutions.TryGetFitsInDispenser(beaker, out var beakerSoln, out var beakerSolution))
                continue;

            var canFit = beakerSolution.AvailableVolume;
            if (canFit <= FixedPoint2.Zero)
                continue;

            var toTransfer = FixedPoint2.Min(canFit, buffer.Volume);
            _solutions.TryAddSolution(beakerSoln.Value, buffer.SplitSolution(toTransfer));
        }

        if (buffer.Volume > FixedPoint2.Zero)
        {
            var coords = Transform(ent.Owner).Coordinates;
            _puddle.TrySpillAt(coords, buffer.SplitSolution(buffer.Volume), out _);
        }
    }
}
