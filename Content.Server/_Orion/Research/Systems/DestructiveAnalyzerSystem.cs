using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Orion.Research.Systems;

public sealed class DestructiveAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DestructiveAnalyzerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<DestructiveAnalyzerComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
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
        if (!_ui.IsUiOpen(ent.Owner, DestructiveAnalyzerUiKey.Key))
            return;

        UpdateUi(ent);
    }

    private void OnRegistrationChanged(Entity<DestructiveAnalyzerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<DestructiveAnalyzerComponent> ent)
    {
        string? serverName = null;
        var pointBalances = new List<ResearchPointAmount>();
        if (_research.TryGetClientServer(ent.Owner, out _, out var server))
        {
            serverName = server.ServerName;
            pointBalances = server.PointBalances.ToList();
        }

        var state = new DestructiveAnalyzerBoundInterfaceState(serverName, pointBalances, ent.Comp.LastSubject, ent.Comp.LastResult);
        _ui.SetUiState(ent.Owner, DestructiveAnalyzerUiKey.Key, state);
    }

    private void OnAfterInteractUsing(Entity<DestructiveAnalyzerComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;

        if (!TryComp<ResearchAnalyzableComponent>(used, out var analyzable) || analyzable.DestructiveReward.Count == 0)
        {
            ent.Comp.LastSubject = ToPrettyString(used);
            ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-invalid-item");
            UpdateUi(ent);
            return;
        }

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var server = client.Server ?? _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault().Owner;
        if (server == EntityUid.Invalid)
        {
            ent.Comp.LastSubject = ToPrettyString(used);
            ent.Comp.LastResult = Loc.GetString("research-machine-common-no-server");
            UpdateUi(ent);
            return;
        }

        foreach (var reward in analyzable.DestructiveReward)
        {
            _research.ModifyServerPoints(server, reward.Type, reward.Amount);
        }

        _research.TryProgressExperimentsWithEntity(server, used, args.User);
        foreach (var action in analyzable.ExperimentActions)
        {
            _research.TryProgressExperimentsByAction(server, action);
        }

        _research.NotifyDiscoveryEvent(server,
            new ResearchSystem.DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = used,
            Machine = ent,
            User = args.User,
        });

        if (!string.IsNullOrWhiteSpace(analyzable.DiscoveryTrigger))
            _research.TriggerDiscovery(server, analyzable.DiscoveryTrigger!);

        _research.LogNetworkEvent(server, "destructive-analyzer", Loc.GetString("research-netlog-destructive-analyzed", ("channels", analyzable.DestructiveReward.Count)), args.User);

        ent.Comp.LastSubject = ToPrettyString(used);
        ent.Comp.LastResult = Loc.GetString("research-machine-destructive-last-result-success", ("channels", analyzable.DestructiveReward.Count));
        UpdateUi(ent);

        Del(used);
        _popup.PopupEntity(Loc.GetString("research-destructive-analyzer-success"), ent, args.User, PopupType.SmallCaution);
        args.Handled = true;
    }
}
