namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ResearchAnalyzableComponent : Component
{
    [DataField]
    public Dictionary<string, List<ResearchPointAmount>> MethodPointRewards = new();

    [DataField]
    public List<string> SupportedMethods = new();

    [DataField]
    public List<string> UnlockTechnologies = new();

    [DataField]
    public List<string> RevealTechnologies = new();

    [DataField]
    public string? DiscoveryTrigger;

    [DataField]
    public List<string> ExperimentActions = new();
}
