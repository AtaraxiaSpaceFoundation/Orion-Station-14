using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /*
     * Station time
     */

    /// <summary>
    /// Offset in years added to the current year for dynamic station year calculation.
    /// Used only when <see cref="StationTimeUseStaticYear"/> is false.
    /// </summary>
    public static readonly CVarDef<int> StationTimeOffsetYears =
        CVarDef.Create("time.station_year_offset", 540, CVar.REPLICATED);

    /// <summary>
    /// Fixed year used when <see cref="StationTimeUseStaticYear"/> is true.
    /// Needs for forks to be compatible with lore, which doesn't use the TG-lore.
    /// </summary>
    public static readonly CVarDef<int> StationTimeStaticYear =
        CVarDef.Create("time.station_static_year", 2565, CVar.REPLICATED);

    /// <summary>
    /// If true, uses a fixed year from <see cref="StationTimeStaticYear"/>.
    /// </summary>
    public static readonly CVarDef<bool> StationTimeUseStaticYear =
        CVarDef.Create("time.station_use_static_year", false, CVar.REPLICATED);
}
