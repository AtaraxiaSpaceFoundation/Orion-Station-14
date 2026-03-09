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
public sealed class DestructiveAnalyzerBoundInterfaceState : BoundUserInterfaceState
{
    public string? ConnectedServerName;
    public List<ResearchPointAmount> PointBalances;
    public string LastSubject;
    public string LastResult;

    public DestructiveAnalyzerBoundInterfaceState(string? connectedServerName,
        List<ResearchPointAmount> pointBalances,
        string lastSubject,
        string lastResult)
    {
        ConnectedServerName = connectedServerName;
        PointBalances = pointBalances;
        LastSubject = lastSubject;
        LastResult = lastResult;
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
    public List<ExperimentatorOperationUiData> Operations;

    public ExperimentatorBoundInterfaceState(string? connectedServerName,
        List<ResearchPointAmount> pointBalances,
        string lastSubject,
        string lastResult,
        List<ExperimentatorOperationUiData> operations)
    {
        ConnectedServerName = connectedServerName;
        PointBalances = pointBalances;
        LastSubject = lastSubject;
        LastResult = lastResult;
        Operations = operations;
    }
}
