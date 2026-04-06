using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Enables or disables authorization via Discord.
    /// </summary>
    public static readonly CVarDef<bool> DiscordAuthEnable =
        CVarDef.Create("discord.auth_enable", false, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// This secret key is used to encrypt UserIDs.
    /// </summary>
    public static readonly CVarDef<int> DiscordAuthEncryptionKey =
        CVarDef.Create("discord.auth_encryption_key", 11114444, CVar.SERVERONLY | CVar.ARCHIVE | CVar.CONFIDENTIAL);

    /// <summary>
    /// IP address of the Discord bot server.
    /// </summary>
    public static readonly CVarDef<string> DiscordAuthBotIp =
        CVarDef.Create("discord.auth_bot_ip", "127.0.0.1", CVar.SERVERONLY);

    /// <summary>
    /// Port of the Discord bot server.
    /// </summary>
    public static readonly CVarDef<int> DiscordAuthBotPort =
        CVarDef.Create("discord.auth_bot_port", 9010, CVar.SERVERONLY);

    /// <summary>
    ///     Link to Discord server to show in the launcher.
    /// </summary>
    public static readonly CVarDef<string> InfoLinksAuthChannelDiscord =
        CVarDef.Create("infolinks.auth_channel_discord", "https://discord.com/channels/1475918847759356117/1481280582368493761", CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///    Secret token to send to the bot when a user tries to authenticate. This is used to prevent unauthorized requests to the bot.
    /// </summary>
    public static readonly CVarDef<string> DiscordAuthSendSecretTokenBot =
        CVarDef.Create("discord.auth_send_secret_token_bot", "", CVar.SERVERONLY | CVar.ARCHIVE | CVar.CONFIDENTIAL);
}
