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
    /// Explicit per-entity override for free vending behavior.
    /// Null means mapload auto-detection may set <see cref="AllProductsFree"/> for off-station map entities.
    /// </summary>
    [DataField]
    public bool? AllProductsFreeOverride;

    /// <summary>
    /// Station department account that receives funds from successful purchases.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? DepartmentAccount;

    /// <summary>
    /// Salary department account that receives discounted regular products.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? DiscountDepartment;

    /// <summary>
    /// Multiplier applied to regular product prices for matching department employees.
    /// </summary>
    [DataField]
    public float? DepartmentDiscount;

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
