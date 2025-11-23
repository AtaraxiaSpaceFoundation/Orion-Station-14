using Content.Server._Orion.ResponseForce;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Orion.ResponseForce;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class CallResponseForceCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public string Command => "callresponseforce";
    public string Description => "Вызов команды спецсил";
    public string Help => "Использование: callresponseforce <ResponseForceTeamPrototypeId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var responseForceSystem = _entManager.System<ResponseForceSystem>();

        if (!_prototypes.TryIndex<ResponseForceTeamPrototype>(args[0], out _))
            return;

        switch (args.Length)
        {
            case 1:
                if(!responseForceSystem.CallResponseForce(args[0],shell.Player != null ? shell.Player.Name : "An administrator"))
                    shell.WriteLine($"Подождите еще {responseForceSystem.DelayTime} перед запуском следующих!");
                _adminLogger.Add(
                    LogType.AdminMessage,
                    LogImpact.Extreme,
                    $"Admin {(shell.Player != null ? shell.Player.Name : "An administrator")} called SpecForceTeam {args[0]}.");
                break;
            default:
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                break;
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Index all Response Force Teams prototypes and write them down in completion result.
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<ResponseForceTeamPrototype>(true, _prototypes),
                "Тип вызова"),
            _ => CompletionResult.Empty,
        };
    }
}
