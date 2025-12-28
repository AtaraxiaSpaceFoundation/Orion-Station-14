using Content.Server._Orion.Chemistry.Systems;
using Content.Shared._Orion.Chemistry;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Server._Orion.Chemistry.Components;

/// <summary>
/// A machine that dispenses reagents into a solution container from containers in its storage slots.
/// </summary>
[RegisterComponent]
[Access(typeof(EnergyReagentDispenserSystem))]
public sealed partial class EnergyReagentDispenserComponent : Component
{
    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    ///     Current dispense amount. Don't worry about it and don't touch it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EnergyReagentDispenserDispenseAmount DispenseAmount = EnergyReagentDispenserDispenseAmount.U10;

    /// <summary>
    ///     Sound played when there's no power.
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier PowerSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    /// <summary>
    ///     The reagents themselves. Specify as (ID): (price)
    /// </summary>
    [DataField]
    public Dictionary<string, float> Reagents = [];

    /// <summary>
    ///     Additional reagents added when emagged.
    /// </summary>
    [DataField]
    public Dictionary<string, float>? ReagentsEmagged = [];

    /// <summary>
    ///     Cannot be emagged when already activated.
    /// </summary>
    [DataField]
    public bool Emagged;
}
