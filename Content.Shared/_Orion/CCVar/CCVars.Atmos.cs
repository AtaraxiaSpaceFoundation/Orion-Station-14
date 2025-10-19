using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether or not Space Wind will create subtle visual indicators for the presence of air currents.
    /// </summary>
    public static readonly CVarDef<bool> SpaceWindVisuals =
        CVarDef.Create("atmos.space_wind_visuals", true, CVar.SERVERONLY);
}
