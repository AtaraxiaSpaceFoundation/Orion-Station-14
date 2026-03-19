using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Components;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Item;
using Content.Shared.Research.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Research.Systems;

public sealed class ExperimentalDestructiveScannerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, ExperimentalDestructiveScannerPerformMessage>(OnPerform);
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<ExperimentalDestructiveScannerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ExperimentalDestructiveScannerComponent> ent, ref ComponentStartup args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Idle);
    }

    private void OnUiOpened(Entity<ExperimentalDestructiveScannerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnOpenServerMenu(Entity<ExperimentalDestructiveScannerComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        _ui.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }

    private void OnPointsChanged(Entity<ExperimentalDestructiveScannerComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        if (_ui.IsUiOpen(ent.Owner, ExperimentalDestructiveScannerUiKey.Key))
            UpdateUi(ent);
    }

    private void OnRegistrationChanged(Entity<ExperimentalDestructiveScannerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnPerform(Entity<ExperimentalDestructiveScannerComponent> ent, ref ExperimentalDestructiveScannerPerformMessage args)
    {
        if (ent.Comp.IsProcessing)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-busy");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        if (!TryResolveServer(ent, out var server))
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var xform = Transform(ent);
        var items = new List<EntityUid>();

        if (xform.GridUid is { } gridUid &&
            TryComp(gridUid, out MapGridComponent? gridComp) &&
            _maps.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef))
        {
            items = _lookup.GetLocalEntitiesIntersecting(tileRef, 0f)
                .Where(uid => uid != ent.Owner
                              && HasComp<ItemComponent>(uid)
                              && !HasComp<ResearchClientComponent>(uid)
                              && !_container.TryGetContainingContainer(uid, out _))
                .Distinct()
                .ToList();
        }

        if (items.Count == 0)
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-items");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        var scannedItems = new List<EntityUid>();
        foreach (var item in items)
        {
            if (_container.Insert(item, itemContainer))
                scannedItems.Add(item);
        }

        if (scannedItems.Count == 0)
        {
            ent.Comp.LastSubject = string.Empty;
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-items");
            _audio.PlayPvs(ent.Comp.FailureSound, ent, ent.Comp.AudioParams);
            UpdateUi(ent);
            return;
        }

        var actor = args.Actor;

        ent.Comp.IsProcessing = true;
        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Down);
        ent.Comp.LastSubject = string.Join(", ", scannedItems.Select(uid => Name(uid)));
        ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-processing", ("count", scannedItems.Count));
        _research.LogNetworkEvent(server, "experimental-destructive-scanner", Loc.GetString("research-netlog-experimental-destructive-scanner-started", ("count", scannedItems.Count), ("user", _research.GetResearchLogUserName(args.Actor))), args.Actor);
        UpdateUi(ent);

        Timer.Spawn(ent.Comp.CapsuleStepDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent) || !ent.Comp.IsProcessing)
                    return;

                UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Scanning);
            });

        Timer.Spawn(ent.Comp.ScanDuration, () => CompleteScan(ent, server, scannedItems, actor));
    }

    private void CompleteScan(Entity<ExperimentalDestructiveScannerComponent> ent, EntityUid server, List<EntityUid> scannedItems, EntityUid? user)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var changedAny = false;
        var completedExperiments = new HashSet<string>();

        foreach (var item in scannedItems)
        {
            if (TerminatingOrDeleted(item))
                continue;

            if (!_research.TryProgressExperimentsWithEntity(server, item, null, out var changed, out var completed, out _, source: ExperimentSourceFlags.MachineScanner))
                continue;

            changedAny |= changed;
            foreach (var completedExperiment in completed)
            {
                completedExperiments.Add(completedExperiment);
            }
        }

        var completedCount = completedExperiments.Count;

        ent.Comp.IsProcessing = false;
        UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Up);

        Timer.Spawn(ent.Comp.CapsuleStepDuration,
            () =>
            {
                if (TerminatingOrDeleted(ent) || ent.Comp.IsProcessing)
                    return;

                var itemContainer = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
                _container.EmptyContainer(itemContainer, true, Transform(ent).Coordinates);

                UpdateAppearance(ent, ExperimentalDestructiveScannerVisualState.Idle);
            });

        if (completedCount > 0)
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-completed-named",
                ("count", completedCount),
                ("experiments", string.Join(", ", completedExperiments.Select(GetExperimentName))));
        else if (changedAny)
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-progressed");
        else
            ent.Comp.LastResult = Loc.GetString("research-machine-experimental-destructive-scanner-no-matching-experiment");

        _chat.TrySendInGameICMessage(ent.Owner, Loc.GetString("research-machine-experimental-destructive-scanner-chat-result", ("result", ent.Comp.LastResult)), InGameICChatType.Speak, false);
        _audio.PlayPvs(changedAny || completedCount > 0
            ? ent.Comp.SuccessSound
            : ent.Comp.FailureSound,
            ent,
            ent.Comp.AudioParams);

        _research.LogNetworkEvent(server,
            "experimental-destructive-scanner",
            Loc.GetString("research-netlog-experimental-destructive-scanner-result",
                ("completed", completedCount),
                ("progressed", Loc.GetString(changedAny
                    ? "research-netlog-experimental-destructive-scanner-progress-yes"
                    : "research-netlog-experimental-destructive-scanner-progress-no")),
                ("user", _research.GetResearchLogUserName(user))),
            user);
        UpdateUi(ent);
    }

    private string GetExperimentName(string experimentId)
    {
        return _prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype)
            ? Loc.GetString(prototype.Name)
            : experimentId;
    }

    private bool TryResolveServer(Entity<ExperimentalDestructiveScannerComponent> ent, out EntityUid server)
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

    private void UpdateAppearance(Entity<ExperimentalDestructiveScannerComponent> ent, ExperimentalDestructiveScannerVisualState state)
    {
        _appearance.SetData(ent.Owner, ExperimentalDestructiveScannerVisuals.State, state);
    }

    private void UpdateUi(Entity<ExperimentalDestructiveScannerComponent> ent)
    {
        string? serverName = null;
        var pointBalances = new List<ResearchPointAmount>();
        var experiments = new List<ResearchMachineExperimentUiData>();

        if (_research.TryGetClientServer(ent.Owner, out var serverUid, out var server))
        {
            serverName = server.ServerName;
            pointBalances = server.PointBalances.ToList();

            if (TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
            {
                foreach (var experimentId in db.ActiveExperiments)
                {
                    if (!_prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype))
                        continue;

                    var progress = db.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experimentId);
                    var target = progress.Target > 0 ? progress.Target : Math.Max(1, prototype.Objective.Target);
                    var objective = Loc.GetString($"research-experiment-objective-{prototype.Objective.Kind.ToString().ToLowerInvariant()}");
                    experiments.Add(new ResearchMachineExperimentUiData(
                        prototype.ID,
                        Loc.GetString(prototype.Name),
                        Loc.GetString(prototype.Description),
                        progress.Progress,
                        target,
                        objective));
                }
            }
        }

        var status = ent.Comp.IsProcessing
            ? Loc.GetString("research-machine-experimental-destructive-scanner-state-processing")
            : Loc.GetString("research-machine-common-none");

        var state = new ExperimentalDestructiveScannerBoundInterfaceState(
            serverName,
            pointBalances,
            ent.Comp.LastSubject,
            ent.Comp.LastResult,
            experiments,
            status);

        _ui.SetUiState(ent.Owner, ExperimentalDestructiveScannerUiKey.Key, state);
    }
}
