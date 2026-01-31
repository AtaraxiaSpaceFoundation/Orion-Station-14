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

    private TimeSpan _lastTimeLeft = TimeSpan.Zero;
    private bool _lastButtonState = true;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        if (!TryComp<GhostComponent>(player, out var ghostComponent))
            return;

        var ui = _userInterfaceManager.GetActiveUIWidgetOrNull<GhostGui>();
        if (ui == null)
            return;

        var timeOffset = _gameTiming.CurTime - ghostComponent.TimeOfDeath;
        var rawTimeLeft = GhostRespawnTime - timeOffset;
        var timeLeft = rawTimeLeft > TimeSpan.Zero ? rawTimeLeft : TimeSpan.Zero;
        var canReturn = timeLeft == TimeSpan.Zero;

        var displayTime = timeLeft.ToString(@"mm\:ss");

        var buttonStateChanged = ui.ReturnToRound.Disabled == canReturn;
        var timeChanged = _lastTimeLeft.ToString(@"mm\:ss") != displayTime;

        if (!buttonStateChanged && !timeChanged)
            return;

        ui.ReturnToRound.Disabled = !canReturn;
        ui.ReturnToRound.Text = canReturn
            ? Loc.GetString("ghost-gui-return-to-round-ready-button")
            : Loc.GetString("ghost-gui-return-to-round-button", ("time", displayTime));

        _lastTimeLeft = timeLeft;
        _lastButtonState = !canReturn;
    }
}
