using Content.Client._Orion.Recruitment.UI;
using Content.Shared._Orion.Recruitment;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Recruitment;

public sealed class RecruitmentMemberListBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RecruitmentMemberListWindow? _window;

    public RecruitmentMemberListBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<RecruitmentMemberListWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RecruitmentMemberListBuiState buiState)
        {
            _window?.UpdateState(buiState);
        }
    }
}
