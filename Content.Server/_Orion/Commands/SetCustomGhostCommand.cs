using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._Orion.CustomGhost;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Commands;

//
// License-Identifier: AGPL-3.0-or-later
//

[AnyCommand]
public sealed class SetCustomGhostCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;

    public string Command => "setcustomghost";
    public string Description => Loc.GetString("setcustomghost-command-description");
    public string Help => Loc.GetString("setcustomghost-command-help-text");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteLine(Loc.GetString("setcustomghost-command-no-session"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var protoId = args[0];

        if (!_proto.TryIndex<CustomGhostPrototype>(protoId, out var proto))
        {
            shell.WriteLine(Loc.GetString("setcustomghost-command-invalid-ghost-id"));
            return;
        }

        if (!proto.CanUse(player, out var failReason))
        {
            shell.WriteLine(failReason);
            return;
        }

        await _db.SaveGhostTypeAsync(player.UserId, protoId);
        var prefs = _prefMan.GetPreferences(player.UserId);
        prefs.CustomGhost = protoId;
        shell.WriteLine(Loc.GetString("setcustomghost-command-saved"));
    }
}
