using System.Numerics;
using Content.Shared._Orion.Bed.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tag;

namespace Content.Shared._Orion.Bed.Systems;

public sealed class DoubleBedSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoubleBedComponent, ComponentStartup>(OnDoubleBedStartup);
        SubscribeLocalEvent<DoubleBedComponent, StrapAttemptEvent>(OnStrapAttempt, before: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<DoubleBedComponent, UnstrappedEvent>(OnUnstrapped, after: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<DoubleBedComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: new[] { typeof(PlaceableSurfaceSystem) });
        SubscribeLocalEvent<DoubleBedComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(Entity<DoubleBedComponent> ent, ref InteractHandEvent args)
    {
        var query = EntityQueryEnumerator<DoubleBedSheetComponent, TransformComponent>();
        while (query.MoveNext(out var bedsheetUid, out _, out var bedsheetXform))
        {
            if (bedsheetXform.ParentUid != ent.Owner)
                continue;

            args.Handled = true;
            _transform.SetCoordinates(bedsheetUid, Transform(args.User).Coordinates);
            return;
        }
    }

    private void OnDoubleBedStartup(Entity<DoubleBedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        if (strap.BuckleOffset != Vector2.Zero)
            return;

        strap.BuckleOffset = ent.Comp.LeftOffset;
        Dirty(ent, strap);
    }

    private void OnStrapAttempt(Entity<DoubleBedComponent> ent, ref StrapAttemptEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        var offset = strap.BuckledEntities.Count == 0 ? ent.Comp.LeftOffset : ent.Comp.RightOffset;
        strap.BuckleOffsets[args.Buckle.Owner] = offset;
        strap.BuckleOffset = offset;
        Dirty(ent, strap);
    }

    private void OnUnstrapped(Entity<DoubleBedComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        strap.BuckleOffsets.Remove(args.Buckle.Owner);

        strap.BuckleOffset = ent.Comp.LeftOffset;
        foreach (var buckledEntity in strap.BuckledEntities)
        {
            strap.BuckleOffset = strap.BuckleOffsets.GetValueOrDefault(buckledEntity, ent.Comp.LeftOffset);
            break;
        }

        Dirty(ent, strap);
    }

    private void OnAfterInteractUsing(Entity<DoubleBedComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<PlaceableSurfaceComponent>(ent, out var surface))
            return;

        const string bedsheetTag = "Bedsheet";
        if (!_tagSystem.HasTag(args.Used, bedsheetTag))
            return;

        var isDoubleBedsheet = HasComp<DoubleBedSheetComponent>(args.Used);

        var bedCoords = Transform(ent).Coordinates;
        var bedsheetCount = 0;
        var hasDoubleBedsheet = false;

        var doubleBedsheetQuery = EntityQueryEnumerator<DoubleBedSheetComponent, TransformComponent>();
        while (doubleBedsheetQuery.MoveNext(out _, out _, out var doubleBedsheetXform))
        {
            if (doubleBedsheetXform.ParentUid != ent.Owner)
                continue;

            hasDoubleBedsheet = true;
            break;
        }

        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (uid == args.Used)
                continue;

            if (!_tagSystem.HasTag(uid, bedsheetTag))
                continue;

            var bedsheetCoords = xform.Coordinates;
            if (bedsheetCoords.TryDistance(EntityManager, bedCoords, out var distance) && distance < 0.5f)
                bedsheetCount++;
        }

        if (isDoubleBedsheet || hasDoubleBedsheet)
            return;

        var offset = bedsheetCount == 0
            ? ent.Comp.RightBedsheetOffset
            : ent.Comp.LeftBedsheetOffset;

        _placeableSurface.SetPositionOffset(ent, offset, surface);
    }
}
