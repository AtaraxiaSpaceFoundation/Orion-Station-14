using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Chat;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Research.Systems;

public sealed class DestructiveAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DestructiveAnalyzerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerSelectMethodMessage>(OnSelectMethod);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerRunMessage>(OnRun);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerEjectMessage>(OnEject);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<DestructiveAnalyzerComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
    }

    private void OnUiOpened(Entity<DestructiveAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnOpenServerMenu(Entity<DestructiveAnalyzerComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        _ui.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }

    private void OnPointsChanged(Entity<DestructiveAnalyzerComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        if (_ui.IsUiOpen(ent.Owner, DestructiveAnalyzerUiKey.Key))
            UpdateUi(ent);
    }

    private void OnRegistrationChanged(Entity<DestructiveAnalyzerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnAfterInteractUsing(Entity<DestructiveAnalyzerComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.InsertedItem != null)
            return;

        var used = args.Used;
        var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        if (!_container.Insert(used, itemContainer))
            return;

        ent.Comp.InsertedItem = used;
        ent.Comp.LastItemAnalyzed = false;
        ent.Comp.IsProcessing = false;
        ent.Comp.LastSubject = Name(used);
        ent.Comp.LastResult = Loc.GetString("research-machine-destructive-item-loaded");
        ent.Comp.SelectedMethod = null;

        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Inserting);
        Timer.Spawn(ent.Comp.InsertAnimationDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent) || ent.Comp.InsertedItem != used)
                    return;

                UpdateAppearance(ent, DestructiveAnalyzerVisualState.Loaded);
            });

        UpdateUi(ent);
        args.Handled = true;
    }

    private void OnSelectMethod(Entity<DestructiveAnalyzerComponent> ent, ref DestructiveAnalyzerSelectMethodMessage args)
    {
        ent.Comp.SelectedMethod = args.MethodId;
        UpdateUi(ent);
    }

    private void OnRun(Entity<DestructiveAnalyzerComponent> ent, ref DestructiveAnalyzerRunMessage args)
    {
        if (ent.Comp.IsProcessing)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-busy");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (ent.Comp.LastItemAnalyzed)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-already-analyzed");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (ent.Comp.InsertedItem is not { } used)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-no-item");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!TryResolveServer(ent, out var server))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!_container.TryGetContainingContainer(used, out var containing) || containing.Owner != ent.Owner)
        {
            ent.Comp.InsertedItem = null;
            ent.Comp.SelectedMethod = null;
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-no-item");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
            UpdateUi(ent);
            return;
        }

        if (TryComp<MobStateComponent>(used, out var mobState) && mobState.CurrentState == MobState.Alive)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-living-subject-blocked");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        string rewardSummary;
        if (TryComp<ResearchAnalyzableComponent>(used, out var analyzable))
        {
            if (!TryRunAnalyzableMethod(ent, used, server, analyzable, args.Actor, out rewardSummary))
                return;
        }
        else
        {
            if (!TryRunDiscoveryRevealMethod(ent, used, server, args.Actor, out rewardSummary))
                return;
        }

        ent.Comp.IsProcessing = true;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Deconstructing);
        ent.Comp.LastResult = Loc.GetString("research-machine-destructive-processing", ("count", 1));
        UpdateUi(ent);

        Timer.Spawn(ent.Comp.DeconstructAnimationDuration,
            () => CompleteAnalysis(ent, used, rewardSummary));
    }

    private bool TryRunAnalyzableMethod(Entity<DestructiveAnalyzerComponent> ent,
        EntityUid used,
        EntityUid server,
        ResearchAnalyzableComponent analyzable,
        EntityUid actor,
        out string rewardSummary)
    {
        rewardSummary = string.Empty;
        var methods = GetAvailableMethods(analyzable);
        var method = ent.Comp.SelectedMethod;
        if (string.IsNullOrWhiteSpace(method) || !methods.Contains(method))
        {
            method = methods.FirstOrDefault();
            ent.Comp.SelectedMethod = method;
        }

        if (string.IsNullOrWhiteSpace(method) || !analyzable.MethodPointRewards.TryGetValue(method, out var rewards))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-unsupported-method");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return false;
        }

        var stackMultiplier = 1;
        if (TryComp<StackComponent>(used, out var stack))
            stackMultiplier = stack.Count;

        foreach (var reward in rewards)
        {
            _research.ModifyServerPoints(server, reward.Type, reward.Amount * stackMultiplier);
        }

        foreach (var technology in analyzable.RevealTechnologies)
        {
            _research.RevealTechnology(server, technology, actor);
        }

        foreach (var technology in analyzable.UnlockTechnologies)
        {
            _research.AddTechnology(server, technology);
        }

        foreach (var actionId in analyzable.ExperimentActions)
        {
            _research.TryProgressExperimentsByAction(server, actionId);
        }

        if (!string.IsNullOrWhiteSpace(analyzable.DiscoveryTrigger))
            _research.TriggerDiscovery(server, analyzable.DiscoveryTrigger!);

        rewardSummary = BuildAnalyzableRewardSummary(rewards, stackMultiplier, analyzable);

        _research.LogNetworkEvent(server,
            "destructive-analyzer",
            Loc.GetString("research-netlog-destructive-analysis-result",
                ("method", LocalizeMethod(method)),
                ("subject", Name(used)),
                ("result", rewardSummary),
                ("user", _research.GetResearchLogUserName(actor))),
            actor);

        return true;
    }

    private bool TryRunDiscoveryRevealMethod(Entity<DestructiveAnalyzerComponent> ent, EntityUid used, EntityUid server, EntityUid actor, out string rewardSummary)
    {
        rewardSummary = string.Empty;
        var methods = GetDiscoveryRevealMethods(used, server);
        var method = ent.Comp.SelectedMethod;
        if (string.IsNullOrWhiteSpace(method) || !methods.Contains(method))
        {
            method = methods.FirstOrDefault();
            ent.Comp.SelectedMethod = method;
        }

        if (string.IsNullOrWhiteSpace(method) || !TryGetRevealTechnologyFromMethod(method, out var technologyId))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-invalid-item");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return false;
        }

        _research.RevealTechnology(server, technologyId, actor);
        rewardSummary = GetTechnologyName(technologyId);

        _research.LogNetworkEvent(server,
            "destructive-analyzer",
            Loc.GetString("research-netlog-destructive-analysis-result",
                ("method", LocalizeMethod(method)),
                ("subject", Name(used)),
                ("result", Loc.GetString("research-machine-destructive-result-revealed-tech", ("technology", rewardSummary))),
                ("user", _research.GetResearchLogUserName(actor))),
            actor);

        return true;
    }

    private void CompleteAnalysis(Entity<DestructiveAnalyzerComponent> ent, EntityUid used, string rewardSummary)
    {
        if (TerminatingOrDeleted(ent))
            return;

        ent.Comp.IsProcessing = false;

        if (TerminatingOrDeleted(used))
        {
            ent.Comp.InsertedItem = null;
            UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
            UpdateUi(ent);
            return;
        }

        ent.Comp.LastItemAnalyzed = true;
        ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-success", ("result", rewardSummary));
        Del(used);
        ent.Comp.InsertedItem = null;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
        _audio.PlayPvs(ent.Comp.SuccessSound, ent, ent.Comp.AudioParams);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("research-destructive-analyzer-success"), ent, PopupType.SmallCaution);
        _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("research-machine-destructive-chat-result", ("result", rewardSummary)), InGameICChatType.Speak, false);
    }

    private void OnEject(Entity<DestructiveAnalyzerComponent> ent, ref DestructiveAnalyzerEjectMessage args)
    {
        if (ent.Comp.IsProcessing || ent.Comp.InsertedItem == null)
            return;

        var item = ent.Comp.InsertedItem.Value;
        var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);

        if (!_container.Remove(item, itemContainer))
            return;

        if (!_hands.TryPickupAnyHand(args.Actor, item))
            _transform.SetCoordinates(item, Transform(ent).Coordinates);

        ent.Comp.InsertedItem = null;
        ent.Comp.SelectedMethod = null;
        ent.Comp.LastResult = string.Empty;
        ent.Comp.LastItemAnalyzed = false;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
        UpdateUi(ent);
    }

    private bool TryResolveServer(Entity<DestructiveAnalyzerComponent> ent, out EntityUid server)
    {
        server = EntityUid.Invalid;

        if (TryComp<ResearchClientComponent>(ent, out var client) && client.Server is { } selected)
        {
            server = selected;
            return true;
        }

        var fallback = _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault();
        if (fallback.Owner == EntityUid.Invalid)
            return false;

        server = fallback.Owner;
        return true;
    }

    private void UpdateAppearance(Entity<DestructiveAnalyzerComponent> ent, DestructiveAnalyzerVisualState state)
    {
        _appearance.SetData(ent.Owner, DestructiveAnalyzerVisuals.State, state);
    }

    private static List<string> GetAvailableMethods(ResearchAnalyzableComponent analyzable)
    {
        if (analyzable.SupportedMethods.Count > 0)
        {
            return analyzable.SupportedMethods
                .Where(analyzable.MethodPointRewards.ContainsKey)
                .ToList();
        }

        if (analyzable.MethodPointRewards.Count > 0)
            return analyzable.MethodPointRewards.Keys.ToList();

        return new List<string>();
    }

    private string BuildAnalyzableRewardSummary(List<ResearchPointAmount> rewards, int stackMultiplier, ResearchAnalyzableComponent analyzable)
    {
        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var reward in rewards)
        {
            var amount = reward.Amount * stackMultiplier;
            totals.TryGetValue(reward.Type, out var existing);
            totals[reward.Type] = existing + amount;
        }

        var segments = new List<string>();
        if (totals.Count > 0)
        {
            var pointsText = totals
                .OrderBy(pair => pair.Key)
                .Select(pair => Loc.GetString("research-machine-destructive-result-points-entry",
                    ("type", LocalizePointType(pair.Key)),
                    ("amount", pair.Value)));
            segments.Add(Loc.GetString("research-machine-destructive-result-points", ("points", string.Join(", ", pointsText))));
        }

        if (analyzable.RevealTechnologies.Count > 0)
        {
            var technologies = analyzable.RevealTechnologies.Select(GetTechnologyName);
            segments.Add(Loc.GetString("research-machine-destructive-result-revealed-tech",
                ("technology", string.Join(", ", technologies))));
        }

        if (analyzable.UnlockTechnologies.Count > 0)
        {
            var technologies = analyzable.UnlockTechnologies.Select(GetTechnologyName);
            segments.Add(Loc.GetString("research-machine-destructive-result-unlocked-tech",
                ("technology", string.Join(", ", technologies))));
        }

        if (segments.Count == 0)
            return Loc.GetString("research-machine-destructive-result-generic");

        return string.Join("; ", segments);
    }

    private string GetTechnologyName(string technologyId)
    {
        if (_prototype.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return Loc.GetString(technology.Name);

        return technologyId;
    }

    private string LocalizePointType(string type)
    {
        var key = $"research-point-type-{type.ToLowerInvariant()}";
        return _loc.TryGetString(key, out var localized) ? localized : type;
    }

    private string LocalizeMethod(string methodId)
    {
        if (TryGetRevealTechnologyFromMethod(methodId, out var technologyId))
        {
            var technologyName = technologyId;
            if (_prototype.TryIndex<TechnologyPrototype>(technologyId, out var technology))
                technologyName = Loc.GetString(technology.Name);

            return Loc.GetString("research-machine-destructive-method-reveal-technology", ("technology", technologyName));
        }

        var key = $"research-machine-destructive-method-{methodId.ToLowerInvariant()}";
        return _loc.TryGetString(key, out var localized)
        ? localized
        : Loc.GetString("research-machine-destructive-method-unknown");
    }

    private static bool TryGetRevealTechnologyFromMethod(string methodId, out string technologyId)
    {
        const string prefix = "reveal:";
        if (!methodId.StartsWith(prefix, StringComparison.Ordinal))
        {
            technologyId = string.Empty;
            return false;
        }

        technologyId = methodId[prefix.Length..];
        return !string.IsNullOrWhiteSpace(technologyId);
    }

    private List<string> GetDiscoveryRevealMethods(EntityUid used, EntityUid server)
    {
        var technologies = _research.GetHiddenTechnologiesForRequiredItem(server, used);
        return technologies.Select(technology => $"reveal:{technology}").ToList();
    }

    private void UpdateUi(Entity<DestructiveAnalyzerComponent> ent)
    {
        string? serverName = null;
        var pointBalances = new List<ResearchPointAmount>();
        var methods = new List<string>();

        if (_research.TryGetClientServer(ent.Owner, out _, out var server))
        {
            serverName = server.ServerName;
            pointBalances = server.PointBalances.ToList();
        }

        if (ent.Comp.InsertedItem is { } used)
        {
            if (TryComp<ResearchAnalyzableComponent>(used, out var analyzable))
                methods = GetAvailableMethods(analyzable);
            else if (_research.TryGetClientServer(ent.Owner, out var serverUid, out _))
                methods = GetDiscoveryRevealMethods(used, serverUid.Value);

            if (ent.Comp.SelectedMethod == null || !methods.Contains(ent.Comp.SelectedMethod))
                ent.Comp.SelectedMethod = methods.FirstOrDefault();
        }

        var state = new DestructiveAnalyzerBoundInterfaceState(
            serverName,
            pointBalances,
            ent.Comp.LastSubject,
            ent.Comp.LastResult,
            ent.Comp.InsertedItem is { } item ? Name(item) : null,
            ent.Comp.InsertedItem is { } inserted ? GetNetEntity(inserted) : null,
            ent.Comp.SelectedMethod,
            methods);

        _ui.SetUiState(ent.Owner, DestructiveAnalyzerUiKey.Key, state);
    }
}
