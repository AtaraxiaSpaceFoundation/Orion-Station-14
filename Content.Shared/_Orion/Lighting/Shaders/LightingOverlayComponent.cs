using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Orion.Lighting.Shaders;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
/// This is used for LightOverlay
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LightingOverlayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool? Enabled;

    [DataField]
    public SpriteSpecifier Sprite = new Texture(new ResPath("_Orion/Effects/LightMasks/lightmask_lamp.png"));

    [DataField]
    public float OffsetX = -0.5f;

    [DataField]
    public float OffsetY = 0.5f;

    [DataField]
    public Color? Color;
}
