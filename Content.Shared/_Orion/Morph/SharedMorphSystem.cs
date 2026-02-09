using Content.Shared.Humanoid;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;

namespace Content.Shared._Orion.Morph;

//
// License-Identifier: AGPL-3.0-or-later
//

public abstract class SharedMorphSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedChameleonProjectorSystem _chameleon = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphComponent, AttemptMeleeEvent>(TryMeleeAttack);
        SubscribeLocalEvent<ChameleonProjectorComponent, EventMimicryActivate>(TryMimicry);
    }

    private void TryMeleeAttack(EntityUid uid, MorphComponent component, ref AttemptMeleeEvent args)
    {
        // Abort attack if the user is disguised
        if (HasComp<ChameleonDisguisedComponent>(uid))
            args.Cancelled = true;
    }

    private void TryMimicry(Entity<ChameleonProjectorComponent> ent, ref EventMimicryActivate arg)
    {
        var target = GetEntity(arg.Target);
        if (target == null)
            return;

        if (!_chameleon.TryDisguise(ent, ent.Owner, target.Value))
            return;

        DisguiseInventory(ent, target.Value);
    }

    public void DisguiseInventory(Entity<ChameleonProjectorComponent> ent, EntityUid target)
    {
        if (_net.IsClient)
            return;

        var user = ent.Comp.Disguised;

        if (!TryComp<ChameleonDisguisedComponent>(user, out var chameleon))
            return;

        var disguise = chameleon.Disguise;

        if (!TryComp<HumanoidAppearanceComponent>(target, out _))
            return;

        EnsureComp<HumanoidAppearanceComponent>(disguise);
        _humanoidAppearance.CloneAppearance(target, disguise);
    }
}
