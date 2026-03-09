namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class DestructiveAnalyzerComponent : Component
{
    [DataField]
    public string ContainerId = "destructive-analyzer-container";

    [DataField]
    public TimeSpan InsertAnimationDuration = TimeSpan.FromSeconds(1.0f);

    [DataField]
    public TimeSpan DeconstructAnimationDuration = TimeSpan.FromSeconds(2.43f);

    [DataField]
    public EntityUid? InsertedItem;

    [DataField]
    public string? SelectedMethod;

    [DataField]
    public bool IsProcessing;

    [DataField]
    public bool LastItemAnalyzed;

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
