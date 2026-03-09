using Robust.Shared.Audio;

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

    public EntityUid? InsertedItem;

    public string? SelectedMethod;

    public bool IsProcessing;

    public bool LastItemAnalyzed;

    public string LastSubject = string.Empty;

    public string LastResult = string.Empty;

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    [DataField]
    public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

    [DataField]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(-8f).WithVariation(0.25f);
}
