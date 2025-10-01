using Content.Shared._Europa.Donate;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Europa.Donate;

public sealed class DonateItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inv = default!;
    [Dependency] private readonly ItemSlotsSystem _item = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private IEnumerable<DonateItemPrototype> _donateItems = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("donate-item-system");
        _donateItems = _prototypeManager.EnumeratePrototypes<DonateItemPrototype>();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        var playerCkey = ev.Player.Name;
        if (playerCkey.StartsWith("localhost@"))
            playerCkey = playerCkey.Substring("localhost@".Length);

        var playerEntity = ev.Player.AttachedEntity;
        if (playerEntity == null)
            return;

        if (HasComp<GhostComponent>(playerEntity.Value))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(playerEntity.Value))
            return;

        foreach (var donateItem in _donateItems)
        {
            if (playerCkey != donateItem.Ckey)
                continue;

            if (!_prototypeManager.TryIndex(donateItem.Item, out var proto))
                continue;

            if (donateItem.IsBattery)
            {
                if (!TryComp<PowerCellSlotComponent>(playerEntity.Value, out var cellComp))
                    continue;

                var cell = _item.GetItemOrNull(playerEntity.Value, cellComp.CellSlotId);

                if (cell != null)
                    Del(cell);

                if (!SpawnInContainerOrDrop(donateItem.Item, playerEntity.Value, cellComp.CellSlotId).Valid)
                    _sawmill.Error(Loc.GetString("donate-system-is-shit"));

            }
            else
                _inv.SpawnItemOnEntity(playerEntity.Value, proto);
        }
    }
}
