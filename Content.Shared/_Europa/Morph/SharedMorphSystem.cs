using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._Europa.Morph;

//
// License-Identifier: AGPL-3.0-or-later
//

public abstract class SharedMorphSystem : EntitySystem
{
    public override void Initialize() { }
}

[Serializable, NetSerializable]
public sealed partial class MorphDevourDoAfterEvent : SimpleDoAfterEvent { }
