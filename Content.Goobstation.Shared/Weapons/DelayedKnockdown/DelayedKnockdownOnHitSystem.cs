// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System; // Orion
using Content.Goobstation.Common.Weapons.DelayedKnockdown;
using Content.Goobstation.Shared.Clothing;
using Content.Server.Heretic.EntitySystems.PathSpecific;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Shitcode.Weapons.Misc;
using Content.Shared.Armor;
using Content.Shared.Heretic.Components.PathSpecific;
using Content.Shared.Inventory;
// using Content.Shared.StatusEffect; // Orion-Edit: removed
using Content.Shared.Stunnable;
using Content.Shared.Timing;
// Orion-Start: added usings for toggle and server-only guard
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Network;
// Orion-End

namespace Content.Goobstation.Shared.Weapons.DelayedKnockdown;

public sealed class DelayedKnockdownOnHitSystem : EntitySystem
{
    // [Dependency] private readonly StatusEffectsSystem _status = default!; // Orion-Edit: removed obsolete old StatusEffectsSystem
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly ChampionStanceSystem _champion = default!;
    [Dependency] private readonly INetManager _net = default!; // Orion: server-only guard dependency

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedKnockdownOnHitComponent, StaminaDamageMeleeHitEvent>(OnStaminaHit); // Orion-Edit
        SubscribeLocalEvent<DelayedKnockdownOnHitComponent, MeleeHitEvent>(OnLightHit); // Orion

        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, DelayedKnockdownAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, InventoryRelayedEvent<DelayedKnockdownAttemptEvent>>(OnInventoryAttempt); // Orion-Edit
        //    OnInventoryAttempt); // Orion-Edit: replaced
        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, ArmorExamineEvent>(OnExamine);

        SubscribeLocalEvent<ChampionStanceComponent, DelayedKnockdownAttemptEvent>(OnChampionDelayedKnockdownAttempt);
        SubscribeLocalEvent<SilverMaelstromComponent, DelayedKnockdownAttemptEvent>(OnMaelstromDelayedKnockdownAttempt);
    }

// Orion-Edit-Start: execute on server only to avoid client-side desync
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<DelayedKnockdownComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Time -= frameTime;
            if (comp.Time > 0f)
                continue;

            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(comp.KnockdownTime), comp.Refresh);
            RemCompDeferred<DelayedKnockdownComponent>(uid);
        }
    }

    private void OnChampionDelayedKnockdownAttempt(Entity<ChampionStanceComponent> ent, ref DelayedKnockdownAttemptEvent args)
    {
        if (_champion.Condition(ent))
            args.Cancel();
    }

    private void OnMaelstromDelayedKnockdownAttempt(Entity<SilverMaelstromComponent> ent, ref DelayedKnockdownAttemptEvent args)
    {
// Orion-Edit-End
        args.Cancel();
    }

    private void OnExamine(Entity<ModifyDelayedKnockdownComponent> ent, ref ArmorExamineEvent args)
    {
        var comp = ent.Comp;

        if (comp.Cancel)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-cancel-delayed-knockdown"));
            return;
        }

        if (comp.DelayDelta != 0f)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-delay",
                ("amount", MathF.Abs(comp.DelayDelta)),
                ("deltasign", MathF.Sign(comp.DelayDelta))));
        }

        if (comp.KnockdownTimeDelta != 0f)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-time",
                ("amount", MathF.Abs(comp.KnockdownTimeDelta)),
                ("deltasign", MathF.Sign(comp.KnockdownTimeDelta))));
        }
    }

    private void OnInventoryAttempt(Entity<ModifyDelayedKnockdownComponent> ent, ref InventoryRelayedEvent<DelayedKnockdownAttemptEvent> args) // Orion-Edit
//        ref InventoryRelayedEvent<DelayedKnockdownAttemptEvent> args) // Orion-Edit: replaced
    {
        OnAttempt(ent, ref args.Args);
    }

    private void OnAttempt(Entity<ModifyDelayedKnockdownComponent> ent, ref DelayedKnockdownAttemptEvent args)
    {
        var comp = ent.Comp;

        if (comp.Cancel)
        {
            args.Cancel();
            return;
        }

        args.DelayDelta += comp.DelayDelta;
        args.KnockdownTimeDelta += comp.KnockdownTimeDelta;
    }

// Orion-Edit-Start
    private void OnStaminaHit(Entity<DelayedKnockdownOnHitComponent> ent, ref StaminaDamageMeleeHitEvent args)
    {

        if (_net.IsClient) // Orion: server-only guard
            return;

        if (args.HitEntities.Count == 0)
            return;

        var (weapon, comp) = ent;

        if (!IsWeaponToggledOn(weapon)) // Orion: apply only when weapon is toggled ON
            return;

        if (comp.ApplyOnHeavyAttack && args.Direction == null)
            return;

        if (TryComp(weapon, out UseDelayComponent? useDelay)) // Orion: share use-delay across LMB/RMB
            _delay.TryResetDelay((weapon, useDelay), id: comp.UseDelay);

        foreach (var (target, _) in args.HitEntities)
            ScheduleKnockdown(target, comp);
// Orion-Edit-End
    }

// Orion-Edit-Start: Bugfix
    private void OnLightHit(Entity<DelayedKnockdownOnHitComponent> ent, ref MeleeHitEvent args)
    {

        if (_net.IsClient) // Orion: server-only guard
            return;

        if (args.HitEntities.Count == 0)
            return;

        if (args.Direction != null)
            return;

        var (weapon, comp) = ent;

        if (!IsWeaponToggledOn(weapon)) // Orion: apply only when weapon is toggled ON
            return;

        if (comp.ApplyOnHeavyAttack)
            return;

        if (TryComp(weapon, out UseDelayComponent? useDelay)) // Orion: share use-delay across LMB/RMB
            _delay.TryResetDelay((weapon, useDelay), id: comp.UseDelay);

        foreach (var target in args.HitEntities)
            ScheduleKnockdown(target, comp);
    }
// Orion-Edit-End

// Orion-Start
    private void ScheduleKnockdown(EntityUid target, DelayedKnockdownOnHitComponent comp)
    {
        var attempt = new DelayedKnockdownAttemptEvent();
        RaiseLocalEvent(target, attempt);
        if (attempt.Cancelled)
            return;

        var relayed = new InventoryRelayedEvent<DelayedKnockdownAttemptEvent>(attempt);
        RaiseLocalEvent(target, relayed);
        attempt = relayed.Args;
        if (attempt.Cancelled)
            return;

        var delayed = EnsureComp<DelayedKnockdownComponent>(target);

        var scheduledDelay = comp.Delay + attempt.DelayDelta;
        var scheduledKnock = comp.KnockdownTime + attempt.KnockdownTimeDelta;

        delayed.Time = delayed.Time <= 0f ? scheduledDelay : MathF.Min(scheduledDelay, delayed.Time);
        delayed.KnockdownTime = MathF.Max(scheduledKnock, delayed.KnockdownTime);
        delayed.Refresh &= comp.Refresh;
    }


    private bool IsWeaponToggledOn(EntityUid weapon) // Orion: helper to check toggle state via ItemToggleComponent
    {
        return !TryComp<ItemToggleComponent>(weapon, out var toggle) || toggle.Activated;
// Orion-End
    }
}
