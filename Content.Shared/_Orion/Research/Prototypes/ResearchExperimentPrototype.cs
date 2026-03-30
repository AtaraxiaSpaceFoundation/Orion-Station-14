using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research.Prototypes;

/// <summary>
/// Prototype definition for experiment progression in research techweb.
/// </summary>
[Prototype("researchExperiment")]
public sealed partial class ResearchExperimentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    [DataField]
    public LocId Description = string.Empty;

    [DataField]
    public string Category = "General";

    [DataField]
    public bool Hidden;

    [DataField]
    public bool Repeatable;

    [DataField]
    public bool StartingExperiment;

    /// <summary>
    /// Required technologies for the experiment to become available.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> RequiredTechnologies = new();

    /// <summary>
    /// Required experiments for this experiment to become available.
    /// </summary>
    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> RequiredExperiments = new();

    [DataField]
    public ExperimentSourceFlags SupportedSources = ExperimentSourceFlags.AnyScanner;

    [DataField(required: true)]
    public ExperimentObjective Objective = new ServerTriggerExperimentObjective();

    [DataField]
    public ExperimentReward Reward = new();

    [DataField]
    public Vector2i? Position;
}

[Serializable, NetSerializable]
public enum ExperimentObjectiveKind : byte
{
    ServerTrigger,
    ScanEntity,
    PresentItem,
    ScanDifferentEntities,
    ScanSamples,
    ActionCount,
    DebugManual,
}

[Flags]
public enum ExperimentSourceFlags : byte
{
    None = 0,
    ResearchConsole = 1 << 0,
    MachineScanner = 1 << 1,
    HandheldScanner = 1 << 2,
    AnyScanner = ResearchConsole | MachineScanner | HandheldScanner,
    DefaultScanner = ResearchConsole | MachineScanner,
}

[DataDefinition, Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial record ExperimentObjective
{
    [DataField]
    public ExperimentObjectiveKind Kind = ExperimentObjectiveKind.ServerTrigger;

    [DataField]
    public int Target = 1;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ServerTriggerExperimentObjective : ExperimentObjective
{
    [DataField]
    public string TriggerId = string.Empty;

    public ServerTriggerExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ServerTrigger;
    }
}

[DataDefinition, Serializable, NetSerializable]
public partial record ScanEntityExperimentObjective : ExperimentObjective
{
    [DataField]
    public List<string> RequiredEntityPrototypes = new();

    [DataField]
    public List<string> RequiredTags = new();

    [DataField]
    public List<string> RequiredComponents = new();

    [DataField]
    public List<ExperimentEntityCondition> RequiredConditions = new();

    [DataField]
    public string? RequiredReagent;

    [DataField]
    public float? MinReagentPurity;

    [DataField]
    public string? RequiredGas;

    [DataField]
    public float? MinGasPurity;

    [DataField]
    public float? MinExplosiveIntensity;

    public ScanEntityExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanEntity;
    }
}

[Serializable, NetSerializable]
public enum ExperimentEntityCondition : byte
{
    AnyFish,
    RareFish,
    IpcOrCyborg,
    HasAugmentedOrgans,
    NonBaselineHumanoid,
    Damaged,
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record PresentItemExperimentObjective : ScanEntityExperimentObjective
{
    public PresentItemExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.PresentItem;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ScanDifferentEntitiesExperimentObjective : ScanEntityExperimentObjective
{
    public ScanDifferentEntitiesExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanDifferentEntities;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ScanSamplesExperimentObjective : ScanEntityExperimentObjective
{
    public ScanSamplesExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ScanSamples;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ActionCountExperimentObjective : ExperimentObjective
{
    [DataField(required: true)]
    public string ActionId = string.Empty;

    public ActionCountExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.ActionCount;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record DebugManualExperimentObjective : ExperimentObjective
{
    public DebugManualExperimentObjective()
    {
        Kind = ExperimentObjectiveKind.DebugManual;
    }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial record ExperimentReward
{
    [DataField]
    public int ResearchPoints;

    [DataField]
    public List<ResearchPointAmount> PointRewards = new();

    [DataField]
    public int FlatDiscount;

    [DataField]
    public float PercentageDiscount;

    [DataField]
    public List<ProtoId<ResearchExperimentPrototype>> UnlockExperiments = new();

    [DataField]
    public List<ProtoId<TechnologyPrototype>> RevealTechnologies = new();

    [DataField]
    public bool InfrastructureUnlock;
}
