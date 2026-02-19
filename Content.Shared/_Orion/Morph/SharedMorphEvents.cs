using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Morph;

public sealed partial class MorphReproduceActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class MorphReproduceDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class MorphMimicryRememberActionEvent : EntityTargetActionEvent;

public sealed partial class MorphOpenRadialMenuEvent : InstantActionEvent;

public sealed partial class MorphAmbushActionEvent : InstantActionEvent;

public sealed partial class MorphVentOpenActionEvent : EntityTargetActionEvent;
