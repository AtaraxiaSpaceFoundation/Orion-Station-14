using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Research.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExperiScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ScanRange = 2f;

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public SoundSpecifier FailureSound = new SoundPathSpecifier("/Audio/Machines/scanbuzz.ogg");

    [DataField]
    public string LastResult = string.Empty;
}
