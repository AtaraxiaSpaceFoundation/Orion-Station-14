using Content.Server.Research.Components;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Content.Shared.Tag;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    private void InitializeExperiments()
    {
        SubscribeLocalEvent<ResearchConsoleComponent, AfterInteractUsingEvent>(OnConsoleAfterInteractUsing);
    }

    private void OnConsoleAfterInteractUsing(EntityUid uid, ResearchConsoleComponent component, ref AfterInteractUsingEvent args)
    {
        if (TryGetClientServer(uid, out var discoveryServerUid, out _))
        {
            var discoveryServer = discoveryServerUid.Value;
            NotifyDiscoveryEvent(discoveryServer,
                new DiscoveryEventData
                {
                    Type = ResearchDiscoveryEventType.ScanEntity,
                    Subject = args.Used,
                    User = args.User,
                });
        }

        if (args.Handled)
            return;

        if (!TryGetClientServer(uid, out var serverUid, out var serverComp))
            return;

        var server = serverUid.Value;

        if (!TryComp<TechnologyDatabaseComponent>(server, out var database))
            return;

        if (!TryProgressExperimentsWithEntity(server, args.Used, args.User, database, serverComp))
            return;

        args.Handled = true;
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    public bool TryProgressExperimentsWithEntity(
        EntityUid serverUid,
        EntityUid subject,
        EntityUid user,
        TechnologyDatabaseComponent? database = null,
        ResearchServerComponent? server = null)
    {
        if (!Resolve(serverUid, ref database, ref server))
            return false;

        var progressed = false;
        var activeExperiments = database.ActiveExperiments.ToArray();
        foreach (var experimentId in activeExperiments)
        {
            if (!PrototypeManager.TryIndex<ResearchExperimentPrototype>(experimentId, out var experiment))
                continue;

            if (!TryIncrementExperimentProgress(database, experiment, subject, out var delta))
                continue;

            if (delta <= 0)
                continue;

            progressed = true;

            if (!TryGetExperimentProgress(database, experimentId, out var progressIndex))
                continue;

            var progress = database.ExperimentProgress[progressIndex];
            progress.Progress = Math.Min(progress.Target, progress.Progress + delta);
            database.ExperimentProgress[progressIndex] = progress;

            if (progress.Progress < progress.Target)
                continue;

            CompleteExperiment(serverUid, experiment, user, database, server);
        }

        if (!progressed)
            return false;

        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);
        return true;
    }

    public bool TryProgressExperimentsByAction(EntityUid serverUid, string actionId, TechnologyDatabaseComponent? database = null, ResearchServerComponent? server = null)
    {
        if (!Resolve(serverUid, ref database, ref server))
            return false;

        var progressed = false;
        foreach (var experimentId in database.ActiveExperiments.ToArray())
        {
            if (!PrototypeManager.TryIndex<ResearchExperimentPrototype>(experimentId, out var experiment) ||
                experiment.Objective is not ActionCountExperimentObjective actionObjective ||
                actionObjective.ActionId != actionId)
            {
                continue;
            }

            progressed |= IncrementSimpleProgress(serverUid, database, server, experiment, 1);
        }

        if (!progressed)
            return false;

        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);
        return true;
    }

    public bool TryTriggerExperiments(EntityUid serverUid, string triggerId, TechnologyDatabaseComponent? database = null, ResearchServerComponent? server = null)
    {
        if (!Resolve(serverUid, ref database, ref server))
            return false;

        var progressed = false;
        foreach (var experimentId in database.ActiveExperiments.ToArray())
        {
            if (!PrototypeManager.TryIndex<ResearchExperimentPrototype>(experimentId, out var experiment) ||
                experiment.Objective is not ServerTriggerExperimentObjective triggerObjective ||
                triggerObjective.TriggerId != triggerId)
            {
                continue;
            }

            progressed |= IncrementSimpleProgress(serverUid, database, server, experiment, 1);
        }

        if (!progressed)
            return false;

        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);
        return true;
    }

    public bool TryCompleteExperimentById(EntityUid serverUid, string experimentId, EntityUid? user = null, TechnologyDatabaseComponent? database = null, ResearchServerComponent? server = null)
    {
        if (!Resolve(serverUid, ref database, ref server))
            return false;

        if (!PrototypeManager.TryIndex<ResearchExperimentPrototype>(experimentId, out var experiment))
            return false;

        if (!database.ActiveExperiments.Contains(experimentId))
            return false;

        CompleteExperiment(serverUid, experiment, user, database, server);
        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);
        return true;
    }

    private void CompleteExperiment(
        EntityUid serverUid,
        ResearchExperimentPrototype experiment,
        EntityUid? user,
        TechnologyDatabaseComponent database,
        ResearchServerComponent server)
    {
        if (!database.CompletedExperiments.Contains(experiment.ID))
            database.CompletedExperiments.Add(experiment.ID);

        database.ActiveExperiments.Remove(experiment.ID);

        for (var i = 0; i < database.ExperimentProgress.Count; i++)
        {
            if (database.ExperimentProgress[i].ExperimentId != experiment.ID)
                continue;

            var progress = database.ExperimentProgress[i];
            progress.Progress = progress.Target;
            progress.CompletedAt = _timing.CurTime;
            database.ExperimentProgress[i] = progress;
            break;
        }

        ApplyExperimentReward(serverUid, experiment, database, server);
        TriggerDiscovery(serverUid, $"experiment:{experiment.ID}", database);

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} completed research experiment {experiment.ID} on {ToPrettyString(serverUid)}.");
    }

    private void ApplyExperimentReward(EntityUid serverUid,
        ResearchExperimentPrototype experiment,
        TechnologyDatabaseComponent database,
        ResearchServerComponent server)
    {
        var reward = experiment.Reward;

        if (reward.ResearchPoints != 0)
            ModifyServerPoints(serverUid, reward.ResearchPoints, server);

        foreach (var pointReward in reward.PointRewards)
        {
            ModifyServerPoints(serverUid, pointReward.Type, pointReward.Amount, server);
        }

        foreach (var unlocked in reward.UnlockExperiments)
        {
            if (!database.UnlockedExperiments.Contains(unlocked))
                database.UnlockedExperiments.Add(unlocked);
        }

        foreach (var technology in reward.RevealTechnologies)
        {
            if (!database.RevealedTechnologies.Contains(technology))
                database.RevealedTechnologies.Add(technology);
        }

        if (reward.InfrastructureUnlock)
        {
            // Foundation hook for future infrastructure unlock rewards.
        }

        LogNetworkEvent(serverUid, "experiment", $"Experiment completed: {experiment.ID}");
    }

    private bool TryIncrementExperimentProgress(
        TechnologyDatabaseComponent database,
        ResearchExperimentPrototype experiment,
        EntityUid subject,
        out int delta)
    {
        delta = 0;

        if (database.CompletedExperiments.Contains(experiment.ID) && !experiment.Repeatable)
            return false;

        switch (experiment.Objective)
        {
            case PresentItemExperimentObjective presentObjective:
                if (!MatchesEntityObjective(subject, presentObjective))
                    return false;

                delta = 1;
                return true;

            case ScanEntityExperimentObjective scanObjective:
                if (!MatchesEntityObjective(subject, scanObjective))
                    return false;

                delta = 1;
                return true;

            default:
                return false;
        }
    }

    private bool IncrementSimpleProgress(EntityUid serverUid,
        TechnologyDatabaseComponent database,
        ResearchServerComponent server,
        ResearchExperimentPrototype experiment,
        int delta)
    {
        if (!TryGetExperimentProgress(database, experiment.ID, out var progressIndex))
            return false;

        var progress = database.ExperimentProgress[progressIndex];
        progress.Progress = Math.Min(progress.Target, progress.Progress + delta);
        database.ExperimentProgress[progressIndex] = progress;

        if (progress.Progress < progress.Target)
            return true;

        CompleteExperiment(serverUid, experiment, null, database, server);
        return true;
    }

    private bool MatchesEntityObjective(EntityUid subject, ScanEntityExperimentObjective objective)
    {
        if (objective.RequiredEntityPrototype != null &&
            (!TryComp<MetaDataComponent>(subject, out var meta) || meta.EntityPrototype?.ID != objective.RequiredEntityPrototype))
        {
            return false;
        }

        foreach (var tag in objective.RequiredTags)
        {
            if (!_tag.HasTag(subject, tag))
                return false;
        }

        foreach (var componentName in objective.RequiredComponents)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(componentName, out var registration))
                return false;

            if (!EntityManager.HasComponent(subject, registration.Type))
                return false;
        }

        return true;
    }

    private bool TryGetExperimentProgress(TechnologyDatabaseComponent database, string experimentId, out int progressIndex)
    {
        for (var i = 0; i < database.ExperimentProgress.Count; i++)
        {
            if (database.ExperimentProgress[i].ExperimentId == experimentId)
            {
                progressIndex = i;
                return true;
            }
        }

        progressIndex = -1;
        return false;
    }
}
