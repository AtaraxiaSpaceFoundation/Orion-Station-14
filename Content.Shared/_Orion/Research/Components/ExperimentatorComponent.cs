namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentatorComponent : Component
{
    [DataField]
    public string ContainerId = "experimentator-container";

    [DataField]
    public float ScanDurationSeconds = 1.5f;

    [DataField]
    public float CapsuleStepDurationSeconds = 0.25f;

    [DataField]
    public bool IsProcessing;

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
