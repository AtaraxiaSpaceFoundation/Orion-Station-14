using Content.Goobstation.Maths.FixedPoint;

namespace Content.Server._Orion.Chemistry.Components;

/// <summary>
/// Enables dynamic buffer capacity for ChemMaster based on the first two
/// FitsInDispenser machine parts inserted during construction.
/// Capacity = sum(beaker.MaxVol) * Multiplier.
/// </summary>
[RegisterComponent]
public sealed partial class ChemMasterBeakerCapacityComponent : Component
{
    [DataField]
    public float Multiplier = 10f;

    [DataField]
    public FixedPoint2 FallbackCapacity = FixedPoint2.New(1000);

    public bool InitializedFromConstructionBeakers;
}
