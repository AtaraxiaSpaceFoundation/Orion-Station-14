using Robust.Shared.Prototypes;

namespace Content.Server._Europa.ResponseForce;

[RegisterComponent]
public sealed partial class ResponseForceComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? ActionFTLName { get; private set; }

    /// <summary>
    /// A dictionary mapping the component type list to the YAML mapping containing their settings.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    public EntityUid? FTLKey = null;
}

public sealed class ResponseForceHistory
{
    public TimeSpan RoundTime {get;set;}
    public string Event {get;set;} = default!;
    public string WhoCalled {get;set;} = default!;
}
