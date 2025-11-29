using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /// <summary>
    ///     Enable or disable Patron functions.
    /// </summary>
    public static readonly CVarDef<bool> PatronEnabled =
        CVarDef.Create("support.patron_enabled", false, CVar.SERVER | CVar.REPLICATED);
}
