using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server._Orion.Chemistry.Components;

/// <summary>
/// Enables dynamic buffer capacity for ChemMaster based on two beakers
/// installed in internal capacity slots.
/// Capacity = (beaker1.MaxVol + beaker2.MaxVol) * Multiplier.
/// </summary>
[RegisterComponent]
public sealed partial class ChemMasterBeakerCapacityComponent : Component
{
    [DataField]
    public string CapacitySlot1 = "capacityBeaker1";

    [DataField]
    public string CapacitySlot2 = "capacityBeaker2";

    /// <summary>
    /// Multiplier applied to the sum of both beakers' max volumes.
    /// </summary>
    [DataField]
    public float Multiplier = 10f;

    /// <summary>
    /// Fallback buffer capacity when no beakers are present.
    /// </summary>
    [DataField]
    public FixedPoint2 FallbackCapacity = FixedPoint2.New(1000);
}
