using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /// <summary>
    ///     When false - dont show combat indicator.
    /// </summary>
    public static readonly CVarDef<bool> CombatIndicator =
        CVarDef.Create("accessibility.CombatIndicator", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
