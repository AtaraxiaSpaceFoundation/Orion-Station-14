using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server._Orion.Chemistry.Components;

/// <summary>
/// Enables dynamic buffer capacity for ChemMaster based on the first two
/// FitsInDispenser machine parts inserted during construction.
/// Capacity = sum(beaker.MaxVol) * Multiplier.
/// Transfer from construction beakers to buffer happens exactly once on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class ChemMasterBeakerCapacityComponent : Component
{
    [DataField]
    public float Multiplier = 10f;

    [DataField]
    public FixedPoint2 FallbackCapacity = FixedPoint2.New(1000);

    /// <summary>
    /// True after the one-time post-assembly transfer from construction beakers to buffer.
    /// Prevents repeated draining on subsequent events.
    /// </summary>
    public bool InitializedFromConstructionBeakers;
}
