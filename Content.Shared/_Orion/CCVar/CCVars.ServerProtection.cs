using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /*
     * Server Protection
     */

    /// <summary>
    /// Protect chat from retards.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionEnabled =
        CVarDef.Create("protection.chat_protection", true, CVar.SERVERONLY);
}
