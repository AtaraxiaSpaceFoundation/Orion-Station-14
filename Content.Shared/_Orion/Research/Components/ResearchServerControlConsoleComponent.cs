using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ResearchServerControlConsoleComponent : Component
{
}

[RegisterComponent]
public sealed partial class ResearchServerControlStatusComponent : Component
{
    [DataField]
    public bool GenerationEnabled = true;
}

[NetSerializable, Serializable]
public enum ResearchServerControlUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ToggleServerGenerationMessage : BoundUserInterfaceMessage
{
    public int ServerId;

    public ToggleServerGenerationMessage(int serverId)
    {
        ServerId = serverId;
    }
}

[Serializable, NetSerializable]
public sealed class ResearchServerControlBoundInterfaceState : BoundUserInterfaceState
{
    public List<ResearchServerControlEntry> Servers;

    public ResearchServerControlBoundInterfaceState(List<ResearchServerControlEntry> servers)
    {
        Servers = servers;
    }
}

[Serializable, NetSerializable]
public sealed class ResearchServerControlEntry
{
    public int Id;
    public string Name;
    public bool GenerationEnabled;
    public int TotalPointsPerSecond;
    public List<ResearchPointAmount> PointGeneration;
    public List<ResearchPointAmount> Balances;
    public int LogCount;

    public ResearchServerControlEntry(int id, string name, bool generationEnabled, int totalPointsPerSecond, List<ResearchPointAmount> pointGeneration, List<ResearchPointAmount> balances, int logCount)
    {
        Id = id;
        Name = name;
        GenerationEnabled = generationEnabled;
        TotalPointsPerSecond = totalPointsPerSecond;
        PointGeneration = pointGeneration;
        Balances = balances;
        LogCount = logCount;
    }
}
