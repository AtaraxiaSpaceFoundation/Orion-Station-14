using Content.Shared.Cargo.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VendingMachinePricingComponent : Component
{
    /// <summary>
    /// If true, all products from this vending machine are treated as free regardless of configured prices.
    /// </summary>
    [DataField]
    public bool AllProductsFree;

    /// <summary>
    /// Station department account that receives funds from successful purchases.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? DepartmentAccount;

    /// <summary>
    /// Fallback default price for regular inventory entries when pack prototype does not define one.
    /// </summary>
    [DataField]
    public int DefaultPrice;

    /// <summary>
    /// Fallback default price for emagged / contraband inventory entries when pack prototype does not define one.
    /// </summary>
    [DataField]
    public int ExtraPrice;
}
