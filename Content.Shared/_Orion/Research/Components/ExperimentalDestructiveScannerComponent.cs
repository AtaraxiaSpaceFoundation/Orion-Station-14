using Robust.Shared.Audio;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentalDestructiveScannerComponent : Component
{
    [DataField]
    public string ContainerId = "experimental-destructive-scanner-container";

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(3.5f);

    [DataField]
    public TimeSpan CapsuleStepDuration = TimeSpan.FromSeconds(1.15f);

    public bool IsProcessing;

    public string LastSubject = string.Empty;

    public string LastResult = string.Empty;

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

    [DataField]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(-8f).WithVariation(0.25f);
}
