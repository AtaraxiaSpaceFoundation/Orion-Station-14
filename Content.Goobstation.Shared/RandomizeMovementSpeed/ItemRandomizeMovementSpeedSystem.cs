// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.RandomizeMovementSpeed;

public sealed class ItemRandomizeMovementSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemRandomizeMovementspeedComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        // Orion-Start
        SubscribeLocalEvent<ItemRandomizeMovementspeedComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<ItemRandomizeMovementspeedComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ItemRandomizeMovementspeedComponent, ComponentShutdown>(OnComponentShutdown);
        // Orion-End
        SubscribeLocalEvent<ItemRandomizeMovementspeedComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeedModifiers);
    }

    private void OnGotEquippedHand(EntityUid uid, ItemRandomizeMovementspeedComponent comp, GotEquippedHandEvent args)
    {
        // Orion-Start
        if (comp.Whitelist != null && !_whitelist.IsValid(comp.Whitelist, args.User))
            return;
        // Orion-End

        comp.User = args.User;
        // Orion-Start
        comp.CurrentModifier = 1f;
        comp.TargetModifier = _random.NextFloat(comp.Min, comp.Max);
        comp.NextExecutionTime = _timing.CurTime + comp.ExecutionInterval;
        Dirty(uid, comp);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
        // Orion-End
    }

    // Orion-Start
    private void OnGotUnequippedHand(EntityUid uid, ItemRandomizeMovementspeedComponent comp, GotUnequippedHandEvent args)
    {
        ClearUser(uid, comp, args.User);
    }

    private void OnDropped(EntityUid uid, ItemRandomizeMovementspeedComponent comp, DroppedEvent args)
    {
        ClearUser(uid, comp, args.User);
    }

    private void OnComponentShutdown(EntityUid uid, ItemRandomizeMovementspeedComponent comp, ComponentShutdown args)
    {
        if (comp.User is { } user)
            ClearUser(uid, comp, user);
    }
    // Orion-End

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, ItemRandomizeMovementspeedComponent comp, ref HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        // Orion-Start
        if (comp.User is not { } user || !CanAffectUser(uid, comp, user))
            return;
        // Orion-End

        args.Args.ModifySpeed(comp.CurrentModifier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ItemRandomizeMovementspeedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Orion-Edit-Start
            if (comp.User is not { } user)
                continue;

            if (!CanAffectUser(uid, comp, user))
            {
                ClearUser(uid, comp, user);
                continue;
            }

            var previous = comp.CurrentModifier;
            comp.CurrentModifier = MathHelper.Lerp(comp.CurrentModifier, comp.TargetModifier, frameTime / comp.SmoothingTime);

            if (!MathHelper.CloseToPercent(previous, comp.CurrentModifier))
            {
                Dirty(uid, comp);
                _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
            }

            if (_timing.CurTime < comp.NextExecutionTime)
                continue;

            comp.TargetModifier = _random.NextFloat(comp.Min, comp.Max);
            comp.NextExecutionTime = _timing.CurTime + comp.ExecutionInterval;
            Dirty(uid, comp);
            // Orion-Edit-End
        }
    }

    // Orion-Start
    private bool CanAffectUser(EntityUid uid, ItemRandomizeMovementspeedComponent comp, EntityUid user)
    {
        if (!_hands.IsHolding(user, uid))
            return false;

        return comp.Whitelist == null || _whitelist.IsValid(comp.Whitelist, user);
    }

    private void ClearUser(EntityUid uid, ItemRandomizeMovementspeedComponent comp, EntityUid user)
    {
        comp.User = null;
        comp.CurrentModifier = 1f;
        comp.TargetModifier = 1f;
        Dirty(uid, comp);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }
    // Orion-End
}
