using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research;

[Serializable, NetSerializable]
public enum ResearchTechnologyLockReason : byte
{
    None,
    Hidden,
    MissingDiscovery,
    MissingPrerequisites,
    MissingExperiments,
    InsufficientPoints,
    AlreadyResearched,
    NotSupported,
}

[Serializable, NetSerializable]
public enum ResearchExperimentState : byte
{
    Unavailable,
    Available,
    Active,
    Completed,
    Skipped,
}
