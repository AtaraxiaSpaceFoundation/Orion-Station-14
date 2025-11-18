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

    /// <summary>
    /// Protect Emotes from macros retards.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionEnabled =
        CVarDef.Create("protection.emote_protection", true, CVar.SERVERONLY);

    /*
     * Server Protection - Configuration
     */

    #region Chat Protection
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
    #endregion

    #region Emote Protection
    /// <summary>
    /// Ban the player when violating emote rules.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionBanEnabled =
        CVarDef.Create("protection.emote_ban", true, CVar.SERVERONLY);

    /// <summary>
    /// Kick the player (if ban is disabled) when violating emote rules.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionKickEnabled =
        CVarDef.Create("protection.emote_kick", false, CVar.SERVERONLY);

    /// <summary>
    /// Erase the character (delete entity, wipe mind, etc.) when violating emote rules.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionEraseEnabled =
        CVarDef.Create("protection.emote_erase", true, CVar.SERVERONLY);

    /// <summary>
    /// Delete all chat messages by the violating player.
    /// </summary>
    public static readonly CVarDef<bool> EmoteProtectionDeleteMessages =
        CVarDef.Create("protection.emote_delete_messages", true, CVar.SERVERONLY);

    /// <summary>
    /// Hard threshold for emote spam. If exceeded, immediate action is taken.
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionHardThreshold =
        CVarDef.Create("protection.emote_hard_threshold", 400, CVar.SERVERONLY);

    /// <summary>
    /// Variance for soft threshold calculation (random reduction from base threshold).
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionSoftVariance =
        CVarDef.Create("protection.emote_soft_variance", 5, CVar.SERVERONLY);

    /// <summary>
    /// Probability multiplier per step above soft threshold (e.g. 0.08 = 8% per step).
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionPostSoftProbability =
        CVarDef.Create("protection.emote_post_soft_probability", 0.08f, CVar.SERVERONLY);

    /// <summary>
    /// Cooldown in seconds before soft threshold is recalculated.
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionSoftRefreshCooldown =
        CVarDef.Create("protection.emote_soft_refresh_cooldown", 34f, CVar.SERVERONLY);

    /// <summary>
    /// Interval in seconds after which emote count is reset for all players.
    /// </summary>
    public static readonly CVarDef<float> EmoteProtectionClearInterval =
        CVarDef.Create("protection.emote_clear_interval", 20f, CVar.SERVERONLY);
    #endregion

    /*
     * Server Protection - Settings
     */

    /// <summary>
    /// Duration of the ban in seconds for chat violations. Set to 0 for permanent ban.
    /// </summary>
    public static readonly CVarDef<int> ChatProtectionBanDuration =
        CVarDef.Create("protection.chat_ban_duration", 0, CVar.SERVERONLY);

    /// <summary>
    /// Duration of the ban in seconds for emote violations. Set to 0 for permanent ban.
    /// </summary>
    public static readonly CVarDef<int> EmoteProtectionBanDuration =
        CVarDef.Create("protection.emote_ban_duration", 0, CVar.SERVERONLY);
}
