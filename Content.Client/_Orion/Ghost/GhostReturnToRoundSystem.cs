using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _acc;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _acc += frameTime;
        if (_acc <= 1)
            return;

        _acc -= 1;

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        if (!TryComp<GhostComponent>(player, out var ghostComponent))
            return;

        var ui = _userInterfaceManager.GetActiveUIWidgetOrNull<GhostGui>();
        if (ui == null)
            return;

        var timeOffset = _gameTiming.CurTime - ghostComponent.TimeOfDeath;
        var respawnTime = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.GhostRespawnTime));
        if (timeOffset >= respawnTime)
        {
            if (!ui.ReturnToRound.Disabled)
                return;

            ui.ReturnToRound.Disabled = false;
            ui.ReturnToRound.Text = Loc.GetString("ghost-gui-return-to-round-button");

            return;
        }

        ui.ReturnToRound.Disabled = true;
        var timeLeft = respawnTime - timeOffset;
        ui.ReturnToRound.Text = Loc.GetString("ghost-gui-return-to-round-button", ("time", timeLeft.ToString("mm\\:ss")));
    }
}
