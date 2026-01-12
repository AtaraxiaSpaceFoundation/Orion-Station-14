using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Access;

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

    /// <summary>
    /// List of access groups that can bypass the alert level lock.
    /// Each group is a set of access tags that all must be present for the user to bypass the lock.
    /// If any group matches completely, the user can bypass the lock.
    /// If empty, no one can bypass the lock with access.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<HashSet<ProtoId<AccessLevelPrototype>>> BypassAccessLists = new();
}
