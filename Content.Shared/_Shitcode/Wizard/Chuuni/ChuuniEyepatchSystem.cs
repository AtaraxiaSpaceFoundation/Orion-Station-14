// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Magic.Components;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._Goobstation.Wizard.Chuuni;

public sealed class ChuuniEyepatchSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;

    private readonly HashSet<Entity<ChuuniEyepatchComponent>> _activeEyepatches = new(); // Orion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChuuniEyepatchComponent, GetVerbsEvent<AlternativeVerb>>(AddFlipVerb);
        SubscribeLocalEvent<ChuuniEyepatchComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ChuuniEyepatchComponent, ComponentShutdown>(OnShutdown); // Orion
        SubscribeLocalEvent<ChuuniEyepatchComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ChuuniEyepatchComponent, InventoryRelayedEvent<GetSpellInvocationEvent>>(OnGetInvocation);
        SubscribeLocalEvent<ChuuniEyepatchComponent, InventoryRelayedEvent<GetMessageColorOverrideEvent>>(OnGetPostfix);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        // Orion-Edit-Start
        var toRemove = new List<Entity<ChuuniEyepatchComponent>>();
        foreach (var (uid, eyepatch) in _activeEyepatches)
        {
            if (TerminatingOrDeleted(uid))
            {
                toRemove.Add((uid, eyepatch));
                continue;
            }

            eyepatch.Accumulator += frameTime;

            if (!(eyepatch.Accumulator >= eyepatch.Delay))
                continue;

            eyepatch.Accumulator = eyepatch.Delay;
            Dirty(uid, eyepatch);
            toRemove.Add((uid, eyepatch));
        }

        foreach (var ent in toRemove)
        {
            _activeEyepatches.Remove(ent);
        }
        // Orion-Edit-End
    }

    private void OnGetPostfix(Entity<ChuuniEyepatchComponent> ent,
        ref InventoryRelayedEvent<GetMessageColorOverrideEvent> args)
    {
        args.Args.Color = ent.Comp.Color;
    }

    private void OnGetInvocation(Entity<ChuuniEyepatchComponent> ent,
        ref InventoryRelayedEvent<GetSpellInvocationEvent> args)
    {
        args.Args.Invocation = ent.Comp.Invocations[args.Args.School];

        var performer = args.Args.Performer;
        if (!TryComp(performer, out DamageableComponent? damageable))
            return;

        if (!ent.Comp.CanHeal || damageable.TotalDamage <= FixedPoint2.Zero)
            return;

        // Orion-Edit-Start
        if (ent.Comp.Accumulator != 0f)
        {
            ent.Comp.Accumulator = 0f;
            Dirty(ent);
        }
        // Orion-Edit-End

        _activeEyepatches.Add(ent); // Orion

        if (ent.Comp.HealAmount < damageable.TotalDamage)
            args.Args.ToHeal = damageable.Damage * ent.Comp.HealAmount / damageable.TotalDamage;
        else
            args.Args.ToHeal = damageable.Damage;
    }

    private void OnExamine(Entity<ChuuniEyepatchComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.SelectedBackstory != null)
            args.PushMarkup(Loc.GetString(ent.Comp.SelectedBackstory.Value));
    }

    private void OnInit(Entity<ChuuniEyepatchComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;

        _appearance.SetData(uid, FlippedVisuals.Flipped, comp.IsFliped);
        _clothing.SetEquippedPrefix(uid, comp.IsFliped ? comp.FlippedPrefix : null);

        if (_net.IsClient || comp.Backstories.Count == 0)
            return;

        comp.SelectedBackstory = _random.Pick(comp.Backstories);
        Dirty(uid, comp);
    }

    // Orion-Start
    private void OnShutdown(Entity<ChuuniEyepatchComponent> ent, ref ComponentShutdown args)
    {
        _activeEyepatches.Remove(ent);
    }
    // Orion-End

    private void AddFlipVerb(Entity<ChuuniEyepatchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var (uid, comp) = ent;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                comp.IsFliped = !comp.IsFliped;
                // Orion-Edit-Start
                var prefix = comp.IsFliped ? comp.FlippedPrefix : null;
                _clothing.SetEquippedPrefix(uid, prefix);
                // Orion-Edit-End
                _appearance.SetData(uid, FlippedVisuals.Flipped, comp.IsFliped);
                Dirty(ent);
            },
            Text = Loc.GetString("flippable-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Priority = 1,
        };

        args.Verbs.Add(verb);
    }
}

public sealed class GetSpellInvocationEvent(MagicSchool school, EntityUid performer)
    : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES;

    public MagicSchool School = school;

    public EntityUid Performer = performer;

    public DamageSpecifier ToHeal = new();

    public LocId? Invocation;
}

public sealed class GetMessageColorOverrideEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES;

    public Color? Color;
}
