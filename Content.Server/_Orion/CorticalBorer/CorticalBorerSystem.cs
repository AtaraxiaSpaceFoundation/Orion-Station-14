// SPDX-FileCopyrightText: 2025 Coenx-flex
// SPDX-FileCopyrightText: 2025 Cojoke
// SPDX-FileCopyrightText: 2025 ScyronX
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Medical;
using Content.Server.Medical.Components;
using Content.Server.Objectives;
using Content.Server.Stunnable;
using Content.Shared._Orion.CorticalBorer;
using Content.Shared._Orion.CorticalBorer.Components;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Inventory;
using Content.Shared.MedicalScanner;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Orion.CorticalBorer;

public sealed partial class CorticalBorerSystem : SharedCorticalBorerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly HealthAnalyzerSystem _analyzer = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _admin = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GhostRoleSystem _ghost = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private const string HeadSlot = "head";
    private const string HostMindContainer = "PlayerMindContainer";

    private const string EndControlAction = "ActionBorerEndControlHost";

    public override void Initialize()
    {
        SubscribeAbilities();

        SubscribeLocalEvent<CorticalBorerComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<CorticalBorerComponent, CorticalBorerDispenserInjectMessage>(OnInjectReagentMessage);
        SubscribeLocalEvent<CorticalBorerComponent, CorticalBorerDispenserSetInjectAmountMessage>(
            OnSetInjectAmountMessage);

        SubscribeLocalEvent<InventoryComponent, InfestHostAttempt>(OnInfestHostAttempt);
        SubscribeLocalEvent<CorticalBorerComponent, CheckTargetedSpeechEvent>(OnSpeakEvent);

        SubscribeLocalEvent<CorticalBorerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<CorticalBorerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);

        SubscribeLocalEvent<CorticalBorerComponent, CorticalBorerForceSpeakMessage>(OnForceSpeakMessage);

        SubscribeLocalEvent<CorticalBorerComponent, CorticalBorerSurgicallyRemovedEvent>(OnSurgicallyRemoved);
        SubscribeLocalEvent<CorticalBorerComponent, EntityTerminatingEvent>(OnBorerTerminating);

        SubscribeLocalEvent<CorticalBorerInfestedComponent, PolymorphedEvent>(OnHostPolymorphed);
        SubscribeLocalEvent<CorticalBorerInfestedComponent, EntParentChangedMessage>(OnHostParentChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
        SubscribeLocalEvent<GameRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);
    }

    private void OnStartup(Entity<CorticalBorerComponent> ent, ref ComponentStartup args)
    {
        // Add actions
        foreach (var actionId in ent.Comp.InitialCorticalBorerActions)
        {
            Actions.AddAction(ent, actionId);
        }

        _alerts.ShowAlert(ent, ent.Comp.ChemicalAlert);
        UpdateUiState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityManager.EntityQuery<CorticalBorerComponent>())
        {
            if (_timing.CurTime < comp.UpdateTimer)
                continue;

            comp.UpdateTimer = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateCooldown);

#pragma warning disable CS0618
             if (!comp.Host.HasValue)
            {
                _alerts.ClearAlert(comp.Owner, comp.SugarAlert);
                continue;
            }

            var chemicalGeneration = comp.ChemicalGenerationRate;

            if (comp.WillingHosts.Contains(comp.Host.Value))
                chemicalGeneration = (int) MathF.Ceiling(chemicalGeneration * comp.WillingHostChemicalGenerationMultiplier);

            UpdateChemicals((comp.Owner, comp), chemicalGeneration);
            _damageable.TryChangeDamage(comp.Owner, comp.HealingDamage); // Heal borer

            if (HasBorerProtection(comp.Host.Value))
                _alerts.ShowAlert(comp.Owner, comp.SugarAlert);
            else
                _alerts.ClearAlert(comp.Owner, comp.SugarAlert);
