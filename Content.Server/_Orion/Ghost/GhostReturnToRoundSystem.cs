using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._Orion.Ghost;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly SharedGhostSystem _ghostSystem = default!;

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

        _console.RegisterCommand("returntoround", ReturnToRoundCommand, ReturnToRoundCompletion);
    }

    public void TryGhostReturnToRound(EntityUid uid, Entity<GhostComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        if (_playerManager.PlayerCount >= _ghostRespawnMaxPlayers)
        {
            SendChatMsg(session,
                Loc.GetString("ghost-respawn-max-players", ("players", _ghostRespawnMaxPlayers))
            );
            return;
        }

        var timeOffset = GameTiming.CurTime - ent.Comp.TimeOfDeath;
        if (timeOffset < GhostRespawnTime)
        {
            SendChatMsg(session,
                Loc.GetString("ghost-respawn-time-left", ("time", (GhostRespawnTime - timeOffset).ToString()))
            );
            return;
        }

        _deathTime.Remove(session.UserId);

        _gameTicker.Respawn(session);
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

        TryGhostReturnToRound(ghost, (ghost, ghostComponent));
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
