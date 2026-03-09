using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    public bool RevealTechnology(EntityUid serverUid, string technologyId, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return false;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return false;

        if (database.RevealedTechnologies.Contains(technology.ID))
            return false;

        database.RevealedTechnologies.Add(technology.ID);
        RecalculateTechnologyState(serverUid, database);
        UpdateTechnologyCards(serverUid, database);
        Dirty(serverUid, database);

        LogNetworkEvent(serverUid, "discovery", Loc.GetString("research-netlog-discovery-hidden-tech", ("technology", Loc.GetString(technology.Name))));
        return true;
    }

    public bool UnlockTechnology(EntityUid serverUid, string technologyId, EntityUid? user, bool ignoreCosts, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(serverUid, ref database))
            return false;

        if (!PrototypeManager.TryIndex<TechnologyPrototype>(technologyId, out var technology))
            return false;

        if (database.ResearchedTechnologies.Contains(technology.ID))
            return false;

        AddTechnology(serverUid, technology, database);
        LogNetworkEvent(serverUid, "technology", Loc.GetString("research-netlog-technology-unlocked", ("technology", Loc.GetString(technology.Name))), user);
        return true;
    }
}
