using Content.Shared.Damage;

namespace Content.Shared._Europa.Morph;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent]
public sealed partial class MorphAmbushComponent : Component
{
    /// <summary>
    ///     Stun time after touching, not hitting.
    /// </summary>
    [DataField]
    public TimeSpan StunTimeInteract = TimeSpan.FromSeconds(6);

    [DataField]
    public DamageSpecifier DamageOnTouch = new()
    {
        DamageDict = new()
        {
            { "Blunt", 20 },
            { "Slash", 20 },
        }
    };
}
