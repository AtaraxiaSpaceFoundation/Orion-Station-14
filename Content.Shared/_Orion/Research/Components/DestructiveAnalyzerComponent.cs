namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class DestructiveAnalyzerComponent : Component
{
    [DataField]
    public EntityUid? InsertedItem;

    [DataField]
    public string? SelectedMethod;

    [DataField]
    public bool LastItemAnalyzed;

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
