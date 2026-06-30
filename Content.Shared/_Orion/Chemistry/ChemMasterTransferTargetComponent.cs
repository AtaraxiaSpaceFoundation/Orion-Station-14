namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Marker: generic solution transfer should treat this target specially.
/// Used by ChemMaster to avoid overfilling and to let inserted beakers transfer via slot logic.
/// </summary>
[RegisterComponent]
public sealed partial class ChemMasterTransferTargetComponent : Component
{
}
