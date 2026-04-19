namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent]
public sealed partial class QuantumConsoleComponent : Component
{
    [DataField]
    public float LinkRange = 4f;

    [ViewVariables]
    public EntityUid? LinkedServerId;
}
