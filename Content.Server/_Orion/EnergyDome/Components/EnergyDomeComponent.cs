using Content.Server._Orion.EnergyDome.Systems;

namespace Content.Server._Orion.EnergyDome.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
/// Allows linking the dome generator with the dome itself
/// </summary>
[RegisterComponent, Access(typeof(EnergyDomeSystem))]
public sealed partial class EnergyDomeComponent : Component
{
    /// <summary>
    /// Linked generator that uses energy
    /// </summary>
    [DataField]
    public EntityUid? Generator;
}
