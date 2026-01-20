using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._Orion.Ghost;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
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
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private int _ghostRespawnMaxPlayers;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostReturnToRoundRequest>(OnGhostReturnToRoundRequest);

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

        _gameTicker.Respawn(session);
        _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", session.Name))}");

        var message= Loc.GetString("ghost-respawn-window-rules-footer");
        SendChatMsg(session, message);
    }

    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ghost)
            return;

        if (!TryComp<GhostComponent>(ghost, out var ghostComponent))
            return;

        TryGhostReturnToRound(ghost, (ghost, ghostComponent));
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

        TryGhostReturnToRound(ghost, (ghost, ghostComponent));
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
