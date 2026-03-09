using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Research.Systems;

public sealed class DestructiveAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private static readonly TimeSpan InsertAnimationDuration = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan DeconstructAnimationDuration = TimeSpan.FromSeconds(1.0);

    public override void Initialize()
    {
        SubscribeLocalEvent<DestructiveAnalyzerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerSelectMethodMessage>(OnSelectMethod);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, DestructiveAnalyzerRunMessage>(OnRun);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<DestructiveAnalyzerComponent> ent, ref ComponentStartup args)
    {
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

        var used = args.Used;
        ent.Comp.InsertedItem = used;
        ent.Comp.LastItemAnalyzed = false;
        ent.Comp.IsProcessing = false;
        ent.Comp.LastSubject = Name(used);
        ent.Comp.LastResult = Loc.GetString("research-machine-destructive-item-loaded");
        ent.Comp.SelectedMethod = TryComp<ResearchAnalyzableComponent>(used, out var analyzable)
            ? GetAvailableMethods(analyzable).FirstOrDefault()
            : null;

        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Inserting);
        Timer.Spawn(InsertAnimationDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent))
                    return;

                if (ent.Comp.InsertedItem != used)
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
            UpdateUi(ent);
            return;
        }

        if (ent.Comp.LastItemAnalyzed)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-already-analyzed");
            UpdateUi(ent);
            return;
        }

        if (ent.Comp.InsertedItem is not { } used)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-no-item");
            UpdateUi(ent);
            return;
        }

        if (!TryComp<ResearchAnalyzableComponent>(used, out var analyzable))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-invalid-item");
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
            UpdateUi(ent);
            return;
        }

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var server = client.Server ?? _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault().Owner;
        if (server == EntityUid.Invalid)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            UpdateUi(ent);
            return;
        }

        if (!analyzable.MethodPointRewards.TryGetValue(method, out var rewards))
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-unsupported-method");
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
            _research.UnlockTechnology(server, technology, null, true);
        }

        if (!string.IsNullOrWhiteSpace(analyzable.DiscoveryTrigger))
            _research.TriggerDiscovery(server, analyzable.DiscoveryTrigger!);

        _research.LogNetworkEvent(server,
            "destructive-analyzer",
            Loc.GetString("research-netlog-destructive-analysis-result",
                ("method", method),
                ("channels", rewards.Count)));

        ent.Comp.IsProcessing = true;
        UpdateAppearance(ent, DestructiveAnalyzerVisualState.Deconstructing);
        ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-processing", ("count", 1));
        UpdateUi(ent);

        Timer.Spawn(DeconstructAnimationDuration,
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
            UpdateUi(ent);
            _popup.PopupEntity(Loc.GetString("research-destructive-analyzer-success"), ent, PopupType.SmallCaution);
        });
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
            ent.Comp.InsertedItem is { } item ? ToPrettyString(item) : null,
            ent.Comp.InsertedItem is { } inserted ? GetNetEntity(inserted) : null,
            ent.Comp.SelectedMethod,
            methods);

        _ui.SetUiState(ent.Owner, DestructiveAnalyzerUiKey.Key, state);
    }
}
