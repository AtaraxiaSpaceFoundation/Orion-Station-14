using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Research.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
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
        RaiseLocalEvent(ent.Owner, new ConsoleServerSelectionMessage(), true);
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
        ent.Comp.SelectedMethod = TryComp<ResearchAnalyzableComponent>(used, out var analyzable)
            ? GetAvailableMethods(analyzable).FirstOrDefault()
            : null;

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
            ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-busy");
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

        if (!TryComp<ResearchAnalyzableComponent>(used, out var analyzable))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-invalid-item");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
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

        var methods = GetAvailableMethods(analyzable);
        var method = ent.Comp.SelectedMethod;
        if (string.IsNullOrWhiteSpace(method) || !methods.Contains(method))
        {
            method = methods.FirstOrDefault();
            ent.Comp.SelectedMethod = method;
        }

        if (string.IsNullOrWhiteSpace(method))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-unsupported-method");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var server = client.Server ?? _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault().Owner;
        if (server == EntityUid.Invalid)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!analyzable.MethodPointRewards.TryGetValue(method, out var rewards))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-unsupported-method");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        foreach (var reward in rewards)
        {
            _research.ModifyServerPoints(server, reward.Type, reward.Amount);
        }

        foreach (var technology in analyzable.RevealTechnologies)
        {
            _research.RevealTechnology(server, technology);
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

        _research.LogNetworkEvent(server,
            "destructive-analyzer",
            Loc.GetString("research-netlog-destructive-analysis-result",
                ("method", LocalizeMethod(method)),
                ("channels", rewards.Count),
                ("subject", Name(used))),
            args.Actor);

        ent.Comp.IsProcessing = true;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Deconstructing);
        ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-processing", ("count", 1));
        UpdateUi(ent);

        Timer.Spawn(ent.Comp.DeconstructAnimationDuration,
            () =>
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
                ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-success", ("channels", rewards.Count));
                Del(used);
                ent.Comp.InsertedItem = null;
                UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
                _audio.PlayPvs(ent.Comp.SuccessSound, ent, ent.Comp.AudioParams);
                UpdateUi(ent);
                _popup.PopupEntity(Loc.GetString("research-destructive-analyzer-success"), ent, PopupType.SmallCaution);
            });
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
            Transform(item).Coordinates = Transform(ent).Coordinates;

        ent.Comp.InsertedItem = null;
        ent.Comp.SelectedMethod = null;
        ent.Comp.LastResult = string.Empty;
        ent.Comp.LastItemAnalyzed = false;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Idle);
        UpdateUi(ent);
    }

    private void UpdateAppearance(Entity<DestructiveAnalyzerComponent> ent, DestructiveAnalyzerVisualState state)
    {
        _appearance.SetData(ent.Owner, DestructiveAnalyzerVisuals.State, state);
    }

    private static List<string> GetAvailableMethods(ResearchAnalyzableComponent analyzable)
    {
        if (analyzable.SupportedMethods.Count > 0)
            return analyzable.SupportedMethods;

        if (analyzable.MethodPointRewards.Count > 0)
            return analyzable.MethodPointRewards.Keys.ToList();

        return new List<string>();
    }

    private string LocalizeMethod(string methodId)
    {
        var key = $"research-machine-destructive-method-{methodId.ToLowerInvariant()}";
        return Loc.TryGetString(key, out var localized) ? localized : methodId;
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

        if (ent.Comp.InsertedItem is { } used && TryComp<ResearchAnalyzableComponent>(used, out var analyzable))
            methods = GetAvailableMethods(analyzable);

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
