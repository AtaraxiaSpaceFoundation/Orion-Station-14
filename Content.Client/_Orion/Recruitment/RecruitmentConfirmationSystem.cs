using Content.Client._Orion.Recruitment.UI;
using Content.Shared._Orion.Recruitment;
using Content.Shared._Orion.Recruitment.Events;

namespace Content.Client._Orion.Recruitment;

public sealed class RecruitmentConfirmationSystem : EntitySystem
{
    private RecruitmentConfirmationWindow? _window;
    private EntityUid? _scanner;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RecruitmentOpenConfirmationEvent>(OnOpenConfirmation);
    }

    public override void Shutdown()
    {
        CloseWindow();
        base.Shutdown();
    }

    private void OnOpenConfirmation(RecruitmentOpenConfirmationEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(ev.Scanner, out var scanner))
            return;

        CloseWindow();

        _scanner = scanner.Value;
        _window = new RecruitmentConfirmationWindow();
        _window.UpdateState(new RecruitmentConfirmationBuiState
        {
            OrganizationName = ev.OrganizationName,
            ImplantName = ev.ImplantName,
        });

        _window.OnAcceptPressed += OnAcceptPressed;
        _window.OnDeclinePressed += OnDeclinePressed;
        _window.OnClose += OnWindowClosed;
        _window.OpenCentered();
    }

    private void OnAcceptPressed()
    {
        SendResponse(true);
    }

    private void OnDeclinePressed()
    {
        SendResponse(false);
    }

    private void SendResponse(bool accepted)
    {
        if (_scanner == null)
            return;

        RaiseNetworkEvent(new RecruitmentRespondConfirmationEvent
        {
            Scanner = GetNetEntity(_scanner.Value),
            Accepted = accepted,
        });

        CloseWindow();
    }

    private void OnWindowClosed()
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
        if (_window == null)
            return;

        _window.OnAcceptPressed -= OnAcceptPressed;
        _window.OnDeclinePressed -= OnDeclinePressed;
        _window.OnClose -= OnWindowClosed;
        _window.Dispose();
        _window = null;
        _scanner = null;
    }
}
