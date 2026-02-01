using System.Numerics;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Stunnable;
using Content.Shared._Orion.Morph;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Body.Events;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Orion.Morph;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class MorphSystem : SharedMorphSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChameleonProjectorSystem _chameleon = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    public ProtoId<DamageGroupPrototype> BurnDamageGroup = "Burn";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<MorphComponent, MeleeHitEvent>(OnAttack);

        SubscribeLocalEvent<MorphComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MorphComponent, BeingGibbedEvent>(OnDestroy);
        SubscribeLocalEvent<MorphComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<MorphComponent, MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<MorphComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
        SubscribeLocalEvent<MorphDisguiseComponent, ExaminedEvent>(OnDisguiseExamined);
        SubscribeLocalEvent<MorphComponent, InteractHandEvent>(OnInteract);

        SubscribeLocalEvent<MorphComponent, MorphOpenRadialMenuEvent>(OnMimicryRadialMenu);
        SubscribeLocalEvent<MorphComponent, EventMimicryActivate>(OnMimicryActivate);
        SubscribeLocalEvent<MorphComponent, MorphDevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<MorphComponent, MorphReproduceActionEvent>(OnReproduceAction);
        SubscribeLocalEvent<MorphComponent, MorphMimicryRememberActionEvent>(OnMimicryRememberAction);
        SubscribeLocalEvent<MorphComponent, MorphVentOpenActionEvent>(OnOpenVentAction);

        SubscribeLocalEvent<MorphAmbushComponent, MeleeHitEvent>(OnAmbushAttack);
        SubscribeLocalEvent<MorphAmbushComponent, UndisguisedEvent>(OnAmbushInteract);
        SubscribeLocalEvent<MorphComponent, MorphAmbushActionEvent>(OnAmbushAction);
        SubscribeLocalEvent<MorphAmbushComponent, UpdateCanMoveEvent>(OnCanMoveEvent);

        SubscribeLocalEvent<MorphComponent, MorphDevourDoAfterEvent>(OnDoDevourAfter);
    }

    #region Core

    private void OnInit(EntityUid uid, MorphComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.DevourActionEntity, component.DevourAction);
        _actions.AddAction(uid, ref component.MemoryActionEntity, component.MemoryAction);
        _actions.AddAction(uid, ref component.MimicryActionEntity, component.MimicryAction);
        _actions.AddAction(uid, ref component.ReplicationActionEntity, component.ReplicationAction);
        _actions.AddAction(uid, ref component.AmbushActionEntity, component.AmbushAction);
        _actions.AddAction(uid, ref component.VentOpenActionEntity, component.VentOpenAction);

        _alerts.ShowAlert(uid, component.BiomassAlert);
    }

    private void OnInteract(Entity<MorphComponent> morph, ref InteractHandEvent args)
    {
        _chameleon.TryReveal(morph.Owner);
    }

    private void OnDestroy(EntityUid uid, MorphComponent morph, ref BeingGibbedEvent args)
    {
        foreach (var entity in morph.ContainedCreatures)
        {
            var transform = Transform(uid);
            _transform.SetCoordinates(entity, transform.Coordinates);
        }
    }

    private void OnDamage(EntityUid uid, MorphComponent morph, DamageChangedEvent args)
    {
        if (!HasComp<ChameleonDisguisedComponent>(uid))
            return;

        if (args.DamageDelta is null)
            return;

        if (!args.DamageIncreased)
            return;

        if (args.DamageDelta.GetTotal() < morph.DamageThreshold)
            return;

        if (TryComp<ChameleonDisguisedComponent>(uid, out var comp))
            _chameleon.TryReveal((uid, comp));
    }

    private void OnDeath(Entity<MorphComponent> morph, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Dead && TryComp<ChameleonDisguisedComponent>(morph.Owner, out var comp))
            _chameleon.TryReveal((morph.Owner, comp));
    }

    private void OnTransformSpeakerName(Entity<MorphComponent> morph, ref TransformSpeakerNameEvent arg)
    {
        if (!TryComp<ChameleonDisguisedComponent>(morph.Owner, out var comp))
            return;

        arg.VoiceName = MetaData(comp.Disguise).EntityName;
        arg.Sender = comp.Disguise;
    }

    private void OnDisguiseExamined(Entity<MorphDisguiseComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = Loc.GetString(ent.Comp.ExamineMessage);
        args.PushMarkup($"[color={ent.Comp.ExamineColor.ToHex()}]{msg}[/color]");
    }

    private void ChangeBiomassAmount(FixedPoint2 amount, EntityUid uid, MorphComponent? morph = null)
    {
        if (morph == null)
            return;

        morph.Biomass = FixedPoint2.Min(morph.Biomass + amount, morph.MaxBiomass);
        Dirty(uid, morph);
        _alerts.ShowAlert(uid, morph.BiomassAlert);
    }

    #endregion

    #region Attack

    private void OnAttacked(Entity<MorphComponent> morph, ref AttackedEvent args)
    {
        if (!TryComp<HungerComponent>(morph, out var hunger))
            return;

        if (args.User == args.Used)
        {
            _damageable.TryChangeDamage(args.User, morph.Comp.DamageOnTouch);
            _hunger.ModifyHunger(morph, morph.Comp.DevourWeaponHungerCost, hunger);
        }
        else if (_random.Prob(morph.Comp.DevourWeaponOnBeingHit) && _hunger.GetHunger(hunger) >= morph.Comp.DevourWeaponHungerCost)
        {
            morph.Comp.ContainedCreatures.Add(args.Used);
            _transform.SetCoordinates(args.Used, new EntityCoordinates(EntityUid.Invalid, Vector2.Zero));
            _audioSystem.PlayPvs(morph.Comp.SoundDevour, morph);
            _hunger.ModifyHunger(morph, -morph.Comp.DevourWeaponHungerCost, hunger);
        }
    }

    private void OnAttack(Entity<MorphComponent> morph, ref MeleeHitEvent args)
    {
        _chameleon.TryReveal(morph.Owner);

        if (args.HitEntities.Count <= 0)
            return;

        if (!TryComp<HandsComponent>(args.HitEntities[0], out var hands))
            return;

        if (!TryComp<HungerComponent>(morph, out var hunger))
            return;

        if (!_hands.TryGetActiveItem((args.HitEntities[0], hands), out var item) ||
            !_random.Prob(morph.Comp.DevourWeaponOnHit))
            return;

        if (_hunger.GetHunger(hunger) < morph.Comp.DevourWeaponHungerCost)
            return;

        morph.Comp.ContainedCreatures.Add(item.Value);
        _transform.SetCoordinates(item.Value, new EntityCoordinates(EntityUid.Invalid, Vector2.Zero));
        _audioSystem.PlayPvs(morph.Comp.SoundDevour, morph);
        _hunger.ModifyHunger(morph, -morph.Comp.DevourWeaponHungerCost, hunger);
    }

    #endregion

    #region Ambush

    private void OnAmbushAction(EntityUid uid, MorphComponent morph, MorphAmbushActionEvent args)
    {
        if (!TryComp<ChameleonProjectorComponent>(uid, out var chamel))
            return;

        if (NonMorphInRange(uid, morph))
        {
            _popup.PopupCursor(Loc.GetString("morph-ambush-blocked"), uid);
            return;
        }

        if (TryComp<MorphAmbushComponent>(uid, out _))
        {
            AmbushBreak(uid);
            if (chamel.Disguised != null)
                AmbushBreak(chamel.Disguised.Value);
        }
        else
        {
            EnsureComp<MorphAmbushComponent>(uid);
            _popup.PopupCursor(Loc.GetString("morphs-into-ambush"), uid);

            if (TryComp<ChameleonDisguisedComponent>(uid, out var disgui))
                EnsureComp<MorphAmbushComponent>(disgui.Disguise);
            _actionBlocker.UpdateCanMove(uid);
        }
    }

    private void OnCanMoveEvent(EntityUid uid, MorphAmbushComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnAmbushAttack(Entity<MorphAmbushComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        _standing.Down(args.HitEntities[0]);
        AmbushBreak(ent);
    }

    public void AmbushBreak(EntityUid uid)
    {
        if (!HasComp<MorphAmbushComponent>(uid))
            return;

        _popup.PopupCursor(Loc.GetString("morphs-out-of-ambush"), uid);
        RemCompDeferred<MorphAmbushComponent>(uid);

        if (TryComp<MorphComponent>(uid, out var morph))
        {
            _chameleon.TryReveal(uid);
            _actions.StartUseDelay(morph.AmbushActionEntity);
        }

        if (TryComp<ChameleonProjectorComponent>(uid, out var chamel) && chamel.Disguised != null)
            RemCompDeferred<MorphAmbushComponent>(chamel.Disguised.Value);

        if (!TryComp<InputMoverComponent>(uid, out var input))
            return;

        input.CanMove = true;
        Dirty(uid, input);
    }

    private bool NonMorphInRange(EntityUid uid, MorphComponent morph)
    {
        var coordinates = _transform.GetMapCoordinates(uid);
        foreach (var entity in _lookup.GetEntitiesInRange(coordinates, morph.AmbushBlockRange))
        {
            if (!HasComp<MindContainerComponent>(entity) || HasComp<MorphComponent>(entity) ||
                HasComp<GhostComponent>(entity))
                continue;

            if ((TryComp<MobStateComponent>(entity, out var entityMobState) && HasComp<GhostTakeoverAvailableComponent>(entity) && _mobState.IsDead(entity, entityMobState)))
                continue;

            return true;
        }

        return false;
    }

    private void OnAmbushInteract(EntityUid uid, MorphAmbushComponent component, UndisguisedEvent args)
    {
        _stun.TryParalyze(args.User, component.StunTimeInteract, false);
        _damageable.TryChangeDamage(args.User, component.DamageOnTouch);
        AmbushBreak(uid);
    }

    #endregion

    #region Disguise

    private void OnMimicryRadialMenu(EntityUid uid, MorphComponent morph, MorphOpenRadialMenuEvent args)
    {
        morph.MimicryContainer = _container.EnsureContainer<Container>(uid, morph.MimicryContainerId);

        if (!TryComp<UserInterfaceComponent>(uid, out var uic))
            return;

        _ui.OpenUi((uid, uic), MimicryKey.Key, uid);
        _chameleon.TryReveal(uid);
    }

    private void OnMimicryRememberAction(EntityUid uid, MorphComponent morph, MorphMimicryRememberActionEvent args)
    {
        if (!TryComp<ChameleonProjectorComponent>(uid, out var chamel))
            return;

        if (TryComp<HumanoidAppearanceComponent>(args.Target, out _))
        {
            // TODO: Implement humanoid mimicry properly
            _popup.PopupCursor(Loc.GetString("morph-unable-to-remember-humanoid"), uid);
            return;
        }

        if (_chameleon.IsInvalid(chamel, args.Target))
        {
            _popup.PopupCursor(Loc.GetString("morph-unable-to-remember"), uid);
            return;
        }

        if (morph.MemoryObjects.Count >= 5)
        {
            morph.MemoryObjects.RemoveAt(0);
        }

        morph.MemoryObjects.Add(args.Target);
        _popup.PopupEntity(
            Loc.GetString("morph-remember-action-success", ("target", ToPrettyString(args.Target))),
            uid,
            PopupType.Medium
        );

        Dirty(uid, morph);
    }

    private void OnMimicryActivate(EntityUid uid, MorphComponent morph, EventMimicryActivate args)
    {
        if (!TryComp<ChameleonProjectorComponent>(uid, out var chamel))
            return;

        var targ = GetEntity(args.Target);

        if (targ != null)
            MimicryNonHumanoid((uid, chamel), targ.Value);
    }

    public void MimicryNonHumanoid(Entity<ChameleonProjectorComponent> morph, EntityUid toChameleon)
    {
        if (!Exists(toChameleon) || Deleted(toChameleon))
            return;

        _chameleon.Disguise(morph, morph, toChameleon);
    }

    #endregion

    #region Devour

    private void OnDevourAction(EntityUid uid, MorphComponent morph, MorphDevourActionEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(morph.DevourWhitelist, args.Target))
            return;

        if (_whitelistSystem.IsWhitelistPassOrNull(morph.DevourBlacklist, args.Target))
        {
            _popup.PopupEntity(Loc.GetString("devour-action-popup-message-blacklisted", ("target", ToPrettyString(args.Target))), uid, uid);
            return;
        }

        args.Handled = true;
        var target = args.Target;
        AmbushBreak(uid);

        if (TryComp(target, out MobStateComponent? targetState))
        {
            switch (targetState.CurrentState)
            {
                case MobState.Critical:
                    _popup.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, uid);
                    break;
                case MobState.Dead:

                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, morph.DevourTime, new MorphDevourDoAfterEvent(), uid, target: target, used: uid)
                    {
                        BreakOnMove = true,
                    });
                    break;
                default:
                    _popup.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, uid);
                    break;
            }

            return;
        }

        _popup.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, uid);
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, morph.DevourTime / 2, new MorphDevourDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
        });
    }

    private void OnDoDevourAfter(EntityUid uid, MorphComponent morph, MorphDevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (!TryComp<HungerComponent>(uid, out var hunger))
            return;

        // Item devour
        if (!TryComp<MobThresholdsComponent>(args.Target, out var state) || !_threshold.TryGetDeadThreshold(args.Target.Value, out var health))
        {
            health = -morph.DevourWeaponHungerCost;
            _hunger.ModifyHunger(uid, (int)Math.Abs((float)health.Value / 3.5f), hunger);
            _audioSystem.PlayPvs(morph.SoundDevour, uid);
            morph.ContainedCreatures.Add(args.Target.Value);
            _transform.SetCoordinates(args.Target.Value, new EntityCoordinates(EntityUid.Invalid, Vector2.Zero));
            return;
        }

        if (state.CurrentThresholdState != MobState.Dead)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            health /= 2;

        var damageBrute = new DamageSpecifier(_proto.Index(BruteDamageGroup), -health.Value / 2);
        var damageBurn = new DamageSpecifier(_proto.Index(BurnDamageGroup), -health.Value / 2);

        _damageable.TryChangeDamage(uid, damageBrute);
        _damageable.TryChangeDamage(uid, damageBurn);
        _hunger.ModifyHunger(uid, (int)Math.Abs((float)health.Value / 3.5f), hunger);
        _audioSystem.PlayPvs(morph.SoundDevour, uid);
        morph.ContainedCreatures.Add(args.Target.Value);
        _transform.SetCoordinates(args.Target.Value, new EntityCoordinates(EntityUid.Invalid, Vector2.Zero));
    }

    #endregion

    #region Reproduce

    private void OnReproduceAction(EntityUid uid, MorphComponent morph, MorphReproduceActionEvent args)
    {
        if (!TryComp<HungerComponent>(uid, out var hunger))
            return;

        if (!(_hunger.GetHunger(hunger) >= morph.ReplicationCost))
            return;

        Spawn(morph.MorphSpawnProto, Transform(uid).Coordinates);
        _hunger.ModifyHunger(uid, -morph.ReplicationCost, hunger);

        var morphList = new List<EntityUid>();
        var morphs = AllEntityQuery<MorphComponent, MobStateComponent>();
        while (morphs.MoveNext(out var ent, out _, out _))
        {
            morphList.Add(ent);
        }

        if (morphList.Count == morph.DetectableCount)
        {
            _chatSystem.DispatchFilteredAnnouncement(Filter.Broadcast(), Loc.GetString("morphs-announcement"), playSound: false, colorOverride: Color.Gold);
            _audioSystem.PlayGlobal(morph.SoundReplication, Filter.Broadcast(), true);
        }

        _actions.StartUseDelay(morph.ReplicationActionEntity);
    }

    #endregion

    #region Vent

    private void OnOpenVentAction(EntityUid uid, MorphComponent morph, MorphVentOpenActionEvent args)
    {
        if (!TryComp<HungerComponent>(uid, out var hunger))
            return;

        if (_container.IsEntityInContainer(uid))
            return;

        if (_hunger.GetHunger(hunger) < morph.OpenVentCost)
            return;

        if (!TryComp<WeldableComponent>(args.Target, out var weldableComponent) || !weldableComponent.IsWelded)
            return;

        _hunger.ModifyHunger(uid, -morph.OpenVentCost, hunger);
        _weldable.SetWeldedState(args.Target, false, weldableComponent);
        _popup.PopupEntity(Loc.GetString("morph-vent-action-success", ("target", ToPrettyString(args.Target))), uid, PopupType.Medium);
    }

    #endregion
}
