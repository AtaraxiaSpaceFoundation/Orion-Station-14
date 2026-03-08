namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ResearchAnalyzableComponent : Component
{
    [DataField]
    public List<ResearchPointAmount> DestructiveReward = new();

    [DataField]
    public string? DiscoveryTrigger;

    [DataField]
    public List<string> ExperimentActions = new();
}
