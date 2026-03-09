namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class DestructiveAnalyzerComponent : Component
{
    [DataField]
    public string ContainerId = "destructive-analyzer-container";

    [DataField]
    public float InsertAnimationSeconds = 0.4f;

    [DataField]
    public float DeconstructAnimationSeconds = 1f;

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
