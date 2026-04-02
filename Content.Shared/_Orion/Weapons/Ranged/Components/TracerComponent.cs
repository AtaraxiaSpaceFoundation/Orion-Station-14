using System.Numerics;

namespace Content.Shared._Orion.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class TracerComponent : Component
{
    [DataField]
    public float Lifetime = 10f;

    /// <summary>
    /// The maximum length of the tracer trail
    /// </summary>
    [DataField]
    public float Length = 2f;

    /// <summary>
    /// Color of the tracer line effect
    /// </summary>
    [DataField]
    public Color Color = Color.Red;

    [ViewVariables]
    public TracerData Data = default!;
}

[DataRecord]
public sealed partial class TracerData(List<Vector2> positionHistory, TimeSpan endTime)
{
    public List<Vector2> PositionHistory = positionHistory;

    /// <summary>
    /// When this tracer effect should end
    /// </summary>
    public TimeSpan EndTime = endTime;
}
