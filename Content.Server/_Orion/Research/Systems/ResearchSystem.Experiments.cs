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
            NotifyDiscoveryEvent(discoveryServerUid.Value,
                new DiscoveryEventData
                {
                    Type = ResearchDiscoveryEventType.ScanEntity,
                    Subject = args.Used,
                    User = args.User,
                });
        }

        if (args.Handled)
            return;

        if (!TryGetClientServer(uid, out var serverUid, out _))
            return;

        if (!TryProgressExperimentsWithEntity(serverUid.Value, args.Used, args.User, out _, out _))
            return;

        args.Handled = true;
        SyncClientWithServer(uid);
        UpdateConsoleInterface(uid, component);
    }

    public bool TryProgressExperimentsWithEntity(EntityUid serverUid,
        EntityUid subject,
        EntityUid? user,
        out bool changed,
        out List<string> completed,
        TechnologyDatabaseComponent? database = null,
        ResearchServerComponent? server = null)
    {
        changed = false;
        completed = new List<string>();

        if (!Resolve(serverUid, ref database, ref server))
            return false;

        foreach (var experimentId in database.ActiveExperiments.ToArray())
        {
            if (!PrototypeManager.TryIndex<ResearchExperimentPrototype>(experimentId, out var experiment))
                continue;

            if (!TryGetExperimentProgress(database, experimentId, out var progressIndex))
                continue;

            if (!TryIncrementExperimentProgress(database, progressIndex, experiment, subject, out var delta))
                continue;

            if (delta <= 0)
                continue;

            changed = true;
            var progress = database.ExperimentProgress[progressIndex];
            progress.Progress = Math.Min(progress.Target, progress.Progress + delta);
            database.ExperimentProgress[progressIndex] = progress;

            if (progress.Progress < progress.Target)
                continue;

            completed.Add(experiment.ID);
            CompleteExperiment(serverUid, experiment, user, database, server);
        }

        if (!changed)
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

    private void CompleteExperiment(EntityUid serverUid,
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

        ApplyExperimentReward(serverUid, experiment, user, database, server);
        TriggerDiscovery(serverUid, $"experiment:{experiment.ID}", database);
        LogNetworkEvent(serverUid, "experiment", Loc.GetString("research-netlog-experiment-completed", ("experiment", Loc.GetString(experiment.Name)), ("user", GetResearchLogUserName(user))), user);
        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} completed research experiment {experiment.ID} on {ToPrettyString(serverUid)}.");
    }

    private void ApplyExperimentReward(EntityUid serverUid,
        ResearchExperimentPrototype experiment,
        EntityUid? user,
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
            RevealTechnology(serverUid, technology, user, database);
        }

        LogNetworkEvent(serverUid, "experiment", Loc.GetString("research-netlog-experiment-reward-applied", ("experiment", Loc.GetString(experiment.Name)), ("user", GetResearchLogUserName(user))), user);
    }

    private bool TryIncrementExperimentProgress(TechnologyDatabaseComponent database,
        int progressIndex,
        ResearchExperimentPrototype experiment,
        EntityUid subject,
        out int delta)
    {
        delta = 0;
        var progress = database.ExperimentProgress[progressIndex];

        if (progress.ScannedEntities.Contains(GetNetEntity(subject)))
            return false;

        var objective = experiment.Objective;

        switch (objective)
        {
            case PresentItemExperimentObjective presentObjective when MatchesEntityObjective(subject, presentObjective):
            case ScanEntityExperimentObjective scanObjective when MatchesEntityObjective(subject, scanObjective):
                delta = 1;
                progress.ScannedEntities.Add(GetNetEntity(subject));
                break;
            case ScanDifferentEntitiesExperimentObjective differentObjective when MatchesEntityObjective(subject, differentObjective):
                var key = GetEntityObjectiveUniqKey(subject);
                if (!progress.UniqueProgressKeys.Add(key))
                    return false;

                delta = 1;
                progress.ScannedEntities.Add(GetNetEntity(subject));
                break;
            case ScanSamplesExperimentObjective samplesObjective when MatchesEntityObjective(subject, samplesObjective):
                delta = 1;
                progress.ScannedEntities.Add(GetNetEntity(subject));
                break;
            default:
                return false;
        }

        database.ExperimentProgress[progressIndex] = progress;
        return true;
    }

    private string GetEntityObjectiveUniqKey(EntityUid subject)
    {
        if (TryComp<MetaDataComponent>(subject, out var meta) && meta.EntityPrototype != null)
            return $"proto:{meta.EntityPrototype.ID}";

        return $"ent:{subject}";
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
            return false;

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

    private static bool TryGetExperimentProgress(TechnologyDatabaseComponent database, string experimentId, out int progressIndex)
    {
        for (var i = 0; i < database.ExperimentProgress.Count; i++)
        {
            if (database.ExperimentProgress[i].ExperimentId != experimentId)
                continue;

            progressIndex = i;
            return true;
        }

        progressIndex = -1;
        return false;
    }
}
