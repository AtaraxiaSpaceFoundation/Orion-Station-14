namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentatorComponent : Component
{
    [DataField]
    public string ContainerId = "experimentator-container";

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public TimeSpan CapsuleStepDuration = TimeSpan.FromSeconds(1.2f);

    [DataField]
    public bool IsProcessing;

    [DataField]
    public string LastSubject = string.Empty;

    [DataField]
    public string LastResult = string.Empty;
}
