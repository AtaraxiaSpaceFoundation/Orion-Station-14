using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment;

[Serializable, NetSerializable]
public sealed class RecruitmentMemberListBuiState : BoundUserInterfaceState
{
    public LocId OrganizationName { get; }
    public IReadOnlyList<RecruitedMemberData> Members { get; }

    public RecruitmentMemberListBuiState(string organizationName, IReadOnlyList<RecruitedMemberData> members)
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
