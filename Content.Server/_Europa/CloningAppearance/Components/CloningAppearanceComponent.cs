using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.CloningAppearance.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent]
public sealed partial class CloningAppearanceComponent : Component
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField("startingGear")]
    public ProtoId<StartingGearPrototype>? Gear;
}
