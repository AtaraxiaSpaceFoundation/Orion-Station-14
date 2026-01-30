using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment;

[Serializable, NetSerializable]
public sealed class RecruitmentConfirmationBuiState : BoundUserInterfaceState
{
    public string OrganizationName = string.Empty;
    public string ImplantName = string.Empty;
}

[Serializable, NetSerializable]
public sealed class RecruitmentAcceptMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RecruitmentDeclineMessage : BoundUserInterfaceMessage;
