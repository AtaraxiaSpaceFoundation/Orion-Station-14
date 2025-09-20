using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    public static readonly CVarDef<int> ResponseForceDelay =
        CVarDef.Create("specforce.delay", 2, CVar.SERVERONLY);
}
