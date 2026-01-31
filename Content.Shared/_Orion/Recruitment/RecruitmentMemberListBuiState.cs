using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment;

[Serializable, NetSerializable]
public sealed class RecruitmentMemberListBuiState : BoundUserInterfaceState
{
    public string OrganizationName;
    public RecruitedMemberData[] Members;

    public RecruitmentMemberListBuiState(string organizationName, RecruitedMemberData[] members)
    {
        OrganizationName = organizationName;
        Members = members;
    }

    [Serializable, NetSerializable]
    public sealed record RecruitedMemberData(
        string Name,
        string RecruitedBy,
        TimeSpan RecruitedAt
    );
}
