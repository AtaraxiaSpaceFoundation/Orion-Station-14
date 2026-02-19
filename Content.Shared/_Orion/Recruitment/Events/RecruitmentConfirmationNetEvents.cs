using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Recruitment.Events;

[Serializable, NetSerializable]
public sealed class RecruitmentOpenConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner;
    public string OrganizationName = string.Empty;
    public string ImplantName = string.Empty;
}

[Serializable, NetSerializable]
public sealed class RecruitmentRespondConfirmationEvent : EntityEventArgs
{
    public NetEntity Scanner;
    public bool Accepted;
}
