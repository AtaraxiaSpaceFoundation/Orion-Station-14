using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Orion.Arrivals.CentComm.Components;

[RegisterComponent]
public sealed partial class CentCommStationComponent : Component
{
    /// <summary>
    /// Keeps track of the internal event scheduler.
    /// </summary>
    [ViewVariables]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEventTick;
}
