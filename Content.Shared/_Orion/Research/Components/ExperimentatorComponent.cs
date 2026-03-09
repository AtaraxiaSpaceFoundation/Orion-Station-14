namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentatorComponent : Component
{
    [DataField]
    public bool IsProcessing;

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
