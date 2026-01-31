namespace Content.Shared._Orion.Recruitment.Components;

[RegisterComponent]
public sealed partial class RecruitedComponent : Component
{
    [DataField]
    public string Organization = string.Empty;

    [ViewVariables]
    public EntityUid RecruitedBy;

    [ViewVariables]
    public TimeSpan RecruitedAt;
}
