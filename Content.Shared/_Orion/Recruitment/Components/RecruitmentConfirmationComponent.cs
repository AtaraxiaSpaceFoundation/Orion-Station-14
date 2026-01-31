using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Recruitment.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RecruitmentConfirmationComponent : Component
{
    [DataField] public EntityUid Scanner;
    [DataField] public EntityUid Target;
    [DataField] public EntityUid Recruiter;
    [DataField] public string OrganizationName = string.Empty;
    [DataField] public string ImplantName = string.Empty;
}
