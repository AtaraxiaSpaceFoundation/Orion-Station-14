using Content.Server.Storage.EntitySystems;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class ByteforgeSystem : EntitySystem
{
    [Dependency] private readonly BitrunningDomainSystem _domains = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string ServerSourcePort = "BitrunningServerSource";
    private const string ByteforgeSinkPort = "BitrunningByteforgeSink";

    public override void Initialize()
    {
        SubscribeLocalEvent<ByteforgeComponent, MapInitEvent>(OnByteforgeMapInit);
        SubscribeLocalEvent<ByteforgeComponent, PowerChangedEvent>(OnByteforgePowerChanged);
        SubscribeLocalEvent<QuantumServerComponent, NewLinkEvent>(OnServerNewLink);
        SubscribeLocalEvent<QuantumServerComponent, PortDisconnectedEvent>(OnServerPortDisconnected);
        SubscribeLocalEvent<QuantumServerComponent, GotEmaggedEvent>(OnServerEmagged);
    }

    private void OnByteforgeMapInit(Entity<ByteforgeComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, BitrunningVisuals.ByteforgePowered, _power.IsPowered(ent.Owner));
        _appearance.SetData(ent, BitrunningVisuals.ByteforgeActive, false);
        _appearance.SetData(ent, BitrunningVisuals.ByteforgeAngry, IsLinkedServerEmagged(ent.Comp));
    }

    private void OnByteforgePowerChanged(Entity<ByteforgeComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, BitrunningVisuals.ByteforgePowered, args.Powered);
    }

    private void OnServerEmagged(Entity<QuantumServerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        UpdateByteforgeEmagVisual(ent.Comp);
    }

    private void OnServerNewLink(Entity<QuantumServerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != ServerSourcePort || args.SinkPort != ByteforgeSinkPort)
            return;

        if (!TryComp<ByteforgeComponent>(args.Sink, out var byteforge))
            return;

        if (ent.Comp.LinkedByteforge is { } oldByteforge && oldByteforge != args.Sink && TryComp<ByteforgeComponent>(oldByteforge, out var oldByteforgeComp))
        {
            oldByteforgeComp.LinkedServer = null;
            _appearance.SetData(oldByteforge, BitrunningVisuals.ByteforgeAngry, false);
            Dirty(oldByteforge, oldByteforgeComp);
        }

        ent.Comp.LinkedByteforge = args.Sink;
        byteforge.LinkedServer = ent.Owner;
        UpdateByteforgeEmagVisual(ent.Comp);
        Dirty(ent);
        Dirty(args.Sink, byteforge);
    }

    private void OnServerPortDisconnected(Entity<QuantumServerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ServerSourcePort || ent.Comp.LinkedByteforge != args.RemovedPortUid)
            return;

        if (TryComp<ByteforgeComponent>(args.RemovedPortUid, out var byteforge))
        {
            byteforge.LinkedServer = null;
            Dirty(args.RemovedPortUid, byteforge);
        }

        if (ent.Comp.LinkedByteforge is { } oldLinked && Exists(oldLinked))
            _appearance.SetData(oldLinked, BitrunningVisuals.ByteforgeAngry, false);

        ent.Comp.LinkedByteforge = null;
        _appearance.SetData(args.RemovedPortUid, BitrunningVisuals.ByteforgeAngry, false);
        Dirty(ent);
    }

    public bool HasLinkedByteforge(EntityUid serverUid, QuantumServerComponent server)
    {
        if (server.LinkedByteforge is not { } byteforgeUid || !Exists(byteforgeUid))
            return false;

        return TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge) && byteforge.LinkedServer == serverUid;
    }

    public bool TryDeliverObjectiveCargoToByteforge(EntityUid serverUid, EntityUid cargoUid)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return false;

        if (HasComp<BitrunningDeliveredObjectiveCargoComponent>(cargoUid))
            return false;

        if (!HasLinkedByteforge(serverUid, server))
            return false;

        var byteforgeUid = server.LinkedByteforge!.Value;
        if (!TryComp<TransformComponent>(byteforgeUid, out var byteforgeXform))
            return false;

        if (!_prototype.HasIndex<EntityPrototype>(server.RewardCachePrototype))
        {
            Log.Warning($"Invalid reward cache prototype '{server.RewardCachePrototype}' on server {ToPrettyString(serverUid)}.");
            return false;
        }

        var rewardCargoUid = Spawn(server.RewardCachePrototype, byteforgeXform.Coordinates);
        if (!FillDeliveredCargoWithLoot(rewardCargoUid, server))
        {
            Log.Warning($"Failed to fill delivered cargo reward crate for server {ToPrettyString(serverUid)}.");
            QueueDel(rewardCargoUid);
            return false;
        }

        EnsureComp<BitrunningDeliveredObjectiveCargoComponent>(cargoUid);
        PulseByteforge(byteforgeUid);
        QueueDel(cargoUid);
        return true;
    }

    private void PulseByteforge(EntityUid byteforgeUid)
    {
        if (!TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge))
            return;

        byteforge.VisualPulseSerial++;
        var pulseSerial = byteforge.VisualPulseSerial;

        _appearance.SetData(byteforgeUid, BitrunningVisuals.ByteforgeAngry, IsLinkedServerEmagged(byteforge));
        _appearance.SetData(byteforgeUid, BitrunningVisuals.ByteforgeActive, true);

        Timer.Spawn(TimeSpan.FromSeconds(1.4f),
            () =>
        {
            if (!TryComp<ByteforgeComponent>(byteforgeUid, out var byteforgeComp) || byteforgeComp.VisualPulseSerial != pulseSerial)
                return;

            _appearance.SetData(byteforgeUid, BitrunningVisuals.ByteforgeActive, false);
        });
    }

    private bool IsLinkedServerEmagged(ByteforgeComponent byteforge)
    {
        return byteforge.LinkedServer is { } serverUid && HasComp<EmaggedComponent>(serverUid);
    }

    private void UpdateByteforgeEmagVisual(QuantumServerComponent server)
    {
        if (server.LinkedByteforge is not { } byteforgeUid || !Exists(byteforgeUid) || !TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge))
            return;

        _appearance.SetData(byteforgeUid, BitrunningVisuals.ByteforgeAngry, IsLinkedServerEmagged(byteforge));
    }

    public void RefreshLinkedByteforge(Entity<QuantumServerComponent> ent)
    {
        ent.Comp.LinkedByteforge = null;

        if (!TryComp<DeviceLinkSourceComponent>(ent.Owner, out var source))
        {
            Dirty(ent);
            return;
        }

        foreach (var outputs in source.Outputs.Values)
        {
            foreach (var linkedEntity in outputs)
            {
                if (!TryComp<ByteforgeComponent>(linkedEntity, out var byteforge))
                    continue;

                ent.Comp.LinkedByteforge = linkedEntity;
                byteforge.LinkedServer = ent.Owner;
                UpdateByteforgeEmagVisual(ent.Comp);
                Dirty(linkedEntity, byteforge);
                Dirty(ent);
                return;
            }
        }

        Dirty(ent);
    }

    private bool FillDeliveredCargoWithLoot(EntityUid cargoUid, QuantumServerComponent server)
    {
        var tableId = GetDeliveryLootTable(server);
        if (!_prototype.TryIndex(tableId, out var table))
            return false;

        var coordinates = Transform(cargoUid).Coordinates;
        foreach (var prototypeId in _entityTable.GetSpawns(table))
        {
            var loot = Spawn(prototypeId, coordinates);

            if (TryComp<StorageComponent>(cargoUid, out var storage) &&
                _storage.Insert(cargoUid, loot, out _, storageComp: storage, playSound: false))
                continue;

            if (TryComp<EntityStorageComponent>(cargoUid, out var entityStorage) &&
                _entityStorage.Insert(loot, cargoUid, entityStorage))
                continue;

            QueueDel(loot);
        }

        return true;
    }

    private ProtoId<EntityTablePrototype> GetDeliveryLootTable(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return server.DeliveryEasyLootTable;

        return domain.Difficulty switch
        {
            BitrunningDifficulty.Easy => server.DeliveryEasyLootTable,
            BitrunningDifficulty.Medium => server.DeliveryMediumLootTable,
            BitrunningDifficulty.Hard => server.DeliveryHardLootTable,
            BitrunningDifficulty.Extreme => server.DeliveryExtremeLootTable,
            _ => server.DeliveryEasyLootTable,
        };
    }
}
