using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

[NetSerializable, Serializable]
public enum BitrunningServerState : byte
{
    Ready,
    Running,
    CoolingDown,
}

[NetSerializable, Serializable]
public enum BitrunningGrade : byte
{
    D,
    C,
    B,
    A,
    S,
}

[NetSerializable, Serializable]
public enum BitrunningDifficulty : byte
{
    Easy,
    Medium,
    Hard,
    Extreme,
}

[NetSerializable, Serializable]
public enum NetpodVisuals : byte
{
    State,
}

[NetSerializable, Serializable]
public enum NetpodVisualState : byte
{
    Open,
    Closed,
    Active,
    OpenActive,
    Opening,
    Closing,
}
