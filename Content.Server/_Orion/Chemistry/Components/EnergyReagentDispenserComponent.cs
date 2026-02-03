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
    public ItemSlot EnergyBeakerSlot = new();

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// Текущая выдача. Не забивайте голову и не трогайте
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EnergyReagentDispenserDispenseAmount DispenseAmount = EnergyReagentDispenserDispenseAmount.U10;

    /// <summary>
    /// Звук отсутствия энергии
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier PowerSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    /// <summary>
    /// Сами реагенты. Указываеть как (Айди): (цена)
    /// </summary>
    [DataField]
    public Dictionary<string, float> Reagents = [];

    /// <summary>
    /// Добавление реагентов при емагу
    /// </summary>
    [DataField]
    public Dictionary<string, float>? ReagentsEmagged = [];

    /// <summary>
    /// При включении нельзя емагнуть
    /// </summary>
    [DataField]
    public bool Emagged;
}
