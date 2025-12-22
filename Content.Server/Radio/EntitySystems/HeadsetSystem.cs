// SPDX-FileCopyrightText: 2023 AJCM <AJCM@tutanota.com>
// SPDX-FileCopyrightText: 2023 AlexMorgan3817 <46600554+AlexMorgan3817@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Slava0135 <40753025+Slava0135@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 LordCarve <27449516+LordCarve@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 John Willis <143434770+CerberusWolfie@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Server._EinsteinEngines.Language;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed class HeadsetSystem : SharedHeadsetSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; // Goobstation
    [Dependency] private readonly InventorySystem _inventory = default!; // Orion

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);

//        SubscribeLocalEvent<WearingHeadsetComponent, EntitySpokeEvent>(OnSpeak); // Orion-Edit: Removed
        // Orion-Start
        SubscribeLocalEvent<ActorComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<InventoryComponent, ExaminedEvent>(OnInventoryExamined);
        // Orion-End
        SubscribeLocalEvent<HeadsetComponent, RadioReceiveAttemptEvent>(OnHeadsetReceiveAttempt); // Goobstation - Whitelisted radio channel

        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateRadioChannels(uid, component, args.Component);
    }

    private void UpdateRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add ActiveRadioComponent when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<ActiveRadioComponent>(uid);
        else
            EnsureComp<ActiveRadioComponent>(uid).Channels = new(keyHolder.Channels);
    }

/* // Orion-Edit: Removed
    private void OnSpeak(EntityUid uid, WearingHeadsetComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && TryComp(component.Headset, out EncryptionKeyHolderComponent? keys)
            && keys.Channels.Contains(args.Channel.ID)
            && _whitelist.IsWhitelistPassOrNull(args.Channel.SendWhitelist, uid)) // Goobstation - Whitelisted channels
        {
            _radio.SendRadioMessage(uid, args.Message, args.Channel, component.Headset);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }
*/

    // Orion-Start
    private void OnInventoryExamined(EntityUid uid, InventoryComponent component, ExaminedEvent args)
    {
        if (!_inventory.TryGetSlotEntity(uid, "ears", out var leftEar) ||
            !_inventory.TryGetSlotEntity(uid, "earsright", out var rightEar))
            return;

        if (!HasComp<HeadsetComponent>(leftEar) || !HasComp<HeadsetComponent>(rightEar))
            return;

        var entityName = MetaData(uid).EntityName;
        args.PushMarkup(Loc.GetString("examine-headset-double-wearing", ("entityName", entityName)));
    }
    // Orion-End

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        // Orion-Edit-Start
        UpdateWearingHeadsetComponent(args.Equipee);
        if (component.IsEquipped)
            UpdateRadioChannels(uid, component);
        // Orion-Edit-End
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);
        // Orion-Edit-Start
        RemCompDeferred<ActiveRadioComponent>(uid);

        UpdateWearingHeadsetComponent(args.Equipee);
        // Orion-Edit-End
    }

    // Orion-Start
    private void UpdateWearingHeadsetComponent(EntityUid wearer)
    {
        EntityUid? newActiveHeadset = null;

        var enumerator = _inventory.GetSlotEnumerator(wearer, SlotFlags.EARS | SlotFlags.EARSRIGHT);
        while (enumerator.MoveNext(out var slot))
        {
            if (!_inventory.TryGetSlotEntity(wearer, slot.ID, out var headsetEntity) ||
                !TryComp(headsetEntity, out HeadsetComponent? headset) ||
                !headset.Enabled ||
                !headset.IsEquipped)
                continue;

            newActiveHeadset = headsetEntity;
            break;
        }

        if (newActiveHeadset != null)
        {
            if (TryComp<WearingHeadsetComponent>(wearer, out var wearing))
                wearing.Headset = newActiveHeadset.Value;
            else
                EnsureComp<WearingHeadsetComponent>(wearer).Headset = newActiveHeadset.Value;
        }
        else
        {
            RemComp<WearingHeadsetComponent>(wearer);
        }
    }

    private void OnEntitySpoke(EntityUid uid, ActorComponent component, EntitySpokeEvent args)
    {
        if (args.Channel == null)
            return;

        var enumerator = _inventory.GetSlotEnumerator(uid, SlotFlags.EARS | SlotFlags.EARSRIGHT);
        while (enumerator.MoveNext(out var slot))
        {
            if (!_inventory.TryGetSlotEntity(uid, slot.ID, out var headsetEntity) ||
                !TryComp(headsetEntity, out HeadsetComponent? headset) ||
                !headset.Enabled ||
                !headset.IsEquipped ||
                !TryComp(headsetEntity, out EncryptionKeyHolderComponent? keys))
                continue;

            if (!keys.Channels.Contains(args.Channel.ID))
                continue;

            if (!_whitelist.IsWhitelistPassOrNull(args.Channel.SendWhitelist, uid))
                continue;

            _radio.SendRadioMessage(
                uid,
                args.Message,
                args.Channel,
                headsetEntity.Value
            );

            args.Channel = null;
            break;
        }
    }
    // Orion-End

    public void SetEnabled(EntityUid uid, bool value, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Orion-Edit-Start
        component.Enabled = value;
        Dirty(uid, component);
        // Orion-Edit-End

        if (!value)
        {
            RemCompDeferred<ActiveRadioComponent>(uid);

            // Orion-Edit-Start
            if (!component.IsEquipped)
                return;

            var parent = Transform(uid).ParentUid;
            UpdateWearingHeadsetComponent(parent);
            // Orion-Edit-End
        }
        else if (component.IsEquipped)
        {
            // Orion-Edit-Start
            var parent = Transform(uid).ParentUid;
            UpdateWearingHeadsetComponent(parent);
            UpdateRadioChannels(uid, component);
            // Orion-Edit-End
        }
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent component, ref RadioReceiveEvent args)
    {
        // Einstein Engines - Language begin
        var parent = Transform(uid).ParentUid;

        if (parent.IsValid())
        {
            var relayEvent = new HeadsetRadioReceiveRelayEvent(args);
            RaiseLocalEvent(parent, ref relayEvent);
        }

        if (TryComp(parent, out ActorComponent? actor))
        {
            var canUnderstand = _language.CanUnderstand(parent, args.Language.ID);
            var msg = new MsgChatMessage
            {
                Message = canUnderstand ? args.OriginalChatMsg : args.LanguageObfuscatedChatMsg
            };
            _netMan.ServerSendMessage(msg, actor.PlayerSession.Channel);
        }
        // Einstein Engines - Language end
    }

    private void OnEmpPulse(EntityUid uid, HeadsetComponent component, ref EmpPulseEvent args)
    {
        if (component.Enabled)
        {
            args.Affected = true;
            args.Disabled = true;
        }
    }

    // Goobstation - Whitelisted radio channel
    private void OnHeadsetReceiveAttempt(EntityUid uid, HeadsetComponent component, ref RadioReceiveAttemptEvent args)
    {
        args.Cancelled |= _whitelist.IsWhitelistFail(args.Channel.ReceiveWhitelist, uid);
    }
}
