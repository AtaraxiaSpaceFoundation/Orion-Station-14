using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research.Components;

[Serializable, NetSerializable]
public enum DestructiveAnalyzerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum ExperimentalDestructiveScannerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum ExperiScannerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class OpenResearchServerMenuMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ExperimentalDestructiveScannerPerformMessage : BoundUserInterfaceMessage
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
public sealed class ExperimentalDestructiveScannerBoundInterfaceState : BoundUserInterfaceState
{
    public string? ConnectedServerName;
    public List<ResearchPointAmount> PointBalances;
    public string LastSubject;
    public string LastResult;
    public List<ResearchMachineExperimentUiData> Experiments;
    public string? InsertedItem;

    public ExperimentalDestructiveScannerBoundInterfaceState(string? connectedServerName,
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
public sealed class ExperiScannerBoundInterfaceState : BoundUserInterfaceState
{
    public string? ConnectedServerName;
    public List<ResearchMachineExperimentUiData> Experiments;
    public string LastResult;

    public ExperiScannerBoundInterfaceState(string? connectedServerName,
        List<ResearchMachineExperimentUiData> experiments,
        string lastResult)
    {
        ConnectedServerName = connectedServerName;
        Experiments = experiments;
        LastResult = lastResult;
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
    public string Goal;

    public ResearchMachineExperimentUiData(string id, string name, string description, int progress, int target, string objective, string goal)
    {
        Id = id;
        Name = name;
        Description = description;
        Progress = progress;
        Target = target;
        Objective = objective;
        Goal = goal;
    }
}
