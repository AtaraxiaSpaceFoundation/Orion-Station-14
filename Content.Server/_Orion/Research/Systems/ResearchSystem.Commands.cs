using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private string GetLocalizedPointType(string type)
    {
        return LocalizeResearchPointType(type);
    }

    public int UnlockAllTechnologiesOnServer(EntityUid uid, ResearchServerComponent? server = null, TechnologyDatabaseComponent? database = null)
    {
        if (!Resolve(uid, ref server, ref database, false))
            return 0;

        var authority = GetNetworkAuthority(uid, server);
        if (authority != uid)
        {
            uid = authority;
            server = null;
            database = null;

            if (!Resolve(uid, ref server, ref database, false))
                return 0;
        }

        var unlocked = 0;
        foreach (var technology in PrototypeManager.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (!database.SupportedDisciplines.Contains(technology.Discipline))
                database.SupportedDisciplines.Add(technology.Discipline);

            if (!database.RevealedTechnologies.Contains(technology.ID))
                database.RevealedTechnologies.Add(technology.ID);

            if (database.ResearchedTechnologies.Contains(technology.ID))
                continue;

            database.ResearchedTechnologies.Add(technology.ID);
            unlocked++;
        }

        if (unlocked == 0)
            return 0;

        RecalculateTechnologyState(uid, database);
        UpdateTechnologyCards(uid, database);
        Dirty(uid, database);
        LogNetworkEvent(uid,
            "admin",
            Loc.GetString("research-netlog-admin-unlocked-all-technologies", ("user", Loc.GetString("research-netlog-user-admin"))),
            null,
            server);

        return unlocked;
    }

    public int AddServerPointsByType(EntityUid uid, string type, int amount, ResearchServerComponent? server = null)
    {
        ModifyServerPoints(uid, type, amount, server);
        var balance = GetPointBalance(uid, type, server);
        var localizedType = GetLocalizedPointType(type);
        LogNetworkEvent(uid,
            "admin",
            Loc.GetString("research-netlog-admin-added-points",
                ("user", Loc.GetString("research-netlog-user-admin")),
                ("type", localizedType),
                ("amount", amount),
                ("balance", balance)),
            null,
            server);
        return balance;
    }
}
