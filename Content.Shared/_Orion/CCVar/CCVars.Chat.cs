using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /// <summary>
    ///     Enable or disable directional emotes systems.
    /// </summary>
    public static readonly CVarDef<bool> DirectionalEmotesEnabled =
        CVarDef.Create("chat.directional_emotes_enabled", false, CVar.REPLICATED);

    /// <summary>
    ///     The range at which directional emotes can be used.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteRange =
        CVarDef.Create("chat.directional_emotes_range", 3, CVar.REPLICATED);

    public static readonly CVarDef<int> ChatMaxEmoteLength =
        CVarDef.Create("chat.max_emote_length", 10000, CVar.REPLICATED);
}
