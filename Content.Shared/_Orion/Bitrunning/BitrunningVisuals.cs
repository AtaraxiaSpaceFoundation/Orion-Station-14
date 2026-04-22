using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[Serializable, NetSerializable]
public enum QuantumServerVisualState : byte
{
    Unpowered,
    Cooling,
    Running,
}

[Serializable, NetSerializable]
public enum BitrunningVisuals : byte
{
    QuantumServerState,
    ByteforgePowered,
    ByteforgeActive,
    ByteforgeAngry,
}
