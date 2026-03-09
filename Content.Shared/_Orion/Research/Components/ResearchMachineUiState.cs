using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research.Components;

[Serializable, NetSerializable]
public enum DestructiveAnalyzerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum ExperimentatorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OpenResearchServerMenuMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ExperimentScannerPerformMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerEjectMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerSelectMethodMessage : BoundUserInterfaceMessage
{
    public string MethodId;

    public DestructiveAnalyzerSelectMethodMessage(string methodId)
    {
        MethodId = methodId;
    }
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerRunMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerBoundInterfaceState : BoundUserInterfaceState
{
    public string? ConnectedServerName;
    public List<ResearchPointAmount> PointBalances;
    public string LastSubject;
    public string LastResult;
    public string? InsertedItem;
    public NetEntity? InsertedItemEntity;
    public string? SelectedMethod;
    public List<string> Methods;

    public DestructiveAnalyzerBoundInterfaceState(string? connectedServerName,
        List<ResearchPointAmount> pointBalances,
        string lastSubject,
        string lastResult,
        string? insertedItem,
        NetEntity? insertedItemEntity,
        string? selectedMethod,
        List<string> methods)
    {
        ConnectedServerName = connectedServerName;
        PointBalances = pointBalances;
        LastSubject = lastSubject;
        LastResult = lastResult;
        InsertedItem = insertedItem;
        InsertedItemEntity = insertedItemEntity;
        SelectedMethod = selectedMethod;
        Methods = methods;
    }
}

[Serializable, NetSerializable]
public sealed class ExperimentatorOperationUiData
{
    public string OperationId;
    public List<ResearchPointAmount> SuccessRewards;
    public List<ResearchPointAmount> FailureRewards;
    public float SuccessChance;
    public float BackfireChance;

    public ExperimentatorOperationUiData(string operationId,
        List<ResearchPointAmount> successRewards,
        List<ResearchPointAmount> failureRewards,
        float successChance,
        float backfireChance)
    {
        OperationId = operationId;
        SuccessRewards = successRewards;
        FailureRewards = failureRewards;
        SuccessChance = successChance;
        BackfireChance = backfireChance;
    }
}

[Serializable, NetSerializable]
public sealed class ExperimentatorBoundInterfaceState : BoundUserInterfaceState
{
    public string? ConnectedServerName;
    public List<ResearchPointAmount> PointBalances;
    public string LastSubject;
    public string LastResult;
    public List<ResearchMachineExperimentUiData> Experiments;
    public string? InsertedItem;

    public ExperimentatorBoundInterfaceState(string? connectedServerName,
        List<ResearchPointAmount> pointBalances,
        string lastSubject,
        string lastResult,
        List<ResearchMachineExperimentUiData> experiments,
        string? insertedItem)
    {
        ConnectedServerName = connectedServerName;
        PointBalances = pointBalances;
        LastSubject = lastSubject;
        LastResult = lastResult;
        Experiments = experiments;
        InsertedItem = insertedItem;
    }
}

[Serializable, NetSerializable]
public sealed class ResearchMachineExperimentUiData
{
    public string Id;
    public string Name;
    public string Description;
    public int Progress;
    public int Target;
    public string Objective;

    public ResearchMachineExperimentUiData(string id, string name, string description, int progress, int target, string objective)
    {
        Id = id;
        Name = name;
        Description = description;
        Progress = progress;
        Target = target;
        Objective = objective;
    }
}
