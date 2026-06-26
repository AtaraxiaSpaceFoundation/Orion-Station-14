using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Items.OpenableHeldPrefix;

[RegisterComponent, NetworkedComponent]
public sealed partial class OpenableHeldPrefixComponent : Component
{
    [DataField]
    public string? OpenedPrefix = "open";

    [DataField]
    public string? ClosedPrefix = null;
}