#pragma warning restore CS0618
        }

        foreach (var comp in EntityManager.EntityQuery<CorticalBorerInfestedComponent>())
        {
#pragma warning disable CS0618
            if (_timing.CurTime >= comp.ControlTimeEnd)
                EndControl((comp.Owner, comp));
#pragma warning restore CS0618
        }
    }

    private static void OnSpeakEvent(Entity<CorticalBorerComponent> ent, ref CheckTargetedSpeechEvent args)
    {
        args.ChatTypeIgnore.Add(InGameICChatType.CollectiveMind);

        if (!ent.Comp.Host.HasValue)
            return;

        args.Targets.Add(ent);
        args.Targets.Add(ent.Comp.Host.Value);
    }

    public void UpdateChemicals(Entity<CorticalBorerComponent> ent, int change)
    {
        var (_, comp) = ent;

        if (comp.ChemicalPoints + change >= comp.ChemicalPointCap)
            comp.ChemicalPoints = comp.ChemicalPointCap;
        else if (comp.ChemicalPoints + change <= 0)
            comp.ChemicalPoints = 0;
        else
            comp.ChemicalPoints += change;

        if (comp.UiUpdateInterval > 0 && comp.ChemicalPoints % comp.UiUpdateInterval == 0)
            UpdateUiState(ent);

        _alerts.ShowAlert(ent, ent.Comp.ChemicalAlert);

        if (comp.Host.HasValue && !HasBorerProtection(comp.Host.Value))
            _alerts.ClearAlert(ent, ent.Comp.SugarAlert);
        else if (comp.Host.HasValue)
            _alerts.ShowAlert(ent, ent.Comp.SugarAlert);

        Dirty(ent);
    }

    private void OnInfestHostAttempt(Entity<InventoryComponent> entity, ref InfestHostAttempt args)
    {
        if (!_inventory.TryGetSlotEntity(entity.Owner, HeadSlot, out var headUid) ||
            !TryComp(headUid, out IngestionBlockerComponent? blocker) ||
            !blocker.Enabled)
            return;

        args.Blocker = headUid;
        args.Cancel();
    }

    /// <summary>
    /// Attempts to inject the Borer's host with chems
    /// </summary>
    public bool TryInjectHost(Entity<CorticalBorerComponent> ent,
        CorticalBorerChemicalPrototype chemicalPrototype,
        float chemAmount)
    {
        var (uid, comp) = ent;
        var requiredChemPoints = (int) Math.Ceiling(chemAmount * chemicalPrototype.Cost);

        if (requiredChemPoints <= 0)
            return false;

        // Need a host to inject something
        if (!comp.Host.HasValue)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-no-host"), uid, uid, PopupType.Medium);
            return false;
        }

        // Sugar block from injecting stuff
        if (!CanUseAbility(ent, comp.Host.Value))
            return false;

        // Make sure you can even hold the amount of chems you need
        if (requiredChemPoints > comp.ChemicalPointCap)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-not-enough-chem-storage"), uid, uid, PopupType.Medium);
            return false;
        }

        // Make sure you have enough chems
        if (requiredChemPoints > comp.ChemicalPoints)
        {
            Popup.PopupEntity(Loc.GetString("cortical-borer-not-enough-chem"), uid, uid, PopupType.Medium);
            return false;
        }

        // no injecting things that don't have blood silly
        if (!TryComp<BloodstreamComponent>(comp.Host, out var blood))
            return false;

        var solution = new Solution();
        solution.AddReagent(chemicalPrototype.Reagent, chemAmount);

        // add the chemicals to the bloodstream of the host
        if (!_blood.TryAddToChemicals((comp.Host.Value, blood), solution))
            return false;

        _admin.Add(LogType.ReagentEffect,
            LogImpact.Low,
            $"{ToPrettyString(uid):actor} injected {chemAmount}u of {chemicalPrototype.Reagent:reagent}"
            + $" (severity: {chemicalPrototype.Severity}) into host {ToPrettyString(comp.Host.Value):target}");

        UpdateChemicals(ent, -requiredChemPoints);
        return true;
    }

    private void OnInjectReagentMessage(Entity<CorticalBorerComponent> ent,
        ref CorticalBorerDispenserInjectMessage message)
    {
        if (TryGetBorerChemical(message.ChemProtoId, out var chemProto))
            TryInjectHost(ent, chemProto, ent.Comp.InjectAmount);

        UpdateUiState(ent);
    }

    private bool TryGetBorerChemical(string reagentId, [NotNullWhen(true)] out CorticalBorerChemicalPrototype? chemical)
    {
        foreach (var chem in _proto.EnumeratePrototypes<CorticalBorerChemicalPrototype>())
        {
            if (!chem.Reagent.Equals(reagentId))
                continue;

            chemical = chem;
            return true;
        }

        chemical = null;
        return false;
    }

    private void OnSetInjectAmountMessage(Entity<CorticalBorerComponent> ent,
        ref CorticalBorerDispenserSetInjectAmountMessage message)
    {
        ent.Comp.InjectAmount = message.CorticalBorerDispenserDispenseAmount;
        UpdateUiState(ent);
    }

    private List<CorticalBorerDispenserItem> GetAllBorerChemicals(Entity<CorticalBorerComponent> ent)
    {
        var clones = new List<CorticalBorerDispenserItem>();
        foreach (var prototype in _proto.EnumeratePrototypes<CorticalBorerChemicalPrototype>())
        {
            if (!_proto.TryIndex(prototype.Reagent, out ReagentPrototype? proto))
                continue;

            var reagentName = proto.LocalizedName;
            var reagentId = proto.ID;
            var cost = prototype.Cost;
            var amount = ent.Comp.InjectAmount;
            var borerChemicals = ent.Comp.ChemicalPoints;
            var color = proto.SubstanceColor;

            clones.Add(new CorticalBorerDispenserItem(reagentName,
                reagentId,
                cost,
                amount,
                borerChemicals,
                color)); // need color and name
        }

        return clones;
    }

    private void UpdateUiState(Entity<CorticalBorerComponent> ent)
    {
        var borerChemicals = GetAllBorerChemicals(ent);

        var state = new CorticalBorerDispenserBoundUserInterfaceState(borerChemicals, ent.Comp.InjectAmount);
        _userInterfaceSystem.SetUiState(ent.Owner, CorticalBorerDispenserUiKey.Key, state);
    }

    public bool TryToggleCheckBlood(Entity<CorticalBorerComponent> ent)
    {
        if (!TryComp<UserInterfaceComponent>(ent, out var uic))
            return false;

        if (!TryComp<HealthAnalyzerComponent>(ent, out var health))
            return false;

        // If open - close
        if (UI.IsUiOpen((ent, uic), HealthAnalyzerUiKey.Key))
        {
            UI.CloseUi((ent, uic), HealthAnalyzerUiKey.Key, ent.Owner);
            if (health.ScannedEntity.HasValue)
                _analyzer.StopAnalyzingEntity((ent, health), health.ScannedEntity.Value);
            return true;
        }

        if (!ent.Comp.Host.HasValue || !TryComp<BloodstreamComponent>(ent.Comp.Host.Value, out _))
            return false;

        UI.OpenUi((ent, uic), HealthAnalyzerUiKey.Key, ent.Owner);
        _analyzer.BeginAnalyzingEntity((ent, health), ent.Comp.Host.Value);

        return true;
    }

    public void TakeControlHost(Entity<CorticalBorerComponent> ent, CorticalBorerInfestedComponent infestedComp)
    {
        var (worm, comp) = ent;

        if (comp.Host is not { } host)
            return;

        // Make sure they aren't dead, would throw the worm into a ghost mode and just kill em
        if (TryComp<MobStateComponent>(host, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
            return;

        // If host willing we remove time restriction for control body
        if ((TryComp<MindContainerComponent>(host, out var mindContainer) &&
             mindContainer.HasMind ||
             HasComp<GhostRoleComponent>(host)) &&
            !comp.WillingHosts.Contains(host))
            infestedComp.ControlTimeEnd = _timing.CurTime + comp.ControlDuration;
        else
            infestedComp.ControlTimeEnd = null;

        if (_mind.TryGetMind(worm, out var wormMind, out _))
            infestedComp.BorerMindId = wormMind;
        else
            return;

        EntityUid? hostMindHolder = null;

        if (_mind.TryGetMind(host, out var controlledMind, out _))
        {
            infestedComp.OriginalMindId =
                controlledMind; // Set this var here just in case somehow the mind changes from when the infestation started

            // Temporary entity to hold host's original mind while borer controls the host
            var mindHolder = Spawn(HostMindContainer, MapCoordinates.Nullspace);
            _metaData.SetEntityName(mindHolder, Name(host));

            if (!Container.Insert(mindHolder, infestedComp.ControlContainer))
            {
                QueueDel(mindHolder);
                return;
            }

            _mind.TransferTo(controlledMind, mindHolder);
            hostMindHolder = mindHolder;
        }
        else
        {
            infestedComp.OriginalMindId = null;
        }

        comp.ControllingHost = true;
        _mind.TransferTo(wormMind, host);

        if (TryComp<GhostRoleComponent>(worm, out var ghostRole))
        {
            _ghost.UnregisterGhostRole((worm,
                ghostRole)); // Prevent players from taking the worm role once mind isn't in the worm
        }

        // Add end control action
        if (Actions.AddAction(host, EndControlAction) is { } actionEnd)
            infestedComp.RemoveAbilities.Add(actionEnd);

        // Voluntary hosts should be able to end control from their mind holder while body is controlled.
        if (comp.WillingHosts.Contains(host) && hostMindHolder is { } dummy &&
            Actions.AddAction(dummy, EndControlAction, worm) is { } borerActionEnd)
            infestedComp.RemoveAbilities.Add(borerActionEnd);

        var str = $"{ToPrettyString(worm)} has taken control over {ToPrettyString(host)}";

        Log.Info(str);
        _admin.Add(LogType.Mind,
            LogImpact.Medium,
            $"{ToPrettyString(worm)} has taken control over {ToPrettyString(host)}");
        _chat.SendAdminAlert(str);
    }

    public void EndControl(Entity<CorticalBorerInfestedComponent> host)
    {
        var (infested, infestedComp) = host;

        if (!TryComp<CorticalBorerComponent>(infestedComp.Borer, out var borerComp))
            return;

        if (!borerComp.ControllingHost)
            return;

        borerComp.ControllingHost = false;

        // Remove all the actions set to remove
        foreach (var ability in infestedComp.RemoveAbilities)
        {
            if (!TryComp<ActionComponent>(ability, out var actionComp) || actionComp.AttachedEntity is not { } attached)
                continue;

            Actions.RemoveAction(attached, ability);
        }

        infestedComp.RemoveAbilities = []; // Clear out the list

        if (TryComp<GhostRoleComponent>(infestedComp.Borer, out var ghostRole))
        {
            _ghost.RegisterGhostRole((infestedComp.Borer,
                ghostRole)); // re-enable the ghost role after you return to the body
        }

        // Return everyone to their own bodies
        if (!TerminatingOrDeleted(infestedComp.BorerMindId))
            _mind.TransferTo(infestedComp.BorerMindId, infestedComp.Borer);
        if (!TerminatingOrDeleted(infestedComp.OriginalMindId) && infestedComp.OriginalMindId.HasValue)
            _mind.TransferTo(infestedComp.OriginalMindId.Value, infested);

        infestedComp.ControlTimeEnd = null;
        foreach (var entity in infestedComp.ControlContainer.ContainedEntities.ToArray())
        {
            QueueDel(entity);
        }
    }

    private void OnForceSpeakMessage(Entity<CorticalBorerComponent> ent, ref CorticalBorerForceSpeakMessage args)
    {
        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (!TryGetHost(ent, out var host))
            return;

        if (!CanUseAbility(ent, host.Value))
            return;

        var message = args.Message.Trim();
        if (message.Length > ent.Comp.MaxForceSpeakLength)
            message = message[..ent.Comp.MaxForceSpeakLength];

        _chatSystem.TrySendInGameICMessage(host.Value,
            message,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal,
            ignoreActionBlocker: true,
            forced: true);

        _admin.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(ent):actor} forced host {ToPrettyString(host.Value):target} to say: '{message}'");
    }

    public void AskForWillingHost(Entity<CorticalBorerComponent> borer)
    {
        if (!TryGetHost(borer, out var host))
            return;

        if (!_actor.TryGetSession(host.Value, out var session) || session is null)
            return;

        if (borer.Comp.WillingHosts.Contains(host.Value))
            return;

        _euiManager.OpenEui(new CorticalBorerWillingHostEui(borer, host.Value, this), session);
    }

    public void HandleWillingHostChoice(Entity<CorticalBorerComponent> borer, EntityUid host, bool accepted)
    {
        if (accepted)
            borer.Comp.WillingHosts.Add(host);
        else
            borer.Comp.WillingHosts.Remove(host);

        var msgKey = accepted ? "cortical-borer-willing-result-yes" : "cortical-borer-willing-result-no";
        Popup.PopupEntity(Loc.GetString(msgKey, ("host", Name(host))), borer, borer, PopupType.Medium);

        _admin.Add(LogType.Action,
            LogImpact.Medium,
            $"Host {ToPrettyString(host):target} {(accepted ? "accepted" : "declined")} voluntary submission for {ToPrettyString(borer):actor}");

        Dirty(borer);
    }

    private void OnObjectivesTextGetInfo(Entity<GameRuleComponent> ent, ref ObjectivesTextGetInfoEvent args)
    {
        if (MetaData(ent).EntityPrototype?.ID != "CorticalBorerInfestation")
            return;

        var minds = new List<(EntityUid, string)>();
        var query = EntityQueryEnumerator<MindComponent>();
        while (query.MoveNext(out var mindUid, out var mind))
        {
            if (!mind.MindRoles.Any(HasComp<CorticalBorerRoleComponent>))
                continue;

            var name = mind.CharacterName;
            if (string.IsNullOrWhiteSpace(name))
            {
                var nameEntity = mind.OwnedEntity ?? mindUid;
                name = Name(nameEntity);
            }

            if (string.IsNullOrWhiteSpace(name))
                name = Name(mindUid);

            minds.Add((mindUid, FormattedMessage.EscapeText(name)));
        }

        if (minds.Count == 0)
            return;

        args.Minds = minds;
        args.AgentName = Loc.GetString("cortical-borer-round-end-agent-name");
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent args)
    {
        var lines = new List<string>();
        var borerQuery = EntityQueryEnumerator<CorticalBorerComponent>();

        while (borerQuery.MoveNext(out var borerUid, out var borer))
        {
            var borerName = Name(borerUid);
            if (string.IsNullOrWhiteSpace(borerName) && _mind.TryGetMind(borerUid, out var mindId, out var mind))
                borerName = mind.CharacterName ?? Name(mindId);

            var escapedBorerName = FormattedMessage.EscapeText(borerName);
            lines.Add(Loc.GetString("objectives-with-objectives",
                ("custody", string.Empty),
                ("title", escapedBorerName),
                ("agent", Loc.GetString("cortical-borer-round-end-agent-name"))));

            AddObjectiveResultLine(lines,
                Loc.GetString("cortical-borer-round-end-objective-survive"),
                _mobState.IsAlive(borerUid) ? 1f : 0f);

            AddObjectiveResultLine(lines,
                Loc.GetString("cortical-borer-round-end-objective-willing", ("current", borer.WillingHosts.Count), ("target", 3)),
                MathF.Min(1f, borer.WillingHosts.Count / 3f));

            AddObjectiveResultLine(lines,
                Loc.GetString("cortical-borer-round-end-objective-eggs", ("current", borer.EggsLaid), ("target", 5)),
                MathF.Min(1f, borer.EggsLaid / 5f));

            var names = borer.WillingHosts
                .Where(Exists)
                .Select(host => FormattedMessage.EscapeText(Name(host)))
                .ToArray();

            if (names.Length == 0)
                continue;

            lines.Add(Loc.GetString("cortical-borer-round-end-willing",
                ("borer", escapedBorerName),
                ("count", names.Length),
                ("hosts", string.Join(", ", names))));
        }

        if (lines.Count == 0)
            return;

        args.AddLine(string.Empty);
        foreach (var line in lines)
        {
            args.AddLine(line);
        }
    }

    private void AddObjectiveResultLine(List<string> lines, string objective, float progress)
    {
        var key = progress > 0.99f
            ? "objectives-objective-success"
            : "objectives-objective-fail";

        lines.Add($"- {Loc.GetString(key, ("objective", objective), ("progress", progress))}");
    }

    private void OnSurgicallyRemoved(Entity<CorticalBorerComponent> ent, ref CorticalBorerSurgicallyRemovedEvent args)
    {
        _stun.TryAddParalyzeDuration(ent, TimeSpan.FromSeconds(6));
    }

    private void OnMindAdded(Entity<CorticalBorerComponent> ent, ref MindAddedMessage args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        EnsureBorerObjectives(mindId, mindComp, ent.Comp.Objectives);
    }

    private void OnMindRemoved(Entity<CorticalBorerComponent> ent, ref MindRemovedMessage args)
    {
        if (!ent.Comp.ControllingHost)
            TryEjectBorer(ent); // No storing them in hosts if you don't have a soul
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } ownedEntity ||
            !TryComp<CorticalBorerComponent>(ownedEntity, out var borer))
            return;

        EnsureBorerObjectives(args.MindId, args.Mind, borer.Objectives);
    }

    private void OnHostPolymorphed(Entity<CorticalBorerInfestedComponent> ent, ref PolymorphedEvent args)
    {
        if (!TryComp<CorticalBorerComponent>(ent.Comp.Borer, out var borerComp) || !borerComp.Host.HasValue)
            return;

        _admin.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(ent.Comp.Borer):actor} was ejected because host {ToPrettyString(ent):target} polymorphed into {ToPrettyString(args.NewEntity)}");

        TryEjectBorer((ent.Comp.Borer, borerComp));
    }

    private void OnHostParentChanged(Entity<CorticalBorerInfestedComponent> ent, ref EntParentChangedMessage args)
    {
        if (!TryComp<CorticalBorerComponent>(ent.Comp.Borer, out var borerComp) || !borerComp.Host.HasValue)
            return;

        if (Transform(ent).MapID != MapId.Nullspace)
            return;

        _admin.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(ent.Comp.Borer):actor} was ejected because host {ToPrettyString(ent):target} moved to nullspace");

        TryEjectBorer((ent.Comp.Borer, borerComp));
    }

    private void OnBorerTerminating(Entity<CorticalBorerComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.Host.HasValue)
            return;

        if (!TryComp<CorticalBorerInfestedComponent>(ent.Comp.Host.Value, out var infested) || infested.Borer != ent)
            return;

        if (ent.Comp.ControllingHost)
            EndControl((ent.Comp.Host.Value, infested));

        ent.Comp.WillingHosts.Clear();
        RemCompDeferred<CorticalBorerInfestedComponent>(ent.Comp.Host.Value);
    }

    public void HandleHostTerminating(Entity<CorticalBorerInfestedComponent> infected)
    {
        if (!TryComp<CorticalBorerComponent>(infected.Comp.Borer, out var borerComp))
            return;

        if (borerComp.ControllingHost)
            EndControl(infected);

        borerComp.Host = null;

        EnsureRegistryComponents((infected.Comp.Borer.Owner, borerComp), borerComp.RemoveOnInfest);
        RemoveRegistryComponents((infected.Comp.Borer.Owner, borerComp), borerComp.AddOnInfest);
    }

    private void EnsureBorerObjectives(EntityUid mindId, MindComponent mindComp, List<EntProtoId> objectives)
    {
        if (!mindComp.MindRoles.Any(HasComp<CorticalBorerRoleComponent>))
            return;

        foreach (var objective in objectives)
        {
            if (mindComp.Objectives.Any(uid =>
                {
                    var objectiveProto = MetaData(uid).EntityPrototype;
                    return objectiveProto is not null && objectiveProto.ID == objective;
                }))
                continue;

            _mind.TryAddObjective(mindId, mindComp, objective);
        }
    }
}
