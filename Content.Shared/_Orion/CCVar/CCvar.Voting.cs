using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//
public sealed partial class CCVars
{

    /*
    * AUTOVOTE SYSTEM
    */

    /// <summary>
    /// Automatically starts a map vote when returning to the lobby.
    /// Requires auto voting to be enabled.
    /// </summary>
    public static readonly CVarDef<bool> MapAutoVoteEnabled =
        CVarDef.Create("vote.map_autovote_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Automatically starts a gamemode vote when returning to the lobby.
    /// Requires auto voting to be enabled.
    /// </summary>
    public static readonly CVarDef<bool> PresetAutoVoteEnabled =
        CVarDef.Create("vote.preset_autovote_enabled", true, CVar.SERVERONLY);
}

