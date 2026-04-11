using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Orion.Mood;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Mood;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddMoodEffectCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MoodSystem _moodSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "addmood";
    public override string Description => Loc.GetString("addmood-command-description");
    public override string Help => Loc.GetString("addmood-command-help", ("command", Command));

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.Components<MoodComponent>(args[0], EntityManager),
                Loc.GetString("addmood-command-hint-entity")),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<MoodEffectPrototype>(),
                Loc.GetString("addmood-command-hint-mood")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("addmood-command-error-invalid-uid"));
            return;
        }

        if (!EntityManager.HasComponent<MoodComponent>(uid))
        {
            shell.WriteError(Loc.GetString("addmood-command-error-missing-component"));
            return;
        }

        if (!_prototypeManager.HasIndex<MoodEffectPrototype>(args[1]))
        {
            shell.WriteError(Loc.GetString("addmood-command-error-unknown-mood", ("moodId", args[1])));
            return;
        }

        _moodSystem.AddEffect(uid, args[1]);
        shell.WriteLine(Loc.GetString("addmood-command-success", ("uid", uid), ("moodId", args[1])));
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class RemoveMoodEffectCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MoodSystem _moodSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "removemood";
    public override string Description => Loc.GetString("removemood-command-description");
    public override string Help => Loc.GetString("removemood-command-help", ("command", Command));

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.Components<MoodComponent>(args[0], EntityManager),
                Loc.GetString("removemood-command-hint-entity")),
            2 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<MoodEffectPrototype>(),
                Loc.GetString("removemood-command-hint-mood")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("removemood-command-error-invalid-uid"));
            return;
        }

        if (!EntityManager.HasComponent<MoodComponent>(uid))
        {
            shell.WriteError(Loc.GetString("removemood-command-error-missing-component"));
            return;
        }

        if (!_prototypeManager.HasIndex<MoodEffectPrototype>(args[1]))
        {
            shell.WriteError(Loc.GetString("removemood-command-error-unknown-mood", ("moodId", args[1])));
            return;
        }

        _moodSystem.RemoveEffect(uid, args[1]);
        shell.WriteLine(Loc.GetString("removemood-command-success", ("uid", uid), ("moodId", args[1])));
    }
}
