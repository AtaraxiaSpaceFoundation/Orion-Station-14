using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared._Orion.Ghost;
using Content.Shared.Ghost;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

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
        if (timeOffset >= GhostRespawnTime)
        {
            if (!ui.ReturnToRound.Disabled)
                return;

            ui.ReturnToRound.Disabled = false;
            ui.ReturnToRound.Text = Loc.GetString("ghost-gui-return-to-round-button");

            return;
        }

        ui.ReturnToRound.Disabled = true;
        ui.ReturnToRound.Text = Loc.GetString("ghost-gui-return-to-round-button") + " " + (GhostRespawnTime - timeOffset).ToString("mm\\:ss");
    }
}
