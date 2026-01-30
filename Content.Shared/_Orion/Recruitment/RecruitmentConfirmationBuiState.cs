using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment;

[Serializable, NetSerializable]
public sealed class RecruitmentConfirmationBuiState : BoundUserInterfaceState
{
    public string RecruiterName;
    public string OrganizationName;
    public string ImplantName;
}

[Serializable, NetSerializable]
public sealed class RecruitmentAcceptMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RecruitmentDeclineMessage : BoundUserInterfaceMessage;
