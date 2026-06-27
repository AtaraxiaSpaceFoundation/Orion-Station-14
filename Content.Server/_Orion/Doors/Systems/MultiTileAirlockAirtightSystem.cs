using Content.Server._Orion.Doors.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._Orion.Doors.Components;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Server._Orion.Doors.Systems;

public sealed class MultiTileAirlockAirtightSystem : EntitySystem
{
    [Dependency] private readonly AirtightSystem _airtight = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private const string PhantomPrototype = "PhantomAirtight";

    public override void Initialize()
    {
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, MoveEvent>(OnMoved);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, AirtightChanged>(OnAirtightChanged);
        SubscribeLocalEvent<MultiTileAirlockAirtightComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAnchorChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            SpawnPhantoms(ent);
        else
            DeletePhantoms(ent);
    }

    private void OnMoved(Entity<MultiTileAirlockAirtightComponent> ent, ref MoveEvent args)
    {
        DeletePhantoms(ent);
        if (Transform(ent).Anchored)
            SpawnPhantoms(ent);
    }

    private void OnAirtightChanged(Entity<MultiTileAirlockAirtightComponent> ent, ref AirtightChanged args)
    {
        if (!TryComp<AirtightComponent>(ent, out var parentAirtight))
            return;

        foreach (var phantom in ent.Comp.PhantomEntities)
        {
            if (!TryComp<AirtightComponent>(phantom, out var phantomAirtight))
                continue;

            _airtight.SetAirblocked((phantom, phantomAirtight), parentAirtight.AirBlocked);
        }
    }

    private void OnShutdown(Entity<MultiTileAirlockAirtightComponent> ent, ref ComponentShutdown args)
    {
        DeletePhantoms(ent);
    }

    private void SpawnPhantoms(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        var xform = Transform(ent);
        if (!xform.Anchored || xform.GridUid == null)
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        if (!TryComp<AirtightComponent>(ent, out var parentAirtight))
            return;

        var anchorTile = _mapSystem.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);

        foreach (var offset in ent.Comp.TileOffsets)
        {
            var rotated = xform.LocalRotation.RotateVec(new Vector2(offset.X, offset.Y));
            var snapped = new Vector2i(
                (int) MathF.Round(rotated.X),
                (int) MathF.Round(rotated.Y));

            var phantomTile = anchorTile + snapped;
            var phantomCoords = _mapSystem.GridTileToLocal(xform.GridUid.Value, grid, phantomTile);
            var phantom = EntityManager.SpawnEntity(PhantomPrototype, phantomCoords);

            var parentComp = EnsureComp<PhantomAirtightParentComponent>(phantom);
            parentComp.ParentUid = GetNetEntity(ent.Owner);

            if (TryComp<AirtightComponent>(phantom, out var phantomAirtight))
                _airtight.SetAirblocked((phantom, phantomAirtight), parentAirtight.AirBlocked);

            ent.Comp.PhantomEntities.Add(phantom);
        }
    }

    private void DeletePhantoms(Entity<MultiTileAirlockAirtightComponent> ent)
    {
        foreach (var phantom in ent.Comp.PhantomEntities)
        {
            if (!TerminatingOrDeleted(phantom))
                QueueDel(phantom);
        }
        ent.Comp.PhantomEntities.Clear();
    }
}
