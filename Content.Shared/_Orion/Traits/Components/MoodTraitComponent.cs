using Content.Shared._Orion.Mood;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Traits.Components;

[RegisterComponent]
public sealed partial class MoodTraitComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<MoodEffectPrototype>> MoodEffects = new();
}
