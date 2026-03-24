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

    #region Admin Action Protection
    /// <summary>
    /// Protect the server from suspiciously aggressive admin actions.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionEnabled =
        CVarDef.Create("protection.admin_action_enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// Number of bans in the configured window that triggers an alert.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionBanThreshold =
        CVarDef.Create("protection.admin_action_ban_threshold", 5, CVar.SERVERONLY);

    /// <summary>
    /// Ban action window in seconds for sabotage detection.
    /// </summary>
    public static readonly CVarDef<float> AdminActionProtectionBanWindowSeconds =
        CVarDef.Create("protection.admin_action_ban_window_seconds", 220f, CVar.SERVERONLY);

    /// <summary>
    /// Number of permission-removal actions in the configured window that triggers an alert.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionPermissionThreshold =
        CVarDef.Create("protection.admin_action_permission_threshold", 3, CVar.SERVERONLY);

    /// <summary>
    /// Permission-removal action window in seconds for sabotage detection.
    /// </summary>
    public static readonly CVarDef<float> AdminActionProtectionPermissionWindowSeconds =
        CVarDef.Create("protection.admin_action_permission_window_seconds", 120f, CVar.SERVERONLY);

    /// <summary>
    /// Cooldown in seconds between repeated sabotage alerts for the same admin and action type.
    /// </summary>
    public static readonly CVarDef<float> AdminActionProtectionAlertCooldownSeconds =
        CVarDef.Create("protection.admin_action_alert_cooldown_seconds", 180f, CVar.SERVERONLY);

    /// <summary>
    /// Automatically de-admin a suspected sabotage admin.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionAutoDeAdminEnabled =
        CVarDef.Create("protection.admin_action_auto_deadmin", true, CVar.SERVERONLY);

    /// <summary>
    /// Automatically ban a suspected sabotage admin.
    /// </summary>
    public static readonly CVarDef<bool> AdminActionProtectionAutoBanEnabled =
        CVarDef.Create("protection.admin_action_auto_ban", false, CVar.SERVERONLY);
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

    /// <summary>
    /// Duration of the ban in seconds for admin action violations. Set to 0 for permanent ban.
    /// </summary>
    public static readonly CVarDef<int> AdminActionProtectionAutoBanMinutes =
        CVarDef.Create("protection.admin_action_auto_ban_minutes", 0, CVar.SERVERONLY);
}
