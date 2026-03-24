using System.Linq;
using Content.Server.Administration;
using Content.Server.Research.Systems;
using Content.Shared.Administration;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class ResearchUnlockAllServerCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    public override string Command => "unlockallresearches";
    public override string Description => Loc.GetString("unlockallresearches-command-description");
    public override string Help => Loc.GetString("unlockallresearches-command-help", ("command", Command));

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.Components<ResearchServerComponent>(args[0], EntityManager),
                Loc.GetString("unlockallresearches-command-hint-server")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var serverUid))
        {
            shell.WriteError(Loc.GetString("command-error-invalid-server-uid"));
            return;
        }

        if (!EntityManager.TryGetComponent(serverUid, out ResearchServerComponent? server))
        {
            shell.WriteError(Loc.GetString("command-error-not-research-server"));
            return;
        }

        if (!EntityManager.HasComponent<TechnologyDatabaseComponent>(serverUid))
        {
            shell.WriteError(Loc.GetString("command-error-missing-tech-database"));
            return;
        }

        var research = _systemManager.GetEntitySystem<ResearchSystem>();
        var unlocked = research.UnlockAllTechnologiesOnServer(serverUid, server: server);
        shell.WriteLine(Loc.GetString("unlockallresearches-command-success",
            ("count", unlocked),
            ("serverUid", serverUid),
            ("serverName", server.ServerName)));
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ResearchAddPointsServerCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override string Command => "addresearchpoints";
    public override string Description => Loc.GetString("addresearchpoints-command-description");
    public override string Help => Loc.GetString("addresearchpoints-command-help", ("command", Command));

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.Components<ResearchServerComponent>(args[0], EntityManager),
                Loc.GetString("addresearchpoints-command-hint-server")),
            2 => CompletionResult.FromHintOptions(GetPointTypes(args[1]),
                Loc.GetString("addresearchpoints-command-hint-type")),
            3 => CompletionResult.FromHint(Loc.GetString("addresearchpoints-command-hint-amount")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var serverUid))
        {
            shell.WriteError(Loc.GetString("command-error-invalid-server-uid"));
            return;
        }

        if (!EntityManager.TryGetComponent(serverUid, out ResearchServerComponent? server))
        {
            shell.WriteError(Loc.GetString("command-error-not-research-server"));
            return;
        }

        var pointType = args[1];
        if (string.IsNullOrWhiteSpace(pointType))
        {
            shell.WriteError(Loc.GetString("command-error-empty-point-type"));
            return;
        }

        if (!int.TryParse(args[2], out var amount))
        {
            shell.WriteError(Loc.GetString("command-error-invalid-amount"));
            return;
        }

        var research = _systemManager.GetEntitySystem<ResearchSystem>();
        var balance = research.AddServerPointsByType(serverUid, pointType, amount, server);
        shell.WriteLine(Loc.GetString("addresearchpoints-command-success",
            ("serverUid", serverUid),
            ("serverName", server.ServerName),
            ("type", pointType),
            ("balance", balance)));
    }

    private IEnumerable<string> GetPointTypes(string input)
    {
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var technology in _prototype.EnumeratePrototypes<TechnologyPrototype>())
        {
            foreach (var cost in technology.PointCosts)
            {
                if (!string.IsNullOrWhiteSpace(cost.Type))
                    types.Add(cost.Type);
            }
        }

        var query = EntityManager.EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out _, out var server))
        {
            foreach (var balance in server.PointBalances)
            {
                if (!string.IsNullOrWhiteSpace(balance.Type))
                    types.Add(balance.Type);
            }
        }

        return string.IsNullOrWhiteSpace(input)
            ? types.OrderBy(x => x)
            : types.Where(type => type.Contains(input, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x);
    }
}
