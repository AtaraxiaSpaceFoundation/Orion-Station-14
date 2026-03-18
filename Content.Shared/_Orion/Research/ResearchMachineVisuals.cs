using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research;

[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisualState : byte
{
    Idle,
    Inserting,
    Loaded,
    Deconstructing,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisualState : byte
{
    Idle,
    Up,
    Down,
    Scanning,
}


[Serializable, NetSerializable]
public enum DestructiveAnalyzerVisualLayers : byte
{
    Base,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerVisualLayers : byte
{
    Base,
}
