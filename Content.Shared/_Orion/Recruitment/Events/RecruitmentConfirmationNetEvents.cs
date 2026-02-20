using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment.Events;

[Serializable, NetSerializable]
public sealed class RecruitmentOpenConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner { get; init; }
    public string OrganizationName { get; init; } = string.Empty;
    public string ImplantName { get; init; } = string.Empty;
}

[Serializable, NetSerializable]
public sealed class RecruitmentRespondConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner { get; init; }
    public bool Accepted { get; init; }
}
