using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Recruitment.Components;

/// <summary>
///     Temporary component attached to a recruitment scanner to store confirmation data.
///     Holds information about the target, recruiter, organization, and implant during the confirmation process.
///     Removed after the target accepts or declines recruitment.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RecruitmentConfirmationComponent : Component
{
    [DataField] public EntityUid Scanner;
    [DataField] public EntityUid Target;
    [DataField] public EntityUid Recruiter;
    [DataField] public string OrganizationName = string.Empty;
    [DataField] public string ImplantName = string.Empty;
}
