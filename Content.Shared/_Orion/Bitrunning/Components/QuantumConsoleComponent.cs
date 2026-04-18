using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class QuantumConsoleComponent : Component
{
    [DataField]
    public float LinkRange = 1.5f;
}
