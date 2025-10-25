using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> CombatModeSoundEnabled =
        CVarDef.Create("audio.combat_mode_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
