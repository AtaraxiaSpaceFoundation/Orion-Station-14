using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    public void RevealTechnology(EntityUid serverUid, string technologyId, EntityUid? user = null, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return;

        if (database.RevealedTechnologies.Contains(technology.ID))
            return;

        database.RevealedTechnologies.Add(technology.ID);
        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);

        LogNetworkEvent(serverUid, "discovery", Loc.GetString("research-netlog-discovery-hidden-tech", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(user))), user);
    }

    public void UnlockTechnology(EntityUid serverUid,
        string technologyId,
        EntityUid? user,
        TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return;

        if (database.ResearchedTechnologies.Contains(technology.ID))
            return;

        AddTechnology(serverUid, technology, database);
        LogNetworkEvent(serverUid, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(technology.Name)), ("user", GetResearchLogUserName(user))), user);
    }
}
