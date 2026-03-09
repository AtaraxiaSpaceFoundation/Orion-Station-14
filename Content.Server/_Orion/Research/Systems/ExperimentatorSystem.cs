using System.Linq;
using System.Numerics;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Components;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Item;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Research.Systems;

public sealed class ExperimentatorSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    private static readonly TimeSpan ScanDuration = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperimentatorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ExperimentatorComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<ExperimentatorComponent, ExperimentScannerPerformMessage>(OnPerform);
        SubscribeLocalEvent<ExperimentatorComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<ExperimentatorComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
    }

    private void OnUiOpened(Entity<ExperimentatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnOpenServerMenu(Entity<ExperimentatorComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        RaiseLocalEvent(ent.Owner, new ConsoleServerSelectionMessage(), true);
    }

    private void OnPointsChanged(Entity<ExperimentatorComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        if (_ui.IsUiOpen(ent.Owner, ExperimentatorUiKey.Key))
            UpdateUi(ent);
    }

    private void OnRegistrationChanged(Entity<ExperimentatorComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnPerform(Entity<ExperimentatorComponent> ent, ref ExperimentScannerPerformMessage args)
    {
        if (ent.Comp.IsProcessing)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-busy");
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

        var xform = Transform(ent);
        var items = new List<EntityUid>();

        if (xform.GridUid is { } gridUid &&
            TryComp(gridUid, out MapGridComponent? gridComp) &&
            _maps.TryGetTileRef(gridUid, gridComp, xform.Coordinates, out var tileRef))
        {
            items = _lookup.GetLocalEntitiesIntersecting(tileRef, 0f)
                .Where(uid => uid != ent.Owner && HasComp<ItemComponent>(uid) && !HasComp<ResearchClientComponent>(uid))
                .Distinct()
                .ToList();
        }

        if (items.Count == 0)
        {
            ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-no-items");
            UpdateUi(ent);
            return;
        }

        var hiddenItems = new List<(EntityUid Uid, EntityCoordinates Original)>(items.Count);
        foreach (var item in items)
        {
            hiddenItems.Add((item, Transform(item).Coordinates));
            _transform.SetCoordinates(item, new EntityCoordinates(ent.Owner, Vector2.Zero));
        }

        ent.Comp.IsProcessing = true;
        ent.Comp.LastSubject = string.Join(", ", items.Select(uid => Name(uid)));
        ent.Comp.LastResult = Loc.GetString("research-machine-experiment-scanner-processing", ("count", items.Count));
        _research.LogNetworkEvent(server, "experiment-scanner", Loc.GetString("research-netlog-experiment-scanner-started", ("count", items.Count)));
        UpdateUi(ent);

        Timer.Spawn(ScanDuration, () => CompleteScan(ent, server, hiddenItems));
    }

    private void CompleteScan(Entity<ExperimentatorComponent> ent, EntityUid server, List<(EntityUid Uid, EntityCoordinates Original)> hiddenItems)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var changedAny = false;
        var completedCount = 0;

        foreach (var (item, original) in hiddenItems)
        {
            if (TerminatingOrDeleted(item))
                continue;

            _transform.SetCoordinates(item, original);

            if (!_research.TryProgressExperimentsWithEntity(server, item, null, out var changed, out var completed))
                continue;

            changedAny |= changed;
            completedCount += completed.Count;
        }

        ent.Comp.IsProcessing = false;

        if (completedCount > 0)
            ent.Comp.LastResult = Loc.GetString("research-machine-experimentator-completed", ("count", completedCount));
        else if (changedAny)
            ent.Comp.LastResult = Loc.GetString("research-machine-experimentator-progressed");
        else
            ent.Comp.LastResult = Loc.GetString("research-machine-experimentator-no-matching-experiment");

        _research.LogNetworkEvent(server, "experiment-scanner", Loc.GetString("research-netlog-experiment-scanner-result", ("completed", completedCount), ("progressed", changedAny)));
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ExperimentatorComponent> ent)
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
                    var objective = Loc.GetString($"research-experiment-objective-{prototype.Objective.Kind.ToString().ToLowerInvariant()}");
                    experiments.Add(new ResearchMachineExperimentUiData(
                        prototype.ID,
                        Loc.GetString(prototype.Name),
                        Loc.GetString(prototype.Description),
                        progress.Progress,
                        progress.Target,
                        objective));
                }
            }
        }

        var state = new ExperimentatorBoundInterfaceState(
            serverName,
            pointBalances,
            ent.Comp.LastSubject,
            ent.Comp.LastResult,
            experiments,
            ent.Comp.IsProcessing ? Loc.GetString("research-machine-experiment-scanner-state-processing") : null);

        _ui.SetUiState(ent.Owner, ExperimentatorUiKey.Key, state);
    }
}
