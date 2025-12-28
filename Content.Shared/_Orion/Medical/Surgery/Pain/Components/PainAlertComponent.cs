using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Medical.Surgery.Pain.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
///     Component that tracks pain levels for the pain alert system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PainAlertComponent : Component
{
    /// <summary>
    ///     Current pain level (0-100)
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float PainLevel;

    /// <summary>
    ///     Whether the pain alert is currently being shown
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool IsAlertActive;
}
