using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research;

[Serializable, NetSerializable]
public enum ResearchDiscoveryEventType : byte
{
    ScanEntity,
    MachineInsertion,
    DeconstructEntity,
    ServerTrigger,
}

[Serializable, NetSerializable]
public enum ResearchTechnologyVisibilityState : byte
{
    Hidden,
    RevealedLocked,
    Available,
    Researched,
}
