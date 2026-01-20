using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._Orion.Ghost;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly SharedGhostSystem _ghostSystem = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly ActorSystem _actorSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTrackings = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(ResetDeathTimes);

        Cfg.OnValueChanged(CCVars.GhostRespawnMaxPlayers,
            ghostRespawnMaxPlayers =>
            {
                _ghostRespawnMaxPlayers = ghostRespawnMaxPlayers;
            },
            true);

        _console.RegisterCommand("return_to_round", ReturnToRoundCommand, ReturnToRoundCompletion);
    }

    public void TryGhostReturnToRound(EntityUid uid, Entity<GhostComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (_playerManager.PlayerCount >= _ghostRespawnMaxPlayers)
        {
            SendChatMsg(ui.Player,
                Loc.GetString("ghost-respawn-max-players", ("players", _ghostRespawnMaxPlayers))
            );
            return;
        }

        var timeOffset = GameTiming.CurTime - ent.Comp.TimeOfDeath;
        if (timeOffset < GhostRespawnTime)
        {
            SendChatMsg(ui.Player,
                Loc.GetString("ghost-respawn-time-left", ("time", (GhostRespawnTime - timeOffset).ToString()))
            );
            return;
        }

        _deathTime.Remove(ui.Player.UserId);

        if (ui.Player != null)
            _gameTicker.Respawn(ui.Player);
    }

    private CompletionResult ReturnToRoundCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }

    [AnyCommand]
    private void ReturnToRoundCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } ghost || !TryComp<GhostComponent>(ghost, out var ghostComponent))
        {
            shell.WriteError("This command can only be run by a player with an attached entity.");
            return;
        }

        if (_playerManager.PlayerCount >= _ghostRespawnMaxPlayers)
        {
            SendChatMsg(shell.Player,
                Loc.GetString("ghost-respawn-max-players", ("players", _ghostRespawnMaxPlayers))
            );
            return;
        }

        var userId = shell.Player.UserId;

        if (!_deathTime.TryGetValue(userId, out var deathTime))
        {
            _deathTime[userId] = ghostComponent.TimeOfDeath;
            deathTime = ghostComponent.TimeOfDeath;
        }

        if (deathTime != ghostComponent.TimeOfDeath)
        {
            _ghostSystem.SetTimeOfDeath(ghost, deathTime, ghostComponent);
            Dirty(ghost, ghostComponent);
        }

        var timeLeft = GameTiming.CurTime - deathTime;
        SendChatMsg(shell.Player,
            Loc.GetString("ghost-respawn-time-left", ("time", (GhostRespawnTime - timeLeft).ToString()))
        );
    }

    private int _ghostRespawnMaxPlayers;
    private readonly Dictionary<NetUserId, TimeSpan> _deathTime = new();

    private void ResetDeathTimes(RoundRestartCleanupEvent ev)
    {
        _deathTime.Clear();
    }

    private void SendChatMsg(ICommonSession sess, string message)
    {
        _chatManager.ChatMessageToOne(ChatChannel.Server,
            message,
            Loc.GetString("chat-manager-server-wrap-message", ("message", message)),
            default,
            false,
            sess.Channel,
            Color.Red);
    }
}
