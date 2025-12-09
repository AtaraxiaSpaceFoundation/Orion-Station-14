using System.Text;
using Content.Shared._Orion.CustomGhost;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Commands;

//
// License-Identifier: AGPL-3.0-or-later
//

[AnyCommand]
public sealed class ListCustomGhostsCommand : IConsoleCommand
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public string Command => "listcustomghosts";
    public string Description => Loc.GetString("listcustomghosts-command-description");
    public string Help => Loc.GetString("listcustomghosts-command-help-text");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var protos = _proto.EnumeratePrototypes<CustomGhostPrototype>();
        var sb = new StringBuilder();
        if (shell.Player is not { } player)
        {
            foreach (var proto in protos)
            {
                sb.AppendLine(proto.ID);
            }

            shell.WriteLine(sb.ToString());
            return;
        }

        if (args.Length > 2 || args.Length == 1 && args[0] != "all")
        {
            shell.WriteLine(Help);
            return;
        }

        var all = args.Length == 1;

        sb.AppendLine(Loc.GetString($"listcustomghosts-{(all ? "all" : "available")}-ghosts"));

        foreach(var proto in protos)
        {
            var available = true;
            if(proto.Restrictions is not null)
            {
                foreach(var restriction in proto.Restrictions)
                {
                    if (restriction.CanUse(player, out _))
                        continue;

                    if (restriction.HideOnFail)
                        goto skipPrototype; // wojaks_pointing.png

                    available = false;
                    break;
                }
            }

            if (available)
                sb.AppendLine($"- {proto.ID}");
            else if (all)
                sb.AppendLine($"- {proto.ID} {Loc.GetString("listcustomghosts-locked")}");
            skipPrototype:;
        }

        shell.WriteLine(sb.ToString());
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args) =>
        args.Length switch
        {
            1 => CompletionResult.FromHint("all"),
            _ => CompletionResult.Empty,
        };
}
