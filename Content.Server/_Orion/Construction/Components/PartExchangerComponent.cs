using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Construction.Components;

[RegisterComponent]
public sealed partial class PartExchangerComponent : Component
{
    [DataField]
    public float ExchangeDuration = 3f;

    [DataField]
    public bool DoDistanceCheck = true;

    [DataField]
    public bool RequireOpenPanel = true;

    [DataField]
    public SoundSpecifier ExchangeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [DataField]
    public EntProtoId? ExchangeBeamPrototype;
}
