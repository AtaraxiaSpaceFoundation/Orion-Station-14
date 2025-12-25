using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class InducerComponent : Component
{
    [DataField]
    public string PowerCellSlotId = "inducer_power_cell_slot";

    [DataField]
    public float TransferRate;

    [DataField]
    public List<float> AvailableTransferRates = new();

    [DataField]
    public float TransferDelay;

    [DataField]
    public float MaxDistance;
}

[Serializable, NetSerializable]
public sealed partial class InducerDoAfterEvent : SimpleDoAfterEvent
{
}
