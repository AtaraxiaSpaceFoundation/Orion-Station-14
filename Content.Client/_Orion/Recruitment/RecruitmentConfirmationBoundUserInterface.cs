using Content.Client._Orion.Recruitment.UI;
using Content.Shared._Orion.Recruitment;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Recruitment;

public sealed class RecruitmentConfirmationBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RecruitmentConfirmationWindow? _window;

    public RecruitmentConfirmationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RecruitmentConfirmationWindow>();
        _window.OnAcceptPressed += OnAccept;
        _window.OnDeclinePressed += OnDecline;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RecruitmentConfirmationBuiState buiState)
        {
            _window?.UpdateState(buiState);
        }
    }

    private void OnAccept()
    {
        SendMessage(new RecruitmentAcceptMessage());
        Close();
    }

    private void OnDecline()
    {
        SendMessage(new RecruitmentDeclineMessage());
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _window == null)
            return;

        _window.OnAcceptPressed -= OnAccept;
        _window.OnDeclinePressed -= OnDecline;
    }
}
