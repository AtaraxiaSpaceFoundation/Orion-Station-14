using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class BitrunningObjectiveSystem : EntitySystem
{
    [Dependency] private readonly QuantumServerSystem _server = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BitrunningExitMarkerComponent, StartCollideEvent>(OnExitCollide);
        SubscribeLocalEvent<BitrunningObjectivePointComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<BitrunningObjectiveDeliveryPointComponent, StartCollideEvent>(OnDeliveryCollide);
        SubscribeLocalEvent<BitrunningDomainEnemyObjectiveComponent, MobStateChangedEvent>(OnEnemyStateChanged);
    }

    private void OnExitCollide(Entity<BitrunningExitMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<AvatarConnectionComponent>(args.OtherEntity))
            return;

        if (!TryResolveDomainMapUid(ent.Owner, args.OtherEntity, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out _, out _))
            return;

        _server.DisconnectAvatar(args.OtherEntity, false);
    }

    private void OnInteract(Entity<BitrunningObjectivePointComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryResolveDomainMapUid(ent.Owner, args.User, out var mapUid, out var coordinates))
            return;

        var query = EntityQueryEnumerator<QuantumServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            if (server.DomainMapUid != mapUid)
                continue;

            if (server.ObjectiveType != BitrunningObjectiveType.CollectEncryptedCaches)
                continue;

            _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
            _audio.PlayPvs(ent.Comp.PickupSound, coordinates);
            if (ent.Comp.ConsumeOnUse)
                QueueDel(ent.Owner);

            args.Handled = true;
            return;
        }
    }

    private void OnDeliveryCollide(Entity<BitrunningObjectiveDeliveryPointComponent> ent, ref StartCollideEvent args)
    {
        if (!TryResolveDomainMapUid(ent.Owner, args.OtherEntity, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (!HasComp<BitrunningObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (HasComp<BitrunningDeliveredObjectiveCargoComponent>(args.OtherEntity))
            return;

        if (!HasLinkedByteforge(serverUid, server))
        {
            if (TryComp<MapComponent>(mapUid, out var mapComp))
            {
                _popup.PopupEntity(Loc.GetString("bitrunning-delivery-byteforge-required"), ent, Filter.BroadcastMap(mapComp.MapId), true, PopupType.LargeCaution);
                return;
            }
        }

        if (!_server.TryDeliverObjectiveCargoToByteforge(serverUid, args.OtherEntity))
            return;

        if (server.ObjectiveType == BitrunningObjectiveType.DeliveryCacheCrate)
            _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }

    private void OnEnemyStateChanged(Entity<BitrunningDomainEnemyObjectiveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryResolveDomainMapUid(ent.Owner, null, out var mapUid))
            return;

        if (!_server.TryGetServerByDomainMap(mapUid, out var serverUid, out var server))
            return;

        if (server.ObjectiveType != BitrunningObjectiveType.EliminateEnemies)
            return;

        _server.AddObjectiveProgress(serverUid, ent.Comp.Points);
    }

    private bool TryResolveDomainMapUid(EntityUid primaryUid, EntityUid? fallbackUid, out EntityUid mapUid, out EntityCoordinates coordinates)
    {
        coordinates = default;
        if (TryComp(primaryUid, out TransformComponent? primaryXform) && primaryXform.MapUid is { } primaryMapUid)
        {
            mapUid = primaryMapUid;
            coordinates = primaryXform.Coordinates;
            return true;
        }

        if (fallbackUid != null && TryComp(fallbackUid.Value, out TransformComponent? fallbackXform) && fallbackXform.MapUid is { } fallbackMapUid)
        {
            mapUid = fallbackMapUid;
            coordinates = fallbackXform.Coordinates;
            return true;
        }

        mapUid = default;
        return false;
    }

    private bool TryResolveDomainMapUid(EntityUid primaryUid, EntityUid? fallbackUid, out EntityUid mapUid)
    {
        return TryResolveDomainMapUid(primaryUid, fallbackUid, out mapUid, out _);
    }

    private bool HasLinkedByteforge(EntityUid serverUid, QuantumServerComponent server)
    {
        if (server.LinkedByteforge is not { } byteforgeUid || !Exists(byteforgeUid))
            return false;

        return TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge) && byteforge.LinkedServer == serverUid;
    }
}
