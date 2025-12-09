using Robust.Shared.Prototypes;

//
// License-Identifier: MIT
//

namespace Content.Server._Orion.StationGoal;

/// <summary>
///     if attached to a station prototype, will send the station a random goal from the list
/// </summary>
[RegisterComponent]
public sealed partial class StationGoalComponent : Component
{
    [DataField]
    public List<ProtoId<StationGoalPrototype>> Goals = new();
}
