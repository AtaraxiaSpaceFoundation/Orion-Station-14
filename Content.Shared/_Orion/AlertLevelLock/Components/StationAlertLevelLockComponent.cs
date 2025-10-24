using Robust.Shared.GameStates;

namespace Content.Shared._Orion.AlertLevelLock.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
/// Component that locks entities based on the current station alert level.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAlertLevelLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    /// <summary>
    /// Set of alert levels that will cause this entity to be locked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> LockedAlertLevels = [];

    [DataField, AutoNetworkedField]
    public EntityUid? StationId;
}
