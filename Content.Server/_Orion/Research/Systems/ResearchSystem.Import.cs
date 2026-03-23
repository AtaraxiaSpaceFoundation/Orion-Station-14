using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    /// <summary>
    /// Imports researched technologies directly into the database without triggering per-technology unlock effects.
    /// Intended for snapshot-style data transfers such as research data disks.
    /// </summary>
    public int ImportTechnologySnapshot(EntityUid uid, IEnumerable<string> technologies, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(uid, ref database, false))
            return 0;

        var imported = 0;
        foreach (var technologyId in technologies)
        {
            if (!PrototypeManager.TryIndex<TechnologyPrototype>(technologyId, out _))
                continue;

            if (database.ResearchedTechnologies.Contains(technologyId))
                continue;

            database.ResearchedTechnologies.Add(technologyId);
            imported++;
        }

        if (imported == 0)
            return 0;

        RecalculateTechnologyState(uid, database);
        UpdateTechnologyCards(uid, database);
        Dirty(uid, database);
        return imported;
    }
}
