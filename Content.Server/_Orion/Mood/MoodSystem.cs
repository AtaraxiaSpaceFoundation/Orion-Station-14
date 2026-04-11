using System.Linq;
using Content.Goobstation.Common.Medical;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.Atmos.Events;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared._DV.Roles;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared._Orion.Mood;
using Content.Shared._Orion.Overlays;
using Content.Shared.Atmos;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Shared.Cuffs.Components;
using Content.Shared.Roles;
using Content.Shared.Slippery;

namespace Content.Server._Orion.Mood;

public sealed class MoodSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<MoodComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MoodComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MoodComponent, MoodEffectEvent>(OnMoodEffect);
        SubscribeLocalEvent<MoodComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeLocalEvent<MoodComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<MoodComponent, MoodRemoveEffectEvent>(OnRemoveEffect);
        SubscribeLocalEvent<MoodComponent, MoodPurgeEffectsEvent>(OnPurgeEffects);
        SubscribeLocalEvent<MoodComponent, ShowMoodAlertEvent>(OnShowMoodAlert);
        SubscribeLocalEvent<MoodComponent, CuffedStateChangeEvent>(OnCuffedStateChanged);
        SubscribeLocalEvent<MoodComponent, SuffocationEvent>(OnSuffocationStarted);
        SubscribeLocalEvent<MoodComponent, StopSuffocatingEvent>(OnSuffocationStopped);
        SubscribeLocalEvent<MoodComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<MoodComponent, ExtinguishedEvent>(OnExtinguished);
        SubscribeLocalEvent<MoodComponent, BeforeVomitEvent>(OnBeforeVomit);
        SubscribeLocalEvent<MoodComponent, ResistPressureEvent>(OnPressureDanger);
        SubscribeLocalEvent<MoodComponent, SendSafePressureEvent>(OnPressureSafe);
        SubscribeLocalEvent<SlipEvent>(OnSlip);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        var query = EntityQueryEnumerator<MoodComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var mood, out var mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            ProcessSanity(uid, mood, frameTime);
        }
    }

    private void OnShowMoodAlert(EntityUid uid, MoodComponent component, ShowMoodAlertEvent args)
    {
        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        var msg = BuildMoodExamineMessage(uid, component);
        _chatManager.ChatMessageToOne(ChatChannel.Emotes, msg, msg, EntityUid.Invalid, false, session.Channel);
    }

    private string BuildMoodExamineMessage(EntityUid uid, MoodComponent component)
    {
        var msg = "[examineborder]\n";
        msg += Loc.GetString("mood-show-effects-start");
        msg += $"\n[color=#282D31]{Loc.GetString("examine-border-line")}[/color]\n";

#if DEBUG
        // Keep sanity visible only in debug/development workflows.
        var sanity = Loc.GetString("mood-show-sanity-line", ("sanity", MathF.Round(component.CurrentSanity, 1)));
        msg += $"{sanity}\n";
#endif

        var hadVisibleEffects = false;

        foreach (var (_, protoId) in component.CategorisedEffects)
        {
            if (!_prototypeManager.TryIndex<MoodEffectPrototype>(protoId, out var proto)
                || proto.Hidden)
                continue;

            hadVisibleEffects = true;
            var color = proto.MoodChange > 0 ? "#008000" : "#BA0000";
            msg += $"[font size=10][color={color}]{proto.Description(uid)}[/color][/font]\n";
        }

        foreach (var (protoId, _) in component.UncategorisedEffects)
        {
            if (!_prototypeManager.TryIndex<MoodEffectPrototype>(protoId, out var proto)
                || proto.Hidden)
                continue;

            hadVisibleEffects = true;
            var color = proto.MoodChange > 0 ? "#008000" : "#BA0000";
            msg += $"[font size=10][color={color}]{proto.Description(uid)}[/color][/font]\n";
        }

        if (!hadVisibleEffects)
            msg += $"[font size=10][color=#808080]{Loc.GetString("mood-show-no-effects")}[/color][/font]\n";

        msg += "[/examineborder]";
        return msg;
    }

    private void OnShutdown(EntityUid uid, MoodComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlertCategory(uid, component.MoodCategory);
        RemComp<SaturationScaleOverlayComponent>(uid);
        ResetCritThresholds(uid, component);
    }

    private void OnRemoveEffect(EntityUid uid, MoodComponent component, MoodRemoveEffectEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (component.UncategorisedEffects.TryGetValue(args.EffectId, out _))
            RemoveEffect(uid, args.EffectId, null, args.Reason);
        else
        {
            foreach (var (category, id) in component.CategorisedEffects)
            {
                if (id != args.EffectId)
                    continue;

                RemoveEffect(uid, args.EffectId, category, args.Reason);
                return;
            }
        }
    }

    private void OnPurgeEffects(EntityUid uid, MoodComponent component, MoodPurgeEffectsEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        var moodletList = new List<string>();
        foreach (var moodlet in component.UncategorisedEffects)
        {
            if (!_prototypeManager.TryIndex(moodlet.Key, out MoodEffectPrototype? moodProto)
                || moodProto.Timeout == 0 && !args.RemovePermanentMoodlets)
                continue;

            moodletList.Add(moodlet.Key);
        }

        foreach (var moodId in moodletList)
        {
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent(moodId));
        }
    }

    private void OnRefreshMoveSpeed(EntityUid uid, MoodComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || _jetpack.IsUserFlying(uid))
            return;

        var modifier = 1f;
        if (component.CurrentMoodThreshold != MoodThreshold.Dead && component.CurrentMoodThreshold is <= MoodThreshold.Meh or >= MoodThreshold.Good)
        {
            // This ridiculous math serves a purpose making high mood less impactful on movement speed than low mood
            modifier =
                Math.Clamp(
                    (component.CurrentMoodLevel >= component.MoodThresholds[MoodThreshold.Neutral])
                        ? _config.GetCVar(CCVars.MoodIncreasesSpeed)
                            ? MathF.Pow(component.SpeedBonusGrowth, component.CurrentMoodLevel - component.MoodThresholds[MoodThreshold.Neutral])
                            : 1
                        : _config.GetCVar(CCVars.MoodDecreasesSpeed)
                            ? 2 - component.MoodThresholds[MoodThreshold.Neutral] / MathF.Max(component.CurrentMoodLevel, 1f)
                            : 1,
                    component.MinimumSpeedModifier,
                    component.MaximumSpeedModifier);
        }

        switch (component.CurrentSanityThreshold)
        {
            case <= SanityThreshold.Crazy:
                modifier *= 0.9f;
                break;
            case SanityThreshold.Unstable:
                modifier *= 0.95f;
                break;
        }

        args.ModifySpeed(1, modifier);
    }

    private void OnMoodEffect(EntityUid uid, MoodComponent component, MoodEffectEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !_prototypeManager.TryIndex<MoodEffectPrototype>(args.EffectId, out var prototype) )
            return;

        var ev = new OnMoodEffect(uid, args.EffectId, args.EffectModifier, args.EffectOffset);
        RaiseLocalEvent(uid, ref ev);

        ApplyEffect(uid, component, prototype, ev.EffectModifier, ev.EffectOffset);
    }

    public void AddEffect(EntityUid uid, string effectId, float modifier = 1f, float offset = 0f)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent(effectId, modifier, offset));
    }

    public void RemoveEffect(EntityUid uid, string effectId, MoodEffectRemovalReason reason = MoodEffectRemovalReason.Manual)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent(effectId, reason));
    }

    private void ApplyEffect(EntityUid uid, MoodComponent component, MoodEffectPrototype prototype, float eventModifier = 1, float eventOffset = 0)
    {
        // Apply categorized effect
        if (prototype.Category != null)
        {
            if (component.CategorisedEffects.TryGetValue(prototype.Category, out var oldPrototypeId))
            {
                if (!_prototypeManager.TryIndex<MoodEffectPrototype>(oldPrototypeId, out var oldPrototype))
                    return;

                // Don't send the moodlet popup if we already have the moodlet.
                if (!component.CategorisedEffects.ContainsValue(prototype.ID))
                    SendEffectText(uid, prototype);

                if (prototype.ID != oldPrototype.ID)
                    component.CategorisedEffects[prototype.Category] = prototype.ID;
            }
            else
            {
                component.CategorisedEffects.Add(prototype.Category, prototype.ID);
                SendEffectText(uid, prototype);
            }
        }
        // Apply uncategorized effect
        else
        {
            if (component.UncategorisedEffects.TryGetValue(prototype.ID, out _))
                return;

            var moodChange = prototype.MoodChange * eventModifier + eventOffset;
            if (moodChange == 0)
                return;

            SendEffectText(uid, prototype);

            component.UncategorisedEffects.Add(prototype.ID, moodChange);
        }

        ScheduleTimedRemoval(uid, component, prototype);

        RefreshMood(uid, component);
    }

    private void SendEffectText(EntityUid uid, MoodEffectPrototype prototype)
    {
        if (prototype.Hidden)
            return;

        _popup.PopupEntity(prototype.Description(uid), uid, uid, (prototype.MoodChange > 0) ? PopupType.Medium : PopupType.MediumCaution);
    }

    private void ScheduleTimedRemoval(EntityUid uid, MoodComponent component, MoodEffectPrototype prototype)
    {
        if (prototype.Timeout == 0)
            return;

        if (prototype.Category != null)
        {
            component.CategorisedEffectTimerGenerations.TryGetValue(prototype.Category, out var currentGeneration);
            var generation = currentGeneration + 1;
            component.CategorisedEffectTimerGenerations[prototype.Category] = generation;

            Timer.Spawn(TimeSpan.FromSeconds(prototype.Timeout),
                () =>
            {
                if (!TryComp(uid, out MoodComponent? currentComponent)
                    || !currentComponent.CategorisedEffectTimerGenerations.TryGetValue(prototype.Category, out var activeGeneration)
                    || activeGeneration != generation)
                    return;

                RemoveEffect(uid, prototype.ID, prototype.Category, MoodEffectRemovalReason.Expired);
            });

            return;
        }

        component.UncategorisedEffectTimerGenerations.TryGetValue(prototype.ID, out var uncategorisedGeneration);
        var newGeneration = uncategorisedGeneration + 1;
        component.UncategorisedEffectTimerGenerations[prototype.ID] = newGeneration;

        Timer.Spawn(TimeSpan.FromSeconds(prototype.Timeout),
            () =>
        {
            if (!TryComp(uid, out MoodComponent? currentComponent)
                || !currentComponent.UncategorisedEffectTimerGenerations.TryGetValue(prototype.ID, out var activeGeneration)
                || activeGeneration != newGeneration)
                return;

            RemoveEffect(uid, prototype.ID, null, MoodEffectRemovalReason.Expired);
        });
    }

    private void RemoveEffect(EntityUid uid, string prototypeId, string? category, MoodEffectRemovalReason reason)
    {
        if (!TryComp<MoodComponent>(uid, out var comp))
            return;

        if (category == null)
        {
            if (!comp.UncategorisedEffects.Remove(prototypeId))
                return;

            comp.UncategorisedEffectTimerGenerations.Remove(prototypeId);
        }
        else
        {
            if (!comp.CategorisedEffects.TryGetValue(category, out var currentProtoId)
                || currentProtoId != prototypeId
                || !_prototypeManager.HasIndex<MoodEffectPrototype>(currentProtoId))
                return;

            comp.CategorisedEffects.Remove(category);
            comp.CategorisedEffectTimerGenerations.Remove(category);
        }

        if (reason == MoodEffectRemovalReason.Expired)
            ReplaceMood(uid, prototypeId);

        RefreshMood(uid, comp);
    }

    /// <summary>
    ///     Some moods specifically create a moodlet upon expiration. This is normally used for "Addiction" type moodlets,
    ///     such as a positive moodlet from an addictive substance that becomes a negative moodlet when a timer ends.
    /// </summary>
    /// <remarks>
    ///     Moodlets that use this should probably also share a category with each other, but this isn't necessarily required.
    ///     Only if you intend that "Re-using the drug" should also remove the negative moodlet.
    /// </remarks>
    private void ReplaceMood(EntityUid uid, string prototypeId)
    {
        if (!_prototypeManager.TryIndex<MoodEffectPrototype>(prototypeId, out var proto)
            || proto.MoodletOnEnd is null)
            return;

        var ev = new MoodEffectEvent(proto.MoodletOnEnd);
        RaiseLocalEvent(uid, ev);
    }

    private void OnMobStateChanged(EntityUid uid, MoodComponent component, MobStateChangedEvent args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (args.NewMobState == MobState.Dead && args.OldMobState != MobState.Dead)
        {
            var ev = new MoodEffectEvent("Dead");
            RaiseLocalEvent(uid, ev);
            component.CurrentSanity = component.SanityThresholds[SanityThreshold.Disturbed];
        }
        else if (args.OldMobState == MobState.Dead && args.NewMobState != MobState.Dead)
        {
            var ev = new MoodRemoveEffectEvent("Dead");
            RaiseLocalEvent(uid, ev);
            component.CurrentSanity = component.SanityThresholds[SanityThreshold.Disturbed];
            component.CurrentSanityThreshold = SanityThreshold.Disturbed;
            component.LastSanityThreshold = SanityThreshold.Disturbed;
        }

        RefreshMood(uid, component);
    }

    private void OnCuffedStateChanged(EntityUid uid, MoodComponent component, ref CuffedStateChangeEvent args)
    {
        if (!TryComp<CuffableComponent>(uid, out var cuffable) || cuffable.CuffedHandCount <= 0)
        {
            RaiseLocalEvent(uid, new MoodRemoveEffectEvent("Handcuffed"));
            return;
        }

        RaiseLocalEvent(uid, new MoodEffectEvent("Handcuffed"));
    }

    private void OnSuffocationStarted(EntityUid uid, MoodComponent component, ref SuffocationEvent args)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent("Suffocating"));
    }

    private void OnSuffocationStopped(EntityUid uid, MoodComponent component, ref StopSuffocatingEvent args)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent("Suffocating"));
    }

    private void OnIgnited(EntityUid uid, MoodComponent component, ref IgnitedEvent args)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent("OnFire"));
    }

    private void OnExtinguished(EntityUid uid, MoodComponent component, ref ExtinguishedEvent args)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent("OnFire"));
    }

    private void OnBeforeVomit(EntityUid uid, MoodComponent component, ref BeforeVomitEvent args)
    {
        RaiseLocalEvent(uid, new MoodEffectEvent("MobVomit"));
    }

    private void OnPressureDanger(EntityUid uid, MoodComponent component, ref ResistPressureEvent args)
    {
        var effect = args.Pressure <= Atmospherics.WarningLowPressure
            ? "MobLowPressure"
            : "MobHighPressure";
        RaiseLocalEvent(uid, new MoodEffectEvent(effect));
    }

    private void OnPressureSafe(EntityUid uid, MoodComponent component, ref SendSafePressureEvent args)
    {
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent("MobLowPressure"));
        RaiseLocalEvent(uid, new MoodRemoveEffectEvent("MobHighPressure"));
    }

    private void OnSlip(ref SlipEvent args)
    {
        if (!HasComp<MoodComponent>(args.Slipped))
            return;

        RaiseLocalEvent(args.Slipped, new MoodEffectEvent("MobSlipped"));
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } ownedEntity || !HasComp<MoodComponent>(ownedEntity))
            return;

        if (args.Mind.MindRoles.Any(HasComp<TraitorRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodEffectEvent("TraitorFocused"));

        if (args.Mind.MindRoles.Any(HasComp<RevolutionaryRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodEffectEvent("RevolutionFocused"));

        if (args.Mind.MindRoles.Any(HasComp<CosmicCultRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodEffectEvent("CultFocused"));
    }

    private void OnRoleRemoved(RoleRemovedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } ownedEntity || !HasComp<MoodComponent>(ownedEntity))
            return;

        if (!args.Mind.MindRoles.Any(HasComp<TraitorRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodRemoveEffectEvent("TraitorFocused"));

        if (!args.Mind.MindRoles.Any(HasComp<RevolutionaryRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodRemoveEffectEvent("RevolutionFocused"));

        if (!args.Mind.MindRoles.Any(HasComp<CosmicCultRoleComponent>))
            RaiseLocalEvent(ownedEntity, new MoodRemoveEffectEvent("CultFocused"));
    }

    // <summary>
    //      Recalculate the mood level of an entity by summing up all moodlets.
    // </summary>
    private void RefreshMood(EntityUid uid, MoodComponent component)
    {
        var totalMood = 0f;
        var shownMood = 0f;

        foreach (var (_, protoId) in component.CategorisedEffects)
        {
            if (!_prototypeManager.TryIndex<MoodEffectPrototype>(protoId, out var prototype))
                continue;

            totalMood += prototype.MoodChange;
            if (!prototype.Hidden)
                shownMood += prototype.MoodChange;
        }

        foreach (var (protoId, value) in component.UncategorisedEffects)
        {
            totalMood += value;

            if (!_prototypeManager.TryIndex<MoodEffectPrototype>(protoId, out var prototype)
                || prototype.Hidden)
                continue;

            shownMood += value;
        }

        component.CurrentMood = totalMood;
        component.CurrentShownMood = shownMood;
        SetMood(uid, totalMood, component, refresh: true);
    }

    private void OnInit(EntityUid uid, MoodComponent component, ComponentStartup args)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled))
            return;

        if (_config.GetCVar(CCVars.MoodModifiesThresholds)
            && TryComp<MobThresholdsComponent>(uid, out var mobThresholdsComponent))
            TryCacheBaselineThresholds(uid, component, mobThresholdsComponent);

        EnsureComp<NetMoodComponent>(uid);
        RefreshMood(uid, component);
        DoMoodThresholdsEffects(uid, component, force: true);
        SetCritThreshold(uid, component, GetMovementThreshold(component.CurrentMoodThreshold));
    }

    private void SetMood(EntityUid uid, float amount, MoodComponent? component = null, bool force = false, bool refresh = false)
    {
        if (!_config.GetCVar(CCVars.MoodEnabled)
            || !Resolve(uid, ref component)
            || component.CurrentMoodThreshold == MoodThreshold.Dead && !refresh)
            return;

        var ev = new OnSetMoodEvent(uid, amount, false);
        RaiseLocalEvent(uid, ref ev);

        if (ev.Cancelled)
            return;

        uid = ev.Receiver;
        amount = ev.MoodChangedAmount;

        if (!Resolve(uid, ref component))
            return;

        var neutral = component.MoodThresholds[MoodThreshold.Neutral];

        var targetMoodLevel = amount + neutral + ev.MoodOffset;
        var newMoodLevel = force
            ? targetMoodLevel
            : Math.Clamp(targetMoodLevel,
                component.MoodThresholds[MoodThreshold.Dead],
                component.MoodThresholds[MoodThreshold.Insane]);

        component.CurrentMoodLevel = newMoodLevel;

        if (TryComp<NetMoodComponent>(uid, out var mood))
        {
            mood.CurrentMoodLevel = component.CurrentMoodLevel;
            mood.CurrentMood = component.CurrentMood;
            mood.CurrentShownMood = component.CurrentShownMood;
            mood.NeutralMoodThreshold = component.MoodThresholds.GetValueOrDefault(MoodThreshold.Neutral);
            mood.CurrentSanity = component.CurrentSanity;
            Dirty(uid, mood);
        }

        RefreshShaders(uid, component.CurrentMoodLevel, component.MoodThresholds[MoodThreshold.Neutral]);
        UpdateCurrentThreshold(uid, component);
    }

    private void UpdateCurrentThreshold(EntityUid uid, MoodComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var calculatedThreshold = GetMoodThreshold(component);
        if (calculatedThreshold == component.CurrentMoodThreshold)
            return;

        component.CurrentMoodThreshold = calculatedThreshold;

        DoMoodThresholdsEffects(uid, component);
    }

    private void DoMoodThresholdsEffects(EntityUid uid, MoodComponent? component = null, bool force = false)
    {
        if (!Resolve(uid, ref component)
            || component.CurrentMoodThreshold == component.LastThreshold && !force)
            return;

        var modifier = GetMovementThreshold(component.CurrentMoodThreshold);

        // Modify mob stats
        if (modifier != GetMovementThreshold(component.LastThreshold))
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            SetCritThreshold(uid, component, modifier);
        }

        // Modify interface
        if (TryGetPriorityMoodAlert(component, out var priorityAlert))
            _alerts.ShowAlert(uid, priorityAlert);
        else if (component.MoodThresholdsAlerts.TryGetValue(component.CurrentMoodThreshold, out var alertId))
            _alerts.ShowAlert(uid, alertId);
        else
            _alerts.ClearAlertCategory(uid, component.MoodCategory);

        component.LastThreshold = component.CurrentMoodThreshold;
    }

    private void RefreshShaders(EntityUid uid, float mood, float neutralThreshold)
    {
        if (neutralThreshold <= 0f)
            return;

        EnsureComp<SaturationScaleOverlayComponent>(uid, out var comp);
        comp.NeutralMoodThreshold = neutralThreshold;
        comp.SaturationScale = mood / comp.NeutralMoodThreshold;
        Dirty(uid, comp);
    }

    private bool TryGetPriorityMoodAlert(MoodComponent component, out ProtoId<AlertPrototype> alert)
    {
        alert = default;
        var bestMagnitude = float.MinValue;

        foreach (var (_, protoId) in component.CategorisedEffects)
        {
            TryUpdatePriorityAlert(protoId, ref alert, ref bestMagnitude);
        }

        foreach (var (protoId, _) in component.UncategorisedEffects)
        {
            TryUpdatePriorityAlert(protoId, ref alert, ref bestMagnitude);
        }

        return bestMagnitude > float.MinValue;
    }

    private bool TryUpdatePriorityAlert(string protoId, ref ProtoId<AlertPrototype> bestAlert, ref float bestMagnitude)
    {
        if (!_prototypeManager.TryIndex<MoodEffectPrototype>(protoId, out var prototype)
            || prototype.SpecialAlert is not { } specialAlert
            || !prototype.SpecialAlertReplace)
            return false;

        var magnitude = MathF.Abs(prototype.MoodChange);
        if (magnitude <= bestMagnitude)
            return false;

        bestMagnitude = magnitude;
        bestAlert = specialAlert;
        return true;
    }

    private void ProcessSanity(EntityUid uid, MoodComponent component, float frameTime)
    {
        if (component.CurrentMoodThreshold == MoodThreshold.Dead
            || !component.SanityDeltaPerSecond.TryGetValue(component.CurrentMoodThreshold, out var deltaPerSecond))
            return;

        var sanityTarget = component.MaxSanity;
        if (deltaPerSecond < 0f)
        {
            sanityTarget = component.CurrentMoodThreshold switch
            {
                MoodThreshold.Insane or MoodThreshold.Horrible => component.MinSanity,
                MoodThreshold.Terrible => component.SanityThresholds[SanityThreshold.Crazy],
                _ => component.UnstableFloorSanity,
            };
        }
        else
        {
            sanityTarget = component.CurrentMoodThreshold switch
            {
                <= MoodThreshold.Great => component.UnstableFloorSanity,
                MoodThreshold.Exceptional => component.SanityThresholds[SanityThreshold.Disturbed],
                _ => sanityTarget,
            };
        }

        SetSanity(uid, component, component.CurrentSanity + deltaPerSecond * frameTime, sanityTarget, frameTime);
    }

    private void SetSanity(EntityUid uid, MoodComponent component, float value, float target, float frameTime)
    {
        var min = component.MinSanity;
        var max = component.MaxSanity;
        var boundedTarget = Math.Clamp(target, min, max);

        var clamped = Math.Clamp(value, min, max);

        if (boundedTarget > component.CurrentSanity && clamped < boundedTarget)
        {
            var step = Math.Max(component.SanityRecoveryRate * frameTime, 0f);
            clamped = Math.Min(clamped + step, boundedTarget);
        }

        if (Math.Abs(clamped - component.CurrentSanity) < 0.001f)
            return;

        component.CurrentSanity = clamped;
        component.CurrentSanityThreshold = GetSanityThreshold(component);

        if (component.CurrentSanityThreshold != component.LastSanityThreshold)
        {
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            component.LastSanityThreshold = component.CurrentSanityThreshold;
        }

        if (!TryComp<NetMoodComponent>(uid, out var mood))
            return;

        mood.CurrentSanity = component.CurrentSanity;
        Dirty(uid, mood);
    }

    private static SanityThreshold GetSanityThreshold(MoodComponent component)
    {
        if (component.CurrentSanity <= component.SanityThresholds[SanityThreshold.Insane])
            return SanityThreshold.Insane;

        if (component.CurrentSanity <= component.SanityThresholds[SanityThreshold.Crazy])
            return SanityThreshold.Crazy;

        if (component.CurrentSanity <= component.SanityThresholds[SanityThreshold.Unstable])
            return SanityThreshold.Unstable;

        return component.CurrentSanity <= component.SanityThresholds[SanityThreshold.Disturbed]
            ? SanityThreshold.Disturbed
            : SanityThreshold.Great;
    }

    private void SetCritThreshold(EntityUid uid, MoodComponent component, int modifier)
    {
        if (!_config.GetCVar(CCVars.MoodModifiesThresholds)
            || !TryComp<MobThresholdsComponent>(uid, out var mobThresholds)
            || !TryCacheBaselineThresholds(uid, component, mobThresholds))
            return;

        var multiplier = modifier switch
        {
            1 => component.IncreaseCritThreshold,
            -1 => component.DecreaseCritThreshold,
            _ => 1f,
        };

        var soft = component.SoftCritThresholdBeforeModify.Float() * multiplier;
        var hard = component.HardCritThresholdBeforeModify.Float() * multiplier;
        var dead = component.DeadThresholdBeforeModify.Float() * multiplier;

        // Keep threshold ordering valid after scaling.
        hard = MathF.Max(hard, soft + 0.01f);
        dead = MathF.Max(dead, hard + 0.01f);

        _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(soft), MobState.SoftCritical, mobThresholds);
        _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(hard), MobState.HardCritical, mobThresholds);
        _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(dead), MobState.Dead, mobThresholds);
    }

    private bool TryCacheBaselineThresholds(EntityUid uid, MoodComponent component, MobThresholdsComponent thresholds)
    {
        if (component.SoftCritThresholdBeforeModify != default
            && component.HardCritThresholdBeforeModify != default
            && component.DeadThresholdBeforeModify != default)
            return true;

        if (!_mobThreshold.TryGetThresholdForState(uid, MobState.SoftCritical, out var soft, thresholds)
            || !_mobThreshold.TryGetThresholdForState(uid, MobState.HardCritical, out var hard, thresholds)
            || !_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var dead, thresholds))
            return false;

        component.SoftCritThresholdBeforeModify = soft.Value;
        component.HardCritThresholdBeforeModify = hard.Value;
        component.DeadThresholdBeforeModify = dead.Value;
        return true;
    }

    private void ResetCritThresholds(EntityUid uid, MoodComponent component)
    {
        if (!_config.GetCVar(CCVars.MoodModifiesThresholds)
            || !TryComp<MobThresholdsComponent>(uid, out var thresholds)
            || !TryCacheBaselineThresholds(uid, component, thresholds))
            return;

        _mobThreshold.SetMobStateThreshold(uid, component.SoftCritThresholdBeforeModify, MobState.SoftCritical, thresholds);
        _mobThreshold.SetMobStateThreshold(uid, component.HardCritThresholdBeforeModify, MobState.HardCritical, thresholds);
        _mobThreshold.SetMobStateThreshold(uid, component.DeadThresholdBeforeModify, MobState.Dead, thresholds);
    }

    private static MoodThreshold GetMoodThreshold(MoodComponent component, float? moodLevel = null)
    {
        moodLevel ??= component.CurrentMoodLevel;
        var result = MoodThreshold.Dead;
        var value = component.MoodThresholds[MoodThreshold.Insane];

        foreach (var threshold in component.MoodThresholds)
        {
            if (!(threshold.Value <= value) || !(threshold.Value >= moodLevel))
                continue;

            result = threshold.Key;
            value = threshold.Value;
        }

        return result;
    }

    private static int GetMovementThreshold(MoodThreshold threshold)
    {
        return threshold switch
        {
            >= MoodThreshold.Good => 1,
            <= MoodThreshold.Meh => -1,
            _ => 0,
        };
    }

    private void OnDamageChange(EntityUid uid, MoodComponent component, DamageChangedEvent args)
    {
        if (!_mobThreshold.TryGetPercentageForState(uid, MobState.Critical, args.Damageable.TotalDamage, out var damage))
            return;

        ProtoId<MoodEffectPrototype> protoId = "HealthNoDamage";
        var value = component.HealthMoodEffectsThresholds["HealthNoDamage"];

        foreach (var threshold in component.HealthMoodEffectsThresholds)
        {
            if (!(threshold.Value <= damage) || !(threshold.Value >= value))
                continue;

            protoId = threshold.Key;
            value = threshold.Value;
        }

        var ev = new MoodEffectEvent(protoId);
        RaiseLocalEvent(uid, ev);
    }
}
