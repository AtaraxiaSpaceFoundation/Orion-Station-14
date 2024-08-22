using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> EnableLightsGlowing =
        CVarDef.Create("light.enable_lights_glowing", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
