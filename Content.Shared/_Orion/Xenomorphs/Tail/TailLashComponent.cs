using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Xenomorphs.Tail;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class TailLashComponent : Component
{
    [DataField]
    public EntProtoId? TailLashActionId = "ActionTailLash";

    [ViewVariables]
    public EntityUid? TailLashAction;

    [DataField, AutoNetworkedField]
    public EntProtoId TailAnimationId = "WeaponArcXenomorphTail";

    [DataField]
    public float LashRange = 2f;

    [DataField]
    public SoundSpecifier LashSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg", AudioParams.Default.WithVolume(-3));
}

public sealed partial class TailLashActionEvent : InstantActionEvent;
