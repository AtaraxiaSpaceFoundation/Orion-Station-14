using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

//
// License-Identifier: MIT
//

public sealed partial class CCVars
{
    /// <summary>
    ///     Enable or disable NSFW content, like NSFW flavor.
    /// </summary>
    public static readonly CVarDef<bool> NsfwContentEnabled =
        CVarDef.Create("accessibility.nsfw_content_enabled", false, CVar.SERVER | CVar.REPLICATED);
}
