using Content.Shared.Roles;

namespace Content.Server._Orion.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a morph.
/// </summary>
[RegisterComponent]
public sealed partial class MorphRoleComponent : BaseMindRoleComponent;
