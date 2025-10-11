namespace Content.Server.EnergyDome;

//
// License-Identifier: MIT
//

/// <summary>
/// marker component that allows linking the dome generator with the dome itself
/// </summary>

[RegisterComponent, Access(typeof(EnergyDomeSystem))]
public sealed partial class EnergyDomeComponent : Component
{
    /// <summary>
    /// A linked generator that uses energy
    /// </summary>
    [DataField]
    public EntityUid? Generator;
}
