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

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperimentatorComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(Entity<ExperimentatorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Used is not { } used)
            return;

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var server = client.Server ?? _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault().Owner;
        if (server == EntityUid.Invalid)
            return;

        foreach (var operationId in ent.Comp.Operations)
        {
            if (!PrototypeManager.TryIndex(operationId, out ResearchExperimentatorOperationPrototype? operation))
                continue;

            if (!OperationMatches(operation, used))
                continue;

            RunOperation(ent, used, args.User, server, operation);
            args.Handled = true;
            return;
        }
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

    private void RunOperation(Entity<ExperimentatorComponent> machine, EntityUid used, EntityUid user, EntityUid server,
        ResearchExperimentatorOperationPrototype operation)
    {
        var success = _random.Prob(operation.SuccessChance);
        var rewards = success ? operation.SuccessReward : operation.FailureReward;
        foreach (var reward in rewards)
        {
            _research.ModifyServerPoints(server, reward.Id, reward.Amount);
        }

        if (success && !string.IsNullOrWhiteSpace(operation.SuccessExperimentAction))
            _research.TryProgressExperimentsByAction(server, operation.SuccessExperimentAction!);
        else if (!success && !string.IsNullOrWhiteSpace(operation.FailureExperimentAction))
            _research.TryProgressExperimentsByAction(server, operation.FailureExperimentAction!);

        var backfire = !success && _random.Prob(operation.BackfireChanceOnFailure);
        if (backfire)
        {
            _damageable.TryChangeDamage(user, operation.BackfireDamage);
            if (!string.IsNullOrWhiteSpace(operation.BackfireExperimentAction))
                _research.TryProgressExperimentsByAction(server, operation.BackfireExperimentAction!);
        }

        _research.TryProgressExperimentsWithEntity(server, used, user);
        _research.NotifyDiscoveryEvent(server, new ResearchSystem.DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = used,
            Machine = machine,
            User = user
        });

        if (!string.IsNullOrWhiteSpace(operation.DiscoveryTrigger))
            _research.TriggerDiscovery(server, operation.DiscoveryTrigger!);

        var result = backfire ? "backfire" : (success ? "success" : "failure");
        _research.LogNetworkEvent(server, "experimentator",
            $"Experimentator operation {operation.ID} on {ToPrettyString(used)} => {result}.", user);
        _popup.PopupEntity(Loc.GetString($"research-experimentator-{result}"), machine, user, PopupType.SmallCaution);

        Del(used);
    }
}
