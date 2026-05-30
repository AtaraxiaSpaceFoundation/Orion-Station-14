using Content.Client._Orion.RichText;
using Content.Shared._Orion.RichText;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Orion.StyleSheets;

//
// License-Identifier: GPL-3.0-or-later
//

/// <summary>
/// Extension methods for RichTextLabel to support safe BBCode formatting.
/// Only allows whitelisted tags to prevent client crashes.
/// </summary>
public static class RichTextExtensions
{
    /// <summary>
    /// Sanitizes the input string by removing unsupported BBCode tags (e.g. [font=...]), keeping only whitelisted ones.
    /// Prevents client crashes caused by malicious or malformed BBCode.
    /// </summary>
    /// <param name="text">Input text containing BBCode tags.</param>
    /// <returns>Text with only allowed tags remaining.</returns>
    private static string SanitizeMarkup(string text)
    {
        return SafeMarkup.SanitizeBasic(text);
    }

    /// <summary>
    /// Sets the text with support for safe BBCode formatting.
    /// All disallowed tags (e.g. [font=...]) are stripped out.
    /// </summary>
    /// <param name="label">Target RichTextLabel.</param>
    /// <param name="message">Message with BBCode markup.</param>
    public static void SetMarkup(this RichTextLabel label, string message)
    {
        var safeMessage = SanitizeMarkup(message);
        label.SetMessage(FormattedMessage.FromMarkupPermissive(safeMessage), SafeMarkupTags.Basic);
    }
}
