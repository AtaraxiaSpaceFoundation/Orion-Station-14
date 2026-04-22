using System.Linq;
using System.Numerics;
using Content.Server._Orion.Bitrunning.Components;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage;
using Content.Shared._Orion.Bitrunning;
using Content.Shared._Orion.Bitrunning.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Bitrunning.Systems;

public sealed class BitrunningDiskSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvatarConnectionComponent, ComponentStartup>(OnAvatarStartup);

        SubscribeLocalEvent<BitrunningAbilityDiskComponent, EntInsertedIntoContainerMessage>(OnDiskInsertedIntoContainer);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, EntRemovedFromContainerMessage>(OnDiskRemovedFromContainer);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, ExaminedEvent>(OnDiskExamined);
        SubscribeLocalEvent<BitrunningAbilityDiskComponent, UseInHandEvent>(OnDiskUseInHand);

        Subs.BuiEvents<BitrunningAbilityDiskComponent>(BitrunningDiskUiKey.Key,
            subs =>
        {
            subs.Event<BitrunningDiskSelectOptionMessage>(OnDiskOptionSelected);
        });

        SubscribeLocalEvent<BitrunningSpawnCheeseActionEvent>(OnSpawnCheeseAction);
        SubscribeLocalEvent<BitrunningLesserHealActionEvent>(OnLesserHealAction);
    }

    public void RefreshAvatarEffects(EntityUid avatarUid)
    {
        if (TryComp<AvatarConnectionComponent>(avatarUid, out var avatarConnection))
            UpdateAvatarEffects((avatarUid, avatarConnection));
    }

    private void OnAvatarStartup(Entity<AvatarConnectionComponent> ent, ref ComponentStartup args)
    {
        UpdateAvatarEffects(ent);
    }

    private void OnDiskInsertedIntoContainer(Entity<BitrunningAbilityDiskComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (TryFindAvatarOwner(ent.Owner, out var avatarUid, out var avatarComp))
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskRemovedFromContainer(Entity<BitrunningAbilityDiskComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (TryFindAvatarOwner(ent.Owner, out var avatarUid, out var avatarComp))
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskUseInHand(Entity<BitrunningAbilityDiskComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (ent.Comp.SelectedOption != null)
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-already-selected", ("option", ent.Comp.SelectedOption)), ent, args.User, PopupType.SmallCaution);
            return;
        }

        _ui.TryOpenUi(ent.Owner, BitrunningDiskUiKey.Key, args.User);
        _ui.SetUiState(ent.Owner, BitrunningDiskUiKey.Key, new BitrunningDiskBoundUiState(ent.Comp.Options.Keys.ToList(), null));
    }

    private void OnDiskOptionSelected(Entity<BitrunningAbilityDiskComponent> ent, ref BitrunningDiskSelectOptionMessage args)
    {
        var user = args.Actor;
        if (ent.Comp.SelectedOption != null || !ent.Comp.Options.ContainsKey(args.Option))
            return;

        var foundAvatar = TryFindAvatarOwner(ent.Owner, out var avatarUid, out var avatarComp);
        if (foundAvatar && !IsDiskModificationAllowed(avatarComp))
        {
            _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-modifications-blocked"), ent, user, PopupType.SmallCaution);
            return;
        }

        ent.Comp.SelectedOption = args.Option;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("bitrunning-disk-popup-selected", ("option", LocalizeOption(args.Option))), ent, user, PopupType.Medium);
        _ui.SetUiState(ent.Owner, BitrunningDiskUiKey.Key, new BitrunningDiskBoundUiState(ent.Comp.Options.Keys.ToList(), ent.Comp.SelectedOption));

        if (foundAvatar)
            UpdateAvatarEffects((avatarUid, avatarComp));
    }

    private void OnDiskExamined(Entity<BitrunningAbilityDiskComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.SelectedOption == null)
            args.PushMarkup(Loc.GetString("bitrunning-disk-examine-unselected"));
        else
        {
            args.PushMarkup(Loc.GetString("bitrunning-disk-examine-selected",
                ("option", LocalizeOption(ent.Comp.SelectedOption))));
        }
    }

    private void UpdateAvatarEffects(Entity<AvatarConnectionComponent> avatar)
    {
        var holder = EnsureComp<BitrunningAvatarAbilityHolderComponent>(avatar);

        if (!IsDiskModificationAllowed(avatar.Comp) || avatar.Comp.OriginalBody is not { } bitrunnerUid)
        {
            RemoveAllGrantedActions(holder);
            return;
        }

        var selectedActionDisks = FindSelectedDisks(bitrunnerUid, BitrunningDiskGrantMode.Action);

        foreach (var (diskUid, actionUid) in holder.ActionsByDisk.ToArray())
        {
            if (selectedActionDisks.ContainsKey(diskUid))
                continue;

            _actions.RemoveAction(actionUid);
            holder.ActionsByDisk.Remove(diskUid);
        }

        foreach (var (diskUid, actionProto) in selectedActionDisks)
        {
            if (holder.ActionsByDisk.ContainsKey(diskUid))
                continue;

            EntityUid? actionUid = null;
            _actions.AddAction(avatar.Owner, ref actionUid, actionProto, avatar.Owner);
            holder.ActionsByDisk[diskUid] = actionUid;
        }

        var selectedItemDisks = FindSelectedDisks(bitrunnerUid, BitrunningDiskGrantMode.Item);
        TryGrantDomainItems((avatar.Owner, avatar.Comp), selectedItemDisks);
    }

    private void RemoveAllGrantedActions(BitrunningAvatarAbilityHolderComponent holder)
    {
        foreach (var actionUid in holder.ActionsByDisk.Values)
        {
            _actions.RemoveAction(actionUid);
        }

        holder.ActionsByDisk.Clear();
    }

    private Dictionary<EntityUid, EntProtoId> FindSelectedDisks(EntityUid bitrunnerUid, BitrunningDiskGrantMode mode)
    {
        var found = new Dictionary<EntityUid, EntProtoId>();
        var visited = new HashSet<EntityUid>();
        var queue = new Queue<EntityUid>();
        queue.Enqueue(bitrunnerUid);

        while (queue.TryDequeue(out var current))
        {
            if (!visited.Add(current))
                continue;

            if (TryComp<BitrunningAbilityDiskComponent>(current, out var disk) &&
                disk.GrantMode == mode &&
                disk.SelectedOption is { } selected &&
                disk.Options.TryGetValue(selected, out var prototype))
            {
                found[current] = prototype;
            }

            if (!TryComp<ContainerManagerComponent>(current, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    queue.Enqueue(contained);
                }
            }
        }

        return found;
    }

    private void TryGrantDomainItems(Entity<AvatarConnectionComponent> avatar, Dictionary<EntityUid, EntProtoId> selectedItemDisks)
    {
        if (avatar.Comp.Server is not { } serverUid || !TryComp<QuantumServerComponent>(serverUid, out var server))
            return;

        foreach (var (diskUid, itemProto) in selectedItemDisks)
        {
            if (server.GrantedItemDisks.Contains(diskUid))
                continue;

            var spawned = Spawn(itemProto, Transform(avatar.Owner).Coordinates);
            if (TryInsertIntoInventoryOrHands(avatar.Owner, spawned))
            {
                server.GrantedItemDisks.Add(diskUid);
                continue;
            }

            QueueDel(spawned);
        }

        Dirty(serverUid, server);
    }

    private bool TryInsertIntoInventoryOrHands(EntityUid avatarUid, EntityUid itemUid)
    {
        if (_hands.TryPickupAnyHand(avatarUid, itemUid, checkActionBlocker: false))
            return true;

        if (!_inventory.TryGetSlotEntity(avatarUid, "back", out var backUid))
            return false;

        if (TryComp<StorageComponent>(backUid.Value, out var storage) && _storage.Insert(backUid.Value, itemUid, out _, storageComp: storage, playSound: false))
            return true;

        return false;
    }

    private bool IsDiskModificationAllowed(AvatarConnectionComponent avatarConnection)
    {
        if (avatarConnection.Server is not { } serverUid || !TryComp<QuantumServerComponent>(serverUid, out var server))
            return true;

        return server.AllowDiskModifications;
    }

    private bool TryFindAvatarOwner(EntityUid entity, out EntityUid avatarUid, out AvatarConnectionComponent avatarComp)
    {
        avatarUid = default;
        avatarComp = default!;

        var current = entity;
        while (TryComp<TransformComponent>(current, out var xform))
        {
            if (TryFindAvatarByOriginalBody(current, out avatarUid, out avatarComp))
                return true;

            var parent = xform.ParentUid;
            if (parent == EntityUid.Invalid || parent == current)
                break;

            current = parent;
        }

        return false;
    }

    private bool TryFindAvatarByOriginalBody(EntityUid bodyUid, out EntityUid avatarUid, out AvatarConnectionComponent avatarComp)
    {
        var query = EntityQueryEnumerator<AvatarConnectionComponent>();
        while (query.MoveNext(out var uid, out var connection))
        {
            if (connection.OriginalBody != bodyUid)
                continue;

            avatarUid = uid;
            avatarComp = connection;
            return true;
        }

        avatarUid = default;
        avatarComp = default!;
        return false;
    }

    private void OnSpawnCheeseAction(BitrunningSpawnCheeseActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var origin = Transform(args.Performer).Coordinates;
        var radius = Math.Clamp(args.Radius, 0, 8);
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var spawnCoords = new EntityCoordinates(origin.EntityId, origin.Position + new Vector2(x, y));
                Spawn(args.PrototypeId, spawnCoords);
            }
        }
    }

    private void OnLesserHealAction(BitrunningLesserHealActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var heal = new DamageSpecifier();
        heal.DamageDict["Blunt"] = -12f;
        heal.DamageDict["Heat"] = -12f;
        _damageable.TryChangeDamage(args.Performer, heal, ignoreResistances: true);
    }

    private string LocalizeOption(string optionKey)
    {
        return _loc.TryGetString(optionKey, out var localizedOption)
            ? localizedOption
            : optionKey;
    }
}
