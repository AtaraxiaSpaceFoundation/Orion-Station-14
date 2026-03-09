using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Orion.Research.Systems;

public sealed class ExperimentatorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperimentatorComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<ExperimentatorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ExperimentatorComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
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
        if (!_ui.IsUiOpen(ent.Owner, ExperimentatorUiKey.Key))
            return;

        UpdateUi(ent);
    }

    private void OnRegistrationChanged(Entity<ExperimentatorComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ExperimentatorComponent> ent)
    {
        string? serverName = null;
        var pointBalances = new List<ResearchPointAmount>();
        if (_research.TryGetClientServer(ent.Owner, out _, out var server))
        {
            serverName = server.ServerName;
            pointBalances = server.PointBalances.ToList();
        }

        var operations = new List<ExperimentatorOperationUiData>();
        foreach (var operationId in ent.Comp.Operations)
        {
            if (!_prototype.TryIndex(operationId, out var operation))
                continue;

            operations.Add(new ExperimentatorOperationUiData(
                operation.RequiredTags.Select(tag => tag.Id).ToArray(),
                operation.SuccessReward.ToList(),
                operation.FailureReward.ToList(),
                operation.SuccessChance,
                operation.BackfireChanceOnFailure));
        }

        var state = new ExperimentatorBoundInterfaceState(serverName, pointBalances, ent.Comp.LastSubject, ent.Comp.LastResult, operations);
        _ui.SetUiState(ent.Owner, ExperimentatorUiKey.Key, state);
    }

    private void OnAfterInteractUsing(Entity<ExperimentatorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;

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

        foreach (var operationId in ent.Comp.Operations)
        {
            if (!_prototype.TryIndex(operationId, out var operation))
                continue;

            if (!OperationMatches(operation, used))
                continue;

            RunOperation(ent, used, args.User, server, operation);
            UpdateUi(ent);
            args.Handled = true;
            return;
        }

        ent.Comp.LastSubject = ToPrettyString(used);
        ent.Comp.LastResult = Loc.GetString("research-machine-experimentator-last-result-no-operation");
        UpdateUi(ent);
    }

    private bool OperationMatches(ResearchExperimentatorOperationPrototype operation, EntityUid used)
    {
        foreach (var tag in operation.RequiredTags)
        {
            if (!_tag.HasTag(used, tag))
                return false;
        }

        return true;
    }

    private void RunOperation(Entity<ExperimentatorComponent> machine, EntityUid used, EntityUid user, EntityUid server, ResearchExperimentatorOperationPrototype operation)
    {
        var success = _random.Prob(operation.SuccessChance);
        var rewards = success ? operation.SuccessReward : operation.FailureReward;
        foreach (var reward in rewards)
        {
            _research.ModifyServerPoints(server, reward.Type, reward.Amount);
        }

        switch (success)
        {
            case true when !string.IsNullOrWhiteSpace(operation.SuccessExperimentAction):
                _research.TryProgressExperimentsByAction(server, operation.SuccessExperimentAction!);
                break;
            case false when !string.IsNullOrWhiteSpace(operation.FailureExperimentAction):
                _research.TryProgressExperimentsByAction(server, operation.FailureExperimentAction!);
                break;
        }

        var backfire = !success && _random.Prob(operation.BackfireChanceOnFailure);
        if (backfire)
        {
            _damageable.TryChangeDamage(user, operation.BackfireDamage);
            if (!string.IsNullOrWhiteSpace(operation.BackfireExperimentAction))
                _research.TryProgressExperimentsByAction(server, operation.BackfireExperimentAction!);
        }

        _research.TryProgressExperimentsWithEntity(server, used, user);
        _research.NotifyDiscoveryEvent(server,
            new ResearchSystem.DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = used,
            Machine = machine,
            User = user,
        });

        if (!string.IsNullOrWhiteSpace(operation.DiscoveryTrigger))
            _research.TriggerDiscovery(server, operation.DiscoveryTrigger!);

        var result = backfire ? Loc.GetString("research-netlog-experimentator-result-backfire") : (success ? Loc.GetString("research-netlog-experimentator-result-success") : Loc.GetString("research-netlog-experimentator-result-failure"));
        machine.Comp.LastSubject = ToPrettyString(used);
        machine.Comp.LastResult = Loc.GetString("research-machine-experimentator-last-result", ("result", result));
        _research.LogNetworkEvent(server, "experimentator", Loc.GetString("research-netlog-experimentator-operation", ("result", result)), user);
        _popup.PopupEntity(Loc.GetString($"research-experimentator-{result}"), machine, user, PopupType.SmallCaution);

        Del(used);
    }
}
