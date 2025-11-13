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
    /// Protect IC and OOC chat from retards.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionEnabled =
        CVarDef.Create("protection.chat_protection", true, CVar.SERVERONLY);

    /*
     * Server Protection - Configuration
     */

    /// <summary>
    /// Ban the player when violating chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionBanEnabled =
        CVarDef.Create("protection.chat_ban", false, CVar.SERVERONLY);

    /// <summary>
    /// Kick the player (if ban is disabled) when violating chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionKickEnabled =
        CVarDef.Create("protection.chat_kick", true, CVar.SERVERONLY);

    /// <summary>
    /// Erase the character (delete entity, wipe mind, etc.) when violating IC chat rules.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionEraseEnabled =
        CVarDef.Create("protection.chat_erase", false, CVar.SERVERONLY);

    /// <summary>
    /// Delete all chat messages by the violating player.
    /// </summary>
    public static readonly CVarDef<bool> ChatProtectionDeleteMessages =
        CVarDef.Create("protection.chat_delete_messages", false, CVar.SERVERONLY);

    /*
     * Server Protection - Settings
     */

    /// <summary>
    /// Duration of the ban in seconds for chat violations. Set to 0 for permanent ban.
    /// </summary>
    public static readonly CVarDef<int> ChatProtectionBanDuration =
        CVarDef.Create("protection.chat_ban_duration", 0, CVar.SERVERONLY);
}
