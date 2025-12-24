using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Time.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
///     Store station time. Automatically replicated to clients.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationTimeManagerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan StationTime;

    [DataField, AutoNetworkedField]
    public DateTime StationDate;
}
