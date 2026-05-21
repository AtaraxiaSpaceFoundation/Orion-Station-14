using Content.Shared._Orion.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent]
public sealed partial class ExperimentalDestructiveScannerComponent : Component
{
    [DataField]
    public string ContainerId = "experimental-destructive-scanner-container";

    [DataField]
    public TimeSpan ScanDuration = TimeSpan.FromSeconds(3.5f);

    [DataField]
    public TimeSpan BaseScanDuration = TimeSpan.FromSeconds(3.5f);

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

    [DataField]
    public float BaseFailureChance = 0.25f;

    [DataField]
    public float FinalFailureChance = 0.25f;

    [DataField]
    public float BaseScanQuality = 1f;

    [DataField]
    public float FinalScanQuality = 1f;

    [DataField]
    public ProtoId<MachinePartPrototype> ServoPart = "Servo";

    [DataField]
    public ProtoId<MachinePartPrototype> ScanningModulePart = "ScanningModule";

    [DataField]
    public ProtoId<MachinePartPrototype> MicroLaserPart = "MicroLaser";
}
