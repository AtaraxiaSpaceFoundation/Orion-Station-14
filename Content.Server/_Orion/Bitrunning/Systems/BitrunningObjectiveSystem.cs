using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class BitrunningObjectiveSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BitrunningObjectivePointComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<BitrunningObjectiveDeliveryPointComponent, StartCollideEvent>(OnDeliveryCollide);
        SubscribeLocalEvent<BitrunningDomainEnemyObjectiveComponent, MobStateChangedEvent>(OnEnemyStateChanged);
    }

    private void OnInteract(Entity<BitrunningObjectivePointComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var mapUid = Transform(ent).MapUid;
        if (mapUid == null)
            return;

        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            if (server.DomainMapUid != mapUid)
                continue;

            if (server.ObjectiveType != BitrunningObjectiveType.CollectEncryptedCaches)
                continue;

            _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
            _audio.PlayPvs(ent.Comp.PickupSound, Transform(ent).Coordinates);
            if (ent.Comp.ConsumeOnUse)
                QueueDel(ent.Owner);

            args.Handled = true;
            return;
        }
    }

    private void OnDeliveryCollide(Entity<BitrunningObjectiveDeliveryPointComponent> ent, ref StartCollideEvent args)
    {
        var mapUid = Transform(ent).MapUid;
        if (mapUid == null)
            return;

        if (!_server.TryGetServerByDomainMap(mapUid.Value, out var serverUid, out var server))
            return;

        if (server.ObjectiveType != BitrunningObjectiveType.DeliveryCacheCrate)
            return;

        if (!HasComp<BitrunningObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (HasComp<BitrunningDeliveredObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (!_server.TryDeliverObjectiveCargoToByteforge(serverUid, args.OtherEntity))
            return;

        _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }

    private void OnEnemyStateChanged(Entity<BitrunningDomainEnemyObjectiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var mapUid = Transform(ent).MapUid;
        if (mapUid == null)
            return;

        if (!_server.TryGetServerByDomainMap(mapUid.Value, out var serverUid, out var server))
            return;

        if (server.ObjectiveType != BitrunningObjectiveType.EliminateEnemies)
            return;

        _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }
}
