using System.Linq;
using Content.Server.Construction.Completions;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Containers;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeDiscovery()
    {
        SubscribeLocalEvent<ResearchClientComponent, EntInsertedIntoContainerMessage>(OnDiscoveryMachineInsertion);
        SubscribeLocalEvent<MetaDataComponent, ConstructionBeforeDeleteEvent>(OnDiscoveryDeconstruct);
    }

    private void OnDiscoveryMachineInsertion(EntityUid uid, ResearchClientComponent component, ref EntInsertedIntoContainerMessage args)
    {
        if (component.Server is not { } server)
            return;

        NotifyDiscoveryEvent(server,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = args.Entity,
            Machine = uid,
        });
    }

    private void OnDiscoveryDeconstruct(EntityUid uid, MetaDataComponent component, ref ConstructionBeforeDeleteEvent args)
    {
        if (!HasComp<ResearchAnalyzableComponent>(uid))
            return;

        if (!TryGetResearchServerForEntity(uid, out var serverUid) || serverUid is not { } server)
            return;

        NotifyDiscoveryEvent(server,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.DeconstructEntity,
            Subject = uid,
        });
    }

    public bool NotifyDiscoveryEvent(EntityUid serverUid, DiscoveryEventData data, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return false;

        var revealedAny = false;
        foreach (var technology in PrototypeManager.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!technology.Hidden || !database.SupportedDisciplines.Contains(technology.Discipline))
                continue;

            if (database.RevealedTechnologies.Contains(technology.ID))
                continue;

            var revealedByRequirements = false;
            if (technology.RevealRequirements.Count > 0)
            {
                ProcessTechnologyDiscoveryEvent(database, technology, data);
                revealedByRequirements = AreRevealRequirementsSatisfied(database, technology);
            }

            var revealedByUnlockLists = IsUnlockListRevealSatisfied(technology, data);

            if (!revealedByRequirements && !revealedByUnlockLists)
                continue;

            database.RevealedTechnologies.Add(technology.ID);
            revealedAny = true;
            _sawmill.Info($"Revealed hidden technology {technology.ID} via discovery event {data.Type}.");
            LogNetworkEvent(serverUid, "discovery", Loc.GetString("research-netlog-discovery-hidden-tech", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(data.User))), data.User);
        }

        if (!revealedAny)
            return false;

        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);

        if (!TryComp<ResearchServerComponent>(serverUid, out var serverComp) ||
            serverComp.Clients.Count <= 0)
            return true;

        foreach (var console in serverComp.Clients)
        {
            SyncClientWithServer(console);
            UpdateConsoleInterface(console);
        }

        return true;
    }

    public bool TriggerDiscovery(EntityUid serverUid, string triggerId, TechnologyDatabaseComponent? database = null)
    {
        return NotifyDiscoveryEvent(serverUid,
            new DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.ServerTrigger,
            TriggerId = triggerId,
        },
        database);
    }

    private void ProcessTechnologyDiscoveryEvent(TechnologyDatabaseComponent database, TechnologyPrototype technology, DiscoveryEventData data)
    {
        foreach (var requirement in technology.RevealRequirements)
        {
            if (IsRevealRequirementSatisfied(database, technology.ID, requirement))
                continue;

            if (!DoesDiscoveryEventMatch(requirement, data))
                continue;

            IncrementDiscoveryProgress(database, technology.ID, requirement, 1);
        }
    }

    private bool IsUnlockListRevealSatisfied(TechnologyPrototype technology, DiscoveryEventData data)
    {
        if (data.Subject == null)
            return false;

        var subjectPrototype = GetPrototypeId(data.Subject);
        if (subjectPrototype == null)
            return false;

        return data.Type switch
        {
            ResearchDiscoveryEventType.MachineInsertion => technology.ItemUnlocks.Contains(subjectPrototype),
            ResearchDiscoveryEventType.DeconstructEntity => technology.DeconstructionUnlocks.Contains(subjectPrototype),
            _ => false,
        };
    }

    private static bool AreRevealRequirementsSatisfied(TechnologyDatabaseComponent database, TechnologyPrototype technology)
    {
        return technology.RevealRequirements.Count == 0 || technology.RevealRequirements.All(req => IsRevealRequirementSatisfied(database, technology.ID, req));
    }

    private static bool IsRevealRequirementSatisfied(TechnologyDatabaseComponent database,
        string technologyId,
        TechnologyRevealRequirement requirement)
    {
        return requirement switch
        {
            RevealedTechnologyRevealRequirement revealed => database.RevealedTechnologies.Contains(revealed.Technology) || database.ResearchedTechnologies.Contains(revealed.Technology),
            ResearchedTechnologyRevealRequirement researched => database.ResearchedTechnologies.Contains(researched.Technology),
            CompletedExperimentRevealRequirement completed => database.CompletedExperiments.Contains(completed.Experiment),
            _ => GetDiscoveryProgress(database, technologyId, requirement.Id) >= Math.Max(1, requirement.Target)
        };
    }

    private bool DoesDiscoveryEventMatch(TechnologyRevealRequirement requirement, DiscoveryEventData data)
    {
        switch (requirement)
        {
            case ScanEntityRevealRequirement scan when requirement.Kind == TechnologyRevealRequirementKind.ScanEntity:
                return data.Type == ResearchDiscoveryEventType.ScanEntity && MatchesScanRequirement(scan, data.Subject);

            case MachineInsertionRevealRequirement insertion:
                return data.Type == ResearchDiscoveryEventType.MachineInsertion && MatchesScanRequirement(insertion, data.Subject)
                    && (insertion.RequiredMachinePrototype == null || GetPrototypeId(data.Machine) == insertion.RequiredMachinePrototype);

            case DeconstructEntityRevealRequirement deconstruct:
                return data.Type == ResearchDiscoveryEventType.DeconstructEntity && MatchesDeconstructRequirement(deconstruct, data.Subject);

            case ServerTriggerRevealRequirement trigger:
                return data.Type == ResearchDiscoveryEventType.ServerTrigger && trigger.TriggerId == data.TriggerId;

            default:
                return false;
        }
    }

    private bool MatchesScanRequirement(ScanEntityRevealRequirement requirement, EntityUid? subject)
    {
        if (subject == null)
            return false;

        if (requirement.RequiredEntityPrototype != null && GetPrototypeId(subject) != requirement.RequiredEntityPrototype)
            return false;

        foreach (var tag in requirement.RequiredTags)
        {
            if (!_tag.HasTag(subject.Value, tag))
                return false;
        }

        return true;
    }

    private bool MatchesDeconstructRequirement(DeconstructEntityRevealRequirement requirement, EntityUid? subject)
    {
        if (subject == null)
            return false;

        if (requirement.RequiredEntityPrototype != null && GetPrototypeId(subject) != requirement.RequiredEntityPrototype)
            return false;

        foreach (var tag in requirement.RequiredTags)
        {
            if (!_tag.HasTag(subject.Value, tag))
                return false;
        }

        return true;
    }

    private void IncrementDiscoveryProgress(TechnologyDatabaseComponent database, string technologyId, TechnologyRevealRequirement requirement, int amount)
    {
        var target = Math.Max(1, requirement.Target);

        for (var i = 0; i < database.DiscoveryProgress.Count; i++)
        {
            var entry = database.DiscoveryProgress[i];
            if (entry.TechnologyId != technologyId || entry.RequirementId != requirement.Id)
                continue;

            if (entry.Progress >= target)
                return;

            entry.Progress = Math.Min(target, entry.Progress + amount);
            if (entry.Progress >= target)
                entry.CompletedAt = _timing.CurTime;

            database.DiscoveryProgress[i] = entry;
            return;
        }

        database.DiscoveryProgress.Add(new TechnologyDiscoveryProgress
        {
            TechnologyId = technologyId,
            RequirementId = requirement.Id,
            Progress = Math.Min(target, amount),
            Target = target,
            CompletedAt = amount >= target ? _timing.CurTime : null
        });
    }

    private static int GetDiscoveryProgress(TechnologyDatabaseComponent database, string technologyId, string requirementId)
    {
        foreach (var entry in database.DiscoveryProgress)
        {
            if (entry.TechnologyId == technologyId && entry.RequirementId == requirementId)
                return entry.Progress;
        }

        return 0;
    }

    public List<string> GetHiddenTechnologiesForRequiredItem(EntityUid serverUid, EntityUid subject, TechnologyDatabaseComponent? database = null)
    {
        var result = new List<string>();

        if (!Resolve(serverUid, ref database))
            return result;

        var subjectPrototype = GetPrototypeId(subject);
        if (subjectPrototype == null)
            return result;

        foreach (var technology in PrototypeManager.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!technology.Hidden || !database.SupportedDisciplines.Contains(technology.Discipline))
                continue;

            if (database.RevealedTechnologies.Contains(technology.ID))
                continue;

            if (!technology.RequiredItemsToUnlock.Contains(subjectPrototype))
                continue;

            result.Add(technology.ID);
        }

        return result;
    }

    private string? GetPrototypeId(EntityUid? uid)
    {
        if (uid == null || !TryComp<MetaDataComponent>(uid.Value, out var meta))
            return null;

        return meta.EntityPrototype?.ID;
    }

    private bool TryGetResearchServerForEntity(EntityUid uid, out EntityUid? serverUid)
    {
        serverUid = null;

        if (TryComp<ResearchClientComponent>(uid, out var directClient) && directClient.Server is { } directServer)
        {
            serverUid = directServer;
            return true;
        }

        var transform = Transform(uid);
        if (TryComp<ResearchClientComponent>(transform.ParentUid, out var ownerClient) && ownerClient.Server is { } ownerServer)
        {
            serverUid = ownerServer;
            return true;
        }

        if (transform.GridUid is not { } grid)
            return false;

        var query = EntityQueryEnumerator<ResearchServerComponent, TransformComponent>();
        while (query.MoveNext(out var candidateUid, out _, out _))
        {
            if (Transform(candidateUid).GridUid != grid)
                continue;

            serverUid = candidateUid;
            return true;
        }

        return false;
    }

    public sealed record DiscoveryEventData
    {
        public ResearchDiscoveryEventType Type;
        public EntityUid? Subject;
        public EntityUid? Machine;
        public EntityUid? User;
        public string? TriggerId;
    }
}
